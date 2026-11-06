using System.Globalization;
using System.Text.Json;
using KeywordLab.Core.Abstractions;
using KeywordLab.Core.Trends;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace KeywordLab.Infrastructure.Storage;

/// <summary>
/// 수집한 트렌드 키워드를 SQLite에 쌓는다 (넘긴 기록과 같은 DB 파일).
/// 시각은 UTC "O" 형식 문자열로 저장해 문자열 비교로 순서를 맞춘다.
/// </summary>
public sealed class SqliteTrendStore : ITrendStore
{
    private const string CreateTableSql = """
        CREATE TABLE IF NOT EXISTS trends (
            keyword      TEXT PRIMARY KEY,
            first_seen   TEXT NOT NULL,
            last_seen    TEXT NOT NULL,
            max_traffic  INTEGER NOT NULL,
            news_json    TEXT NOT NULL
        );
        """;

    // 처음 본 시각은 더 이른 쪽을 남기고, 뉴스는 최근 것으로 바꾼다.
    private const string UpsertSql = """
        INSERT INTO trends (keyword, first_seen, last_seen, max_traffic, news_json)
        VALUES ($keyword, $firstSeen, $lastSeen, $traffic, $news)
        ON CONFLICT(keyword) DO UPDATE SET
            first_seen  = MIN(first_seen, excluded.first_seen),
            last_seen   = MAX(last_seen, excluded.last_seen),
            max_traffic = MAX(max_traffic, excluded.max_traffic),
            news_json   = CASE WHEN excluded.news_json = '[]' THEN news_json ELSE excluded.news_json END;
        """;

    private const string SelectSql = """
        SELECT keyword, first_seen, last_seen, max_traffic, news_json
        FROM trends
        WHERE last_seen >= $since
        """;

    private readonly string _path;

    public SqliteTrendStore(IOptions<StorageOptions> options)
    {
        _path = options.Value.ResolvePath();
    }

    public async Task UpsertAsync(IReadOnlyList<TrendItem> items, DateTimeOffset seenAt, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(items);
        if (items.Count == 0)
        {
            return;
        }

        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var tx = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        foreach (var item in items)
        {
            // pubDate를 못 읽었거나 수집 시각보다 늦으면 수집 시각을 등장 시각으로 본다.
            var firstSeen = item.PublishedAt == DateTimeOffset.MinValue || item.PublishedAt > seenAt ? seenAt : item.PublishedAt;

            await using var cmd = connection.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = UpsertSql;
            cmd.Parameters.AddWithValue("$keyword", item.Keyword);
            cmd.Parameters.AddWithValue("$firstSeen", Format(firstSeen));
            cmd.Parameters.AddWithValue("$lastSeen", Format(seenAt));
            cmd.Parameters.AddWithValue("$traffic", item.ApproxTraffic);
            cmd.Parameters.AddWithValue("$news", JsonSerializer.Serialize(item.News));
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<StoredTrend>> GetSinceAsync(DateTimeOffset since, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = SelectSql;
        cmd.Parameters.AddWithValue("$since", Format(since));

        var result = new List<StoredTrend>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            result.Add(new StoredTrend(
                reader.GetString(0),
                Parse(reader.GetString(1)),
                Parse(reader.GetString(2)),
                reader.GetInt32(3),
                JsonSerializer.Deserialize<List<TrendNews>>(reader.GetString(4)) ?? []));
        }

        return result;
    }

    private async Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var dir = Path.GetDirectoryName(Path.GetFullPath(_path));
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = _path }.ToString());
        try
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var create = connection.CreateCommand();
            create.CommandText = CreateTableSql;
            await create.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    private static string Format(DateTimeOffset value) => value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

    private static DateTimeOffset Parse(string value) => DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
}
