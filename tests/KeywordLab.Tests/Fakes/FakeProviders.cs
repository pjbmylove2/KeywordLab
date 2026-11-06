using KeywordLab.Core.Abstractions;
using KeywordLab.Core.Models;

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

internal static class V
{
    public static KeywordVolume Of(string keyword, int pc, int mobile) =>
        new(keyword, VolumeCount.Of(pc), VolumeCount.Of(mobile), "중간");
}
