using KeywordLab.Core.Abstractions;
using KeywordLab.Core.Models;
using KeywordLab.Core.Text;
using KeywordLab.Core.Titles;

namespace KeywordLab.Core.Services;

/// <param name="Volume">메인 키워드 검색량. 검색광고 API에 없으면 null.</param>
/// <param name="Related">메인 키워드를 뺀 연관 키워드 (검색량 순).</param>
/// <param name="Ranked">점수 순으로 정렬한 제목 후보.</param>
public sealed record TitleWorkshop(
    string Keyword,
    string Topic,
    BlogPlatform Platform,
    DateOnly Today,
    KeywordVolume? Volume,
    long BlogDocuments,
    IReadOnlyList<KeywordVolume> Related,
    IReadOnlyList<string> IntentKeywords,
    IReadOnlyList<ScoredTitle> Ranked);

/// <summary>
/// "주제·제목 확정" 흐름: 메인 키워드의 검색량·연관 키워드·문서 수·자동완성 조회 → 의도 키워드 추출 → 제목 생성 → 플랫폼 규칙 점수.
/// </summary>
public sealed class TitleWorkshopService
{
    public const int AlternateCount = 2;

    /// <summary>Codex 블록에 넣을 연관 키워드 수.</summary>
    public const int MaxRelatedInBlock = 5;

    private readonly IKeywordVolumeProvider _volumes;
    private readonly IBlogSearchProvider _blog;
    private readonly IAutocompleteProvider _autocomplete;
    private readonly ITitleGenerator _generator;
    private readonly TitleScorer _scorer;
    private readonly TimeProvider _time;

    public TitleWorkshopService(
        IKeywordVolumeProvider volumes,
        IBlogSearchProvider blog,
        IAutocompleteProvider autocomplete,
        ITitleGenerator generator,
        TitleScorer scorer,
        TimeProvider time)
    {
        _volumes = volumes;
        _blog = blog;
        _autocomplete = autocomplete;
        _generator = generator;
        _scorer = scorer;
        _time = time;
    }

    public DateOnly Today => DateOnly.FromDateTime(_time.GetLocalNow().DateTime);

    public async Task<TitleWorkshop> PrepareAsync(string keyword, string? topic, BlogPlatform platform, CancellationToken cancellationToken = default)
    {
        var kw = KeywordNormalizer.Tidy(keyword ?? string.Empty);
        if (kw.Length == 0)
        {
            throw new ArgumentException("키워드가 비어 있습니다.", nameof(keyword));
        }

        var volumesTask = _volumes.GetVolumesAsync([kw], cancellationToken);
        var blogTask = _blog.SearchAsync(kw, 1, cancellationToken);
        var suggestTask = _autocomplete.SuggestAsync(kw, cancellationToken);
        await Task.WhenAll(volumesTask, blogTask, suggestTask).ConfigureAwait(false);
        var volumes = await volumesTask.ConfigureAwait(false);
        var blog = await blogTask.ConfigureAwait(false);
        var suggestions = await suggestTask.ConfigureAwait(false);

        var compact = KeywordNormalizer.Compact(kw);
        var main = volumes.FirstOrDefault(v => KeywordNormalizer.Compact(v.Keyword) == compact);
        var related = volumes
            .Where(v => KeywordNormalizer.Compact(v.Keyword) != compact)
            .OrderByDescending(v => v.TotalApprox)
            .ToList();

        var intents = IntentKeywordExtractor.Merge(kw, suggestions, IntentKeywordExtractor.Extract(kw, related));
        var today = Today;
        var request = new TitleRequest(kw, intents, today);
        var candidates = await _generator.GenerateAsync(request, cancellationToken).ConfigureAwait(false);
        var ranked = _scorer.Rank(candidates, request, platform);

        return new TitleWorkshop(kw, KeywordNormalizer.Tidy(topic ?? string.Empty), platform, today, main, blog.Total, related, intents, ranked);
    }

    /// <summary>
    /// 기본 선택: 1위를 추천, 대안은 추천과 다른 패턴의 최고점부터 고른다 (세 제목이 서로 다른 모양이 되도록).
    /// </summary>
    public static (ScoredTitle Recommended, IReadOnlyList<ScoredTitle> Alternates) PickDefaults(IReadOnlyList<ScoredTitle> ranked)
    {
        ArgumentNullException.ThrowIfNull(ranked);
        if (ranked.Count == 0)
        {
            throw new ArgumentException("제목 후보가 없습니다.", nameof(ranked));
        }

        var recommended = ranked[0];
        var alternates = new List<ScoredTitle>();
        var usedPatterns = new HashSet<TitlePattern> { recommended.Pattern };

        foreach (var t in ranked.Skip(1))
        {
            if (alternates.Count == AlternateCount)
            {
                break;
            }

            if (usedPatterns.Add(t.Pattern))
            {
                alternates.Add(t);
            }
        }

        foreach (var t in ranked.Skip(1))
        {
            if (alternates.Count == AlternateCount)
            {
                break;
            }

            if (!alternates.Contains(t))
            {
                alternates.Add(t);
            }
        }

        return (recommended, alternates);
    }

    public static CodexBlockInput ToBlockInput(TitleWorkshop workshop, ScoredTitle recommended, IReadOnlyList<ScoredTitle> alternates, string? issueBackground)
    {
        ArgumentNullException.ThrowIfNull(workshop);
        return new CodexBlockInput(
            workshop.Today,
            workshop.Platform,
            workshop.Topic,
            workshop.Keyword,
            workshop.Volume,
            workshop.BlogDocuments,
            workshop.Related.Take(MaxRelatedInBlock).Select(r => r.Keyword).ToList(),
            recommended.Text,
            alternates.Select(a => a.Text).ToList(),
            issueBackground);
    }
}
