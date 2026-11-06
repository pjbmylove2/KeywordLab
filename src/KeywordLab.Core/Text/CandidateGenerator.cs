namespace KeywordLab.Core.Text;

/// <summary>
/// "겨울철 전기요금 아끼는 법" 같은 주제 문장에서 검색 키워드 후보를 만든다.
/// 형태소 분석기 없이 띄어쓰기 단위 조합 + 조사 제거 규칙만 쓴다.
/// 틀린 후보가 섞여도 괜찮다: 실제 판단은 네이버 검색량이 한다.
/// </summary>
public sealed class CandidateGenerator
{
    // 붙어 있어도 명사 끝으로 보기 어려운 조사: 남는 글자가 2자 이상이면 떼어낸다.
    private static readonly string[] StrongParticles =
    {
        "에서는", "으로는", "에서", "으로", "까지", "부터", "에게", "한테", "보다", "처럼", "이랑", "하고",
        "을", "를", "의", "에",
    };

    // 명사 끝 글자와 헷갈리기 쉬운 조사 (고양이, 아끼는): 남는 글자가 3자 이상일 때만 떼어낸다.
    private static readonly string[] WeakParticles = { "은", "는", "이", "가", "도", "만", "와", "과", "로" };

    private static readonly HashSet<string> StopWords = new(StringComparer.Ordinal)
    {
        "그리고", "정말", "요즘", "최근", "관련", "대한", "위한", "하는", "있는", "없는", "어떻게", "무엇", "알아보기",
    };

    public const int DefaultMaxCandidates = 10;

    public IReadOnlyList<string> Generate(string topic, int maxCandidates = DefaultMaxCandidates)
    {
        if (string.IsNullOrWhiteSpace(topic) || maxCandidates <= 0)
        {
            return Array.Empty<string>();
        }

        var tokens = KeywordNormalizer.Tidy(topic)
            .Split(' ')
            .Select(StripParticle)
            .Where(t => t.Length > 0)
            .ToArray();

        var ordered = new List<string>();

        // 1) 주제 전체 (사용자가 쓴 그대로가 가장 중요한 후보)
        ordered.Add(string.Join(' ', tokens));

        // 2) 두 단어 조합 → 3) 세 단어 조합: 롱테일 후보
        foreach (var size in new[] { 2, 3 })
        {
            for (var i = 0; i + size <= tokens.Length; i++)
            {
                ordered.Add(string.Join(' ', tokens.Skip(i).Take(size)));
            }
        }

        // 4) 단일 명사: 2자 이상, 불용어 제외
        ordered.AddRange(tokens.Where(t => t.Length >= 2 && !StopWords.Contains(t)));

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<string>();
        foreach (var candidate in ordered)
        {
            if (seen.Add(KeywordNormalizer.Compact(candidate)))
            {
                result.Add(candidate);
            }

            if (result.Count >= maxCandidates)
            {
                break;
            }
        }

        return result;
    }

    internal static string StripParticle(string token)
    {
        foreach (var p in StrongParticles)
        {
            if (token.EndsWith(p, StringComparison.Ordinal) && token.Length - p.Length >= 2)
            {
                return token[..^p.Length];
            }
        }

        foreach (var p in WeakParticles)
        {
            if (token.EndsWith(p, StringComparison.Ordinal) && token.Length - p.Length >= 3)
            {
                return token[..^p.Length];
            }
        }

        return token;
    }
}
