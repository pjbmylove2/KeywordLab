using KeywordLab.Core.Abstractions;
using KeywordLab.Core.Models;
using KeywordLab.Core.Trends;

namespace KeywordLab.Tests.Fakes;

internal sealed class FakeVolumeProvider : IKeywordVolumeProvider
{
    private readonly IReadOnlyList<KeywordVolume> _volumes;

    public FakeVolumeProvider(params KeywordVolume[] volumes) => _volumes = volumes;

    public List<IReadOnlyList<string>> Calls { get; } = new();

    public Task<IReadOnlyList<KeywordVolume>> GetVolumesAsync(IReadOnlyList<string> hintKeywords, CancellationToken cancellationToken = default)
    {
        Calls.Add(hintKeywords);
        return Task.FromResult(_volumes);
    }
}

internal sealed class FakeBlogProvider : IBlogSearchProvider
{
    private readonly Dictionary<string, long> _totals;

    public FakeBlogProvider(Dictionary<string, long> totals) => _totals = totals;

    public List<string> Queries { get; } = new();

    public Task<BlogSearchSummary> SearchAsync(string query, int display = 5, CancellationToken cancellationToken = default)
    {
        lock (Queries)
        {
            Queries.Add(query);
        }

        var total = _totals.TryGetValue(query, out var t) ? t : 0;
        var posts = new[] { new BlogPostTitle($"{query} 총정리", "https://blog.example/1", "블로거", "20261105") };
        return Task.FromResult(new BlogSearchSummary(total, posts));
    }
}

internal sealed class FakeAutocomplete : IAutocompleteProvider
{
    private readonly Dictionary<string, string[]> _suggestions;

    public FakeAutocomplete(Dictionary<string, string[]>? suggestions = null) => _suggestions = suggestions ?? new();

    public Task<IReadOnlyList<string>> SuggestAsync(string query, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<string>>(_suggestions.TryGetValue(query, out var s) ? s : []);
}

internal sealed class FakeTrendFeed : ITrendFeed
{
    private readonly IReadOnlyList<TrendItem> _items;
    private readonly Exception? _error;

    public FakeTrendFeed(IReadOnlyList<TrendItem> items, Exception? error = null)
    {
        _items = items;
        _error = error;
    }

    public Task<IReadOnlyList<TrendItem>> FetchAsync(CancellationToken cancellationToken = default) =>
        _error is null ? Task.FromResult(_items) : Task.FromException<IReadOnlyList<TrendItem>>(_error);
}

/// <summary>SqliteTrendStore와 같은 규칙(처음 본 시각은 더 이른 쪽)을 메모리에서 흉내 낸다.</summary>
internal sealed class InMemoryTrendStore : ITrendStore
{
    public Dictionary<string, StoredTrend> Items { get; } = new();

    public Task UpsertAsync(IReadOnlyList<TrendItem> items, DateTimeOffset seenAt, CancellationToken cancellationToken = default)
    {
        foreach (var i in items)
        {
            var first = i.PublishedAt > seenAt ? seenAt : i.PublishedAt;
            Items[i.Keyword] = Items.TryGetValue(i.Keyword, out var old)
                ? old with { FirstSeenAt = first < old.FirstSeenAt ? first : old.FirstSeenAt, LastSeenAt = seenAt, MaxTraffic = Math.Max(old.MaxTraffic, i.ApproxTraffic) }
                : new StoredTrend(i.Keyword, first, seenAt, i.ApproxTraffic, i.News);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<StoredTrend>> GetSinceAsync(DateTimeOffset since, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<StoredTrend>>(Items.Values.Where(t => t.LastSeenAt >= since).ToList());
}

internal sealed class FixedClock(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}

internal static class V
{
    public static KeywordVolume Of(string keyword, int pc, int mobile) =>
        new(keyword, VolumeCount.Of(pc), VolumeCount.Of(mobile), "중간");
}
