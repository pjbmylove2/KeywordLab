using KeywordLab.Core.Abstractions;
using KeywordLab.Core.Text;

namespace KeywordLab.Core.Titles;

/// <summary>
/// 템플릿 조합으로 제목 후보를 만든다. 메인 키워드는 원형 그대로 맨 앞에 두고,
/// 검색 의도 키워드는 0~2개를 붙인 조합을 모두 만든다 (어떤 조합이 나은지는 점수가 정한다).
/// </summary>
public sealed class TemplateTitleGenerator : ITitleGenerator
{
    public const int MaxIntentKeywords = 2;

    // {k}: 메인 키워드 + 의도 키워드, {y}: 연도, {m}: 월
    private static readonly (TitlePattern Pattern, string Template)[] Templates =
    [
        (TitlePattern.Summary, "{k} 총정리"),
        (TitlePattern.Summary, "{k} 한눈에 정리"),
        (TitlePattern.Question, "{k}, 어떻게 하면 될까?"),
        (TitlePattern.Question, "{k} 꼭 알아야 할 점은?"),
        (TitlePattern.Timely, "{k} {y}년 {m}월 기준 정리"),
        (TitlePattern.Timely, "{k}, {y}년 {m}월 달라진 점"),
    ];

    public Task<IReadOnlyList<TitleCandidate>> GenerateAsync(TitleRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var keyword = KeywordNormalizer.Tidy(request.MainKeyword);
        if (keyword.Length == 0)
        {
            return Task.FromResult<IReadOnlyList<TitleCandidate>>([]);
        }

        var intents = request.IntentKeywords
            .Select(KeywordNormalizer.Tidy)
            .Where(i => i.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .Take(MaxIntentKeywords)
            .ToList();

        // 의도 키워드 없음 → 1개 → 2개 순으로 앞부분을 만든다.
        var heads = new List<string> { keyword };
        if (intents.Count >= 1)
        {
            heads.Add($"{keyword} {intents[0]}");
        }

        if (intents.Count >= 2)
        {
            heads.Add($"{keyword} {intents[0]}·{intents[1]}");
        }

        var year = request.Today.Year.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var month = request.Today.Month.ToString(System.Globalization.CultureInfo.InvariantCulture);

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<TitleCandidate>();
        foreach (var head in heads)
        {
            foreach (var (pattern, template) in Templates)
            {
                var text = template.Replace("{k}", head).Replace("{y}", year).Replace("{m}", month);
                if (seen.Add(text))
                {
                    result.Add(new TitleCandidate(text, pattern));
                }
            }
        }

        return Task.FromResult<IReadOnlyList<TitleCandidate>>(result);
    }
}
