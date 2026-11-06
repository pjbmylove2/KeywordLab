using KeywordLab.Core.Abstractions;
using KeywordLab.Core.Models;
using KeywordLab.Core.Text;
using KeywordLab.Core.Trends;

namespace KeywordLab.Core.Services;

public sealed record IssueKeyword(
    StoredTrend Trend,
    KeywordVolume? Volume,
    long BlogDocuments,
    IReadOnlyList<string> Autocomplete,
    IssueScore Score)
{
    public string Keyword => Trend.Keyword;
}

/// <param name="Collected">이번에 RSS에서 받은 항목 수.</param>
/// <param name="CollectError">RSS 수집 실패 사유. 실패해도 쌓아 둔 키워드로 목록을 만든다.</param>
public sealed record IssueBoard(IReadOnlyList<IssueKeyword> Keywords, int Collected, string? CollectError, DateTimeOffset Now);

/// <summary>
/// "오늘의 이슈 키워드" 흐름: 구글 트렌드 RSS 수집·저장 → 최근 키워드의 네이버 검색량·문서 수·자동완성 조회 → 점수순.
/// </summary>
public sealed class IssueKeywordService
{
    /// <summary>네이버 API 호출을 아끼기 위해 임시 순위 상위 몇 개만 조회한다.</summary>
    public const int MaxKeywords = 15;

    private readonly ITrendFeed _feed;
    private readonly ITrendStore _store;
    private readonly IKeywordVolumeProvider _volumes;
    private readonly IBlogSearchProvider _blog;
    private readonly IAutocompleteProvider _autocomplete;
    private readonly IssueKeywordScorer _scorer;
    private readonly TimeProvider _time;

    public IssueKeywordService(
        ITrendFeed feed,
        ITrendStore store,
        IKeywordVolumeProvider volumes,
        IBlogSearchProvider blog,
        IAutocompleteProvider autocomplete,
        IssueKeywordScorer scorer,
        TimeProvider time)
    {
        _feed = feed;
        _store = store;
        _volumes = volumes;
        _blog = blog;
        _autocomplete = autocomplete;
        _scorer = scorer;
        _time = time;
    }

    /// <summary>RSS를 받아 저장한다. 수집기(5단계 작업 스케줄러)도 이 메서드를 쓴다.</summary>
    public async Task<int> CollectAsync(CancellationToken cancellationToken = default)
    {
        var items = await _feed.FetchAsync(cancellationToken).ConfigureAwait(false);
        await _store.UpsertAsync(items, _time.GetUtcNow(), cancellationToken).ConfigureAwait(false);
        return items.Count;
    }

    public async Task<IssueBoard> GetBoardAsync(CancellationToken cancellationToken = default)
    {
        var collected = 0;
        string? collectError = null;
        try
        {
            collected = await CollectAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or FormatException or System.Xml.XmlException
                                   || (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            collectError = ex.Message;
        }

        var now = _time.GetUtcNow();
        var trends = (await _store.GetSinceAsync(now.AddHours(-_scorer.Rules.WindowHours), cancellationToken).ConfigureAwait(false))
            .OrderByDescending(t => _scorer.Provisional(t, now))
            .Take(MaxKeywords)
            .ToList();

        if (trends.Count == 0)
        {
            return new IssueBoard([], collected, collectError, now);
        }

        var volumes = await _volumes.GetVolumesAsync(trends.Select(t => t.Keyword).ToList(), cancellationToken).ConfigureAwait(false);
        var byCompact = new Dictionary<string, KeywordVolume>(StringComparer.Ordinal);
        foreach (var v in volumes)
        {
            byCompact.TryAdd(KeywordNormalizer.Compact(v.Keyword), v);
        }

        var blogs = await Task.WhenAll(trends.Select(t => _blog.SearchAsync(t.Keyword, 1, cancellationToken))).ConfigureAwait(false);
        var suggestions = await Task.WhenAll(trends.Select(t => _autocomplete.SuggestAsync(t.Keyword, cancellationToken))).ConfigureAwait(false);

        var result = trends
            .Select((t, i) =>
            {
                byCompact.TryGetValue(KeywordNormalizer.Compact(t.Keyword), out var volume);
                return new IssueKeyword(t, volume, blogs[i].Total, suggestions[i], _scorer.Score(t, volume, blogs[i].Total, now));
            })
            .OrderByDescending(k => k.Score.Score)
            .ToList();

        return new IssueBoard(result, collected, collectError, now);
    }
}
