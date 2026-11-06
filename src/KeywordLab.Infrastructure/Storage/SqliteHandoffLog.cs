using System.Globalization;
using KeywordLab.Core.Abstractions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace KeywordLab.Infrastructure.Storage;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>SQLite 파일 경로. 비우면 %LOCALAPPDATA%\KeywordLab\keywordlab.db (저장소 밖).</summary>
    public string DatabasePath { get; set; } = string.Empty;

    public string ResolvePath() => string.IsNullOrWhiteSpace(DatabasePath)
        ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KeywordLab", "keywordlab.db")
        : DatabasePath;
}

/// <summary>Codex로 넘긴 기록을 SQLite에 저장한다. 표가 없으면 처음 저장할 때 만든다.</summary>
public sealed class SqliteHandoffLog : IHandoffLog
{
    private const string CreateTableSql = """
        CREATE TABLE IF NOT EXISTS handoffs (
            id          INTEGER PRIMARY KEY AUTOINCREMENT,
            keyword     TEXT NOT NULL,
            platform    TEXT NOT NULL,
            title       TEXT NOT NULL,
            pattern     TEXT NOT NULL,
            created_at  TEXT NOT NULL
        );
        """;

    private const string InsertSql = """
        INSERT INTO handoffs (keyword, platform, title, pattern, created_at)
        VALUES ($keyword, $platform, $title, $pattern, $createdAt);
        """;

    private readonly string _path;

    public SqliteHandoffLog(IOptions<StorageOptions> options)
    {
        _path = options.Value.ResolvePath();
    }

    public string DatabasePath => _path;

    public async Task SaveAsync(HandoffRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        var dir = Path.GetDirectoryName(Path.GetFullPath(_path));
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = _path }.ToString());
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using (var create = connection.CreateCommand())
        {
            create.CommandText = CreateTableSql;
            await create.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await using var insert = connection.CreateCommand();
        insert.CommandText = InsertSql;
        insert.Parameters.AddWithValue("$keyword", record.Keyword);
        insert.Parameters.AddWithValue("$platform", record.Platform.ToString());
        insert.Parameters.AddWithValue("$title", record.Title);
        insert.Parameters.AddWithValue("$pattern", record.Pattern.ToString());
        insert.Parameters.AddWithValue("$createdAt", record.CreatedAt.ToString("O", CultureInfo.InvariantCulture));
        await insert.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
