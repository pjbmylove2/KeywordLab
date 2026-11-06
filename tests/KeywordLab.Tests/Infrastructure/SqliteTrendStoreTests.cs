using KeywordLab.Core.Trends;
using KeywordLab.Infrastructure.Storage;
using Microsoft.Data.Sqlite;

namespace KeywordLab.Tests.Infrastructure;

public sealed class SqliteTrendStoreTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 11, 6, 12, 0, 0, TimeSpan.Zero);

    private readonly string _dir = Path.Combine(Path.GetTempPath(), "keywordlab-tests-" + Guid.NewGuid().ToString("N"));

    private SqliteTrendStore Store() =>
        new(Microsoft.Extensions.Options.Options.Create(new StorageOptions { DatabasePath = Path.Combine(_dir, "trends.db") }));

    [Fact]
    public async Task Upsert_keeps_earliest_first_seen_and_max_traffic()
    {
        var store = Store();
        var news = new[] { new TrendNews("뉴스", "https://news.example/1", "언론사") };

        await store.UpsertAsync([new TrendItem("과징금", 200, Now.AddHours(-10), news)], Now.AddHours(-9));
        await store.UpsertAsync([new TrendItem("과징금", 500, Now.AddHours(-1), [])], Now);

        var t = Assert.Single(await store.GetSinceAsync(Now.AddHours(-1)));
        Assert.Equal(Now.AddHours(-10), t.FirstSeenAt);
        Assert.Equal(Now, t.LastSeenAt);
        Assert.Equal(500, t.MaxTraffic);
        Assert.Equal("뉴스", Assert.Single(t.News).Title); // 빈 뉴스로 덮어쓰지 않는다
    }

    [Fact]
    public async Task Unknown_or_future_pubdate_uses_seen_time()
    {
        var store = Store();
        var seenAt = Now.ToOffset(TimeSpan.FromHours(9));

        await store.UpsertAsync(
            [
                new TrendItem("날짜없음", 100, DateTimeOffset.MinValue, []),
                new TrendItem("미래", 100, Now.AddHours(5), []),
            ],
            seenAt);

        var all = await store.GetSinceAsync(Now.AddDays(-1));
        Assert.All(all, t => Assert.Equal(Now, t.FirstSeenAt));
    }

    [Fact]
    public async Task GetSince_filters_by_last_seen()
    {
        var store = Store();
        await store.UpsertAsync([new TrendItem("옛날", 100, Now.AddDays(-5), [])], Now.AddDays(-4));
        await store.UpsertAsync([new TrendItem("요즘", 100, Now.AddHours(-1), [])], Now);

        var recent = await store.GetSinceAsync(Now.AddDays(-1));

        Assert.Equal(["요즘"], recent.Select(t => t.Keyword));
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
