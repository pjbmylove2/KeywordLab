using System.Text.RegularExpressions;
using KeywordLab.Core.Text;

namespace KeywordLab.Core.Titles;

/// <summary>
/// 제목 점수 기준. 금지 표현은 appsettings.json의 "Titles" 구역에서 읽는다 (코드에 고정하지 않는다).
/// 설정 바인딩이 목록을 덮어쓰지 않고 이어 붙이므로 기본값은 빈 배열로 둔다.
/// </summary>
public sealed class TitleScoringRules
{
    public const string SectionName = "Titles";

    /// <summary>금지 표현 (충격, 경악 등). 포함되면 크게 감점한다.</summary>
    public string[] BannedPhrases { get; set; } = [];

    /// <summary>티스토리 제목 길이 가점 기준.</summary>
    public int TistoryMaxLength { get; set; } = 40;

    /// <summary>네이버 제목 특수문자 허용 개수. 넘으면 감점.</summary>
    public int MaxSpecialChars { get; set; } = 3;
}

/// <summary>
/// 규칙 점수. 50점에서 시작해 가점·감점을 더하고 0~100으로 자른다.
/// 공통: 메인 키워드 맨 앞, 검색 의도 키워드 1~2개, 금지 표현.
/// 티스토리: 길이, 연도·월 표기. 네이버: 길이 제한 없음, 같은 단어 반복·특수문자 남용.
/// </summary>
public sealed partial class TitleScorer
{
    public const int BaseScore = 50;
    public const int KeywordFirstPoints = 10;
    public const int IntentPoints = 10;
    public const int BannedPhrasePenalty = -30;
    public const int TistoryLengthPoints = 15;
    public const int TistoryTooLongPenalty = -10;
    public const int DatePoints = 10;
    public const int RepeatPenalty = -10;
    public const int SpecialCharPenalty = -10;

    private readonly TitleScoringRules _rules;

    public TitleScorer(TitleScoringRules? rules = null)
    {
        _rules = rules ?? new TitleScoringRules();
    }

    public ScoredTitle Score(TitleCandidate candidate, TitleRequest request, BlogPlatform platform)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(request);

        var text = candidate.Text;
        var reasons = new List<ScoreReason>();

        var keyword = KeywordNormalizer.Tidy(request.MainKeyword);
        reasons.Add(text.StartsWith(keyword, StringComparison.Ordinal)
            ? new ScoreReason("메인 키워드가 맨 앞", KeywordFirstPoints)
            : new ScoreReason("메인 키워드가 맨 앞에 없음", -KeywordFirstPoints));

        var intents = request.IntentKeywords.Count(i => i.Length > 0 && text.Contains(i, StringComparison.Ordinal));
        if (intents > 0)
        {
            reasons.Add(new ScoreReason($"검색 의도 키워드 {intents}개", IntentPoints));
        }

        foreach (var phrase in _rules.BannedPhrases.Where(p => !string.IsNullOrWhiteSpace(p)))
        {
            if (text.Contains(phrase, StringComparison.Ordinal))
            {
                reasons.Add(new ScoreReason($"금지 표현 '{phrase}'", BannedPhrasePenalty));
            }
        }

        if (platform == BlogPlatform.Tistory)
        {
            reasons.Add(text.Length <= _rules.TistoryMaxLength
                ? new ScoreReason($"{_rules.TistoryMaxLength}자 이내 ({text.Length}자)", TistoryLengthPoints)
                : new ScoreReason($"{_rules.TistoryMaxLength}자 초과 ({text.Length}자)", TistoryTooLongPenalty));

            if (DateMark().IsMatch(text))
            {
                reasons.Add(new ScoreReason("연도·월 표기", DatePoints));
            }
        }
        else
        {
            var repeated = Words().Matches(text)
                .Select(m => m.Value)
                .GroupBy(w => w, StringComparer.Ordinal)
                .Where(g => g.Count() > 1);
            foreach (var g in repeated)
            {
                reasons.Add(new ScoreReason($"'{g.Key}' 반복", RepeatPenalty * (g.Count() - 1)));
            }

            var special = text.Count(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c));
            if (special > _rules.MaxSpecialChars)
            {
                reasons.Add(new ScoreReason($"특수문자 {special}개", SpecialCharPenalty));
            }
        }

        var score = Math.Clamp(BaseScore + reasons.Sum(r => r.Points), 0, 100);
        return new ScoredTitle(candidate, score, reasons);
    }

    /// <summary>점수 높은 순, 같으면 짧은 순.</summary>
    public IReadOnlyList<ScoredTitle> Rank(IEnumerable<TitleCandidate> candidates, TitleRequest request, BlogPlatform platform) =>
        candidates
            .Select(c => Score(c, request, platform))
            .OrderByDescending(s => s.Score)
            .ThenBy(s => s.Text.Length)
            .ToList();

    [GeneratedRegex(@"\d{4}년|\d{1,2}월")]
    private static partial Regex DateMark();

    [GeneratedRegex(@"[\p{L}\p{N}]+")]
    private static partial Regex Words();
}
