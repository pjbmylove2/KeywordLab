using KeywordLab.Core.Abstractions;
using KeywordLab.Core.Assessment;
using KeywordLab.Core.Models;
using KeywordLab.Core.Text;

namespace KeywordLab.Core.Services;

public sealed record CandidateResult(string Keyword, KeywordVolume Volume, BlogSearchSummary Blog, KeywordAssessment Assessment);

public sealed record LookupResult(
    string Topic,
    IReadOnlyList<CandidateResult> Candidates,
    IReadOnlyList<KeywordVolume> Related,
    IReadOnlyList<string> NotFound);

/// <summary>
/// "키워드 직접 조회" 흐름: 주제 → 후보 생성 → 검색량 조회 → 상위 후보만 문서 수 조회 → 경쟁도 판단.
/// </summary>
public sealed class KeywordLookupService
{
    /// <summary>블로그 검색 API 호출을 아끼기 위해 문서 수는 검색량 상위 몇 개만 조회한다.</summary>
    public const int MaxBlogLookups = 5;

    public const int MaxRelated = 10;

    private readonly IKeywordVolumeProvider _volumes;
    private readonly IBlogSearchProvider _blog;
    private readonly CandidateGenerator _generator;
    private readonly CompetitionAssessor _assessor;

    public KeywordLookupService(
        IKeywordVolumeProvider volumes,
        IBlogSearchProvider blog,
        CandidateGenerator generator,
        CompetitionAssessor assessor)
    {
        _volumes = volumes;
        _blog = blog;
        _generator = generator;
        _assessor = assessor;
    }

    public async Task<LookupResult> LookupAsync(string topic, CancellationToken cancellationToken = default)
    {
        var tidy = KeywordNormalizer.Tidy(topic ?? string.Empty);
        var candidates = _generator.Generate(tidy);
        if (candidates.Count == 0)
        {
            return new LookupResult(tidy, Array.Empty<CandidateResult>(), Array.Empty<KeywordVolume>(), Array.Empty<string>());
        }

        var volumes = await _volumes.GetVolumesAsync(candidates, cancellationToken).ConfigureAwait(false);

        var byCompact = new Dictionary<string, KeywordVolume>(StringComparer.Ordinal);
        foreach (var v in volumes)
        {
            byCompact.TryAdd(KeywordNormalizer.Compact(v.Keyword), v);
        }

        var found = new List<(string Keyword, KeywordVolume Volume)>();
        var notFound = new List<string>();
        foreach (var c in candidates)
        {
            if (byCompact.TryGetValue(KeywordNormalizer.Compact(c), out var v))
            {
                found.Add((c, v));
            }
            else
            {
                notFound.Add(c);
            }
        }

        var top = found
            .OrderByDescending(f => f.Volume.TotalApprox)
            .Take(MaxBlogLookups)
            .ToList();

        var blogTasks = top.Select(f => _blog.SearchAsync(f.Keyword, 3, cancellationToken)).ToArray();
        var blogs = await Task.WhenAll(blogTasks).ConfigureAwait(false);

        var results = top
            .Select((f, i) => new CandidateResult(
                f.Keyword,
                f.Volume,
                blogs[i],
                _assessor.Assess(f.Volume.TotalApprox, blogs[i].Total)))
            .OrderBy(r => RankOf(r.Assessment.Level))
            .ThenByDescending(r => r.Volume.TotalApprox)
            .ToList();

        var candidateKeys = new HashSet<string>(candidates.Select(KeywordNormalizer.Compact), StringComparer.Ordinal);
        var related = volumes
            .Where(v => !candidateKeys.Contains(KeywordNormalizer.Compact(v.Keyword)))
            .OrderByDescending(v => v.TotalApprox)
            .Take(MaxRelated)
            .ToList();

        return new LookupResult(tidy, results, related, notFound);
    }

    private static int RankOf(CompetitionLevel level) => level switch
    {
        CompetitionLevel.Easy => 0,
        CompetitionLevel.Good => 1,
        CompetitionLevel.HighCompetition => 2,
        _ => 3,
    };
}
