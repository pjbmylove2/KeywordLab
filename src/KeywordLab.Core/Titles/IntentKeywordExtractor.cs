using KeywordLab.Core.Models;

namespace KeywordLab.Core.Titles;

/// <summary>
/// 연관 키워드에서 검색 의도 키워드를 뽑는다.
/// 연관 키워드가 메인 키워드로 시작하면 뒤에 붙은 말이 의도다. 예: 메인 "전기요금", 연관 "전기요금할인" → "할인".
/// 메인 키워드 앞에 붙는 말("겨울철전기요금"의 "겨울철")은 메인 키워드를 제목 맨 앞에 둔다는 규칙과 맞지 않아 쓰지 않는다.
/// </summary>
public static class IntentKeywordExtractor
{
    /// <summary>너무 긴 꼬리는 별개의 주제일 가능성이 커서 뺀다.</summary>
    public const int MaxIntentLength = 6;

    public static IReadOnlyList<string> Extract(string mainKeyword, IEnumerable<KeywordVolume> related, int max = TemplateTitleGenerator.MaxIntentKeywords)
    {
        ArgumentNullException.ThrowIfNull(mainKeyword);
        ArgumentNullException.ThrowIfNull(related);

        var main = RemoveSpaces(mainKeyword);
        if (main.Length == 0 || max <= 0)
        {
            return [];
        }

        var result = new List<string>();
        foreach (var r in related.OrderByDescending(v => v.TotalApprox))
        {
            var rel = RemoveSpaces(r.Keyword);
            if (rel.Length <= main.Length || !rel.StartsWith(main, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var tail = rel[main.Length..];
            if (tail.Length > MaxIntentLength || result.Contains(tail, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            result.Add(tail);
            if (result.Count == max)
            {
                break;
            }
        }

        return result;
    }

    private static string RemoveSpaces(string s) => new(s.Where(c => !char.IsWhiteSpace(c)).ToArray());
}
