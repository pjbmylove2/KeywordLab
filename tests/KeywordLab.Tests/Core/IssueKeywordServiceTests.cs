using KeywordLab.Core.Services;
using KeywordLab.Core.Trends;
using KeywordLab.Tests.Fakes;

namespace KeywordLab.Tests.Core;

public class IssueKeywordServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 6, 12, 0, 0, TimeSpan.Zero);

    private static IssueKeywordService Service(
        FakeTrendFeed feed,
        InMemoryTrendStore store,
        FakeVolumeProvider? volumes = null,
        FakeBlogProvider? blog = null,
        FakeAutocomplete? autocomplete = null) =>
        new(feed, store, volumes ?? new FakeVolumeProvider(), blog ?? new FakeBlogProvider(new()), autocomplete ?? new FakeAutocomplete(),
            new IssueKeywordScorer(), new FixedClock(Now));

    private static TrendItem Item(string keyword, int traffic, double hoursAgo) =>
        new(keyword, traffic, Now.AddHours(-hoursAgo), [new TrendNews($"{keyword} 뉴스", "https://news.example/1", "언론사")]);

    [Fact]
    public async Task Collects_then_ranks_by_score()
    {
        var feed = new FakeTrendFeed([Item("과징금", 500, 1), Item("나혜미", 200, 1)]);
        var volumes = new FakeVolumeProvider(V.Of("과징금", 100, 200), V.Of("나혜미", 100, 200));
        var blog = new FakeBlogProvider(new() { ["과징금"] = 10, ["나혜미"] = 10 });
        var autocomplete = new FakeAutocomplete(new() { ["과징금"] = ["과징금 뜻"] });

        var board = await Service(feed, new InMemoryTrendStore(), volumes, blog, autocomplete).GetBoardAsync();

        Assert.Equal(2, board.Collected);
        Assert.Null(board.CollectError);
        Assert.Equal(["과징금", "나혜미"], board.Keywords.Select(k => k.Keyword));
        Assert.Equal(300, board.Keywords[0].Volume!.TotalApprox);
        Assert.Equal(["과징금 뜻"], board.Keywords[0].Autocomplete);
        Assert.Single(board.Keywords[0].Trend.News);
    }

    [Fact]
    public async Task Feed_failure_still_shows_stored_keywords()
    {
        var store = new InMemoryTrendStore();
        await store.UpsertAsync([Item("과징금", 500, 2)], Now.AddHours(-1));

        var board = await Service(new FakeTrendFeed([], new HttpRequestException("연결 실패")), store).GetBoardAsync();

        Assert.Equal("연결 실패", board.CollectError);
        Assert.Equal(["과징금"], board.Keywords.Select(k => k.Keyword));
    }

    [Fact]
    public async Task Old_keywords_outside_window_are_left_out()
    {
        var store = new InMemoryTrendStore();
        await store.UpsertAsync([Item("옛날키워드", 5000, 100)], Now.AddHours(-100));

        var board = await Service(new FakeTrendFeed([]), store).GetBoardAsync();

        Assert.Empty(board.Keywords);
    }

    [Fact]
    public async Task Looks_up_naver_for_at_most_max_keywords()
    {
        var items = Enumerable.Range(1, IssueKeywordService.MaxKeywords + 5).Select(i => Item($"키워드{i}", i * 10, 1)).ToList();
        var blog = new FakeBlogProvider(new());

        var board = await Service(new FakeTrendFeed(items), new InMemoryTrendStore(), blog: blog).GetBoardAsync();

        Assert.Equal(IssueKeywordService.MaxKeywords, board.Keywords.Count);
        Assert.Equal(IssueKeywordService.MaxKeywords, blog.Queries.Count);
        Assert.DoesNotContain(board.Keywords, k => k.Keyword == "키워드1"); // 트래픽이 가장 낮은 것은 빠진다
    }

    [Fact]
    public async Task First_seen_keeps_earliest_time_across_collections()
    {
        var store = new InMemoryTrendStore();
        await store.UpsertAsync([Item("과징금", 200, 10)], Now.AddHours(-9));

        await Service(new FakeTrendFeed([Item("과징금", 500, 1)]), store).CollectAsync();

        var t = store.Items["과징금"];
        Assert.Equal(Now.AddHours(-10), t.FirstSeenAt);
        Assert.Equal(500, t.MaxTraffic);
    }
}
