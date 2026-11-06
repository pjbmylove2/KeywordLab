using KeywordLab.Core.Abstractions;
using KeywordLab.Core.Titles;
using KeywordLab.Infrastructure.Storage;
using Microsoft.Data.Sqlite;

namespace KeywordLab.Tests.Infrastructure;

public sealed class SqliteHandoffLogTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "keywordlab-tests-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task Creates_database_and_saves_record()
    {
        var path = Path.Combine(_dir, "sub", "test.db");
        var log = new SqliteHandoffLog(Microsoft.Extensions.Options.Options.Create(new StorageOptions { DatabasePath = path }));
        var at = new DateTimeOffset(2026, 11, 6, 17, 30, 0, TimeSpan.FromHours(9));

        await log.SaveAsync(new HandoffRecord("전기요금", BlogPlatform.Naver, "전기요금 할인 총정리", TitlePattern.Summary, at));
        await log.SaveAsync(new HandoffRecord("겨울철", BlogPlatform.Tistory, "겨울철 2026년 11월 기준 정리", TitlePattern.Timely, at));

        using var conn = new SqliteConnection($"Data Source={path}");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT keyword, platform, title, pattern, created_at FROM handoffs ORDER BY id";
        using var reader = cmd.ExecuteReader();

        Assert.True(reader.Read());
        Assert.Equal("전기요금", reader.GetString(0));
        Assert.Equal("Naver", reader.GetString(1));
        Assert.Equal("전기요금 할인 총정리", reader.GetString(2));
        Assert.Equal("Summary", reader.GetString(3));
        Assert.Equal(at, DateTimeOffset.Parse(reader.GetString(4), System.Globalization.CultureInfo.InvariantCulture));
        Assert.True(reader.Read());
        Assert.False(reader.Read());
    }

    [Fact]
    public void Empty_path_resolves_outside_repository()
    {
        var path = new StorageOptions().ResolvePath();

        Assert.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), path);
        Assert.EndsWith("keywordlab.db", path);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }
}
