namespace KeywordLab.Core.Titles;

public enum BlogPlatform
{
    Tistory,
    Naver,
}

public enum TitlePattern
{
    /// <summary>정보 정리형: "~ 총정리"</summary>
    Summary,
    /// <summary>질문형: "~ 어떻게 하면 될까?"</summary>
    Question,
    /// <summary>시점형: "~ 2026년 11월 기준 정리"</summary>
    Timely,
}

public static class TitleLabels
{
    public static string Of(BlogPlatform platform) => platform switch
    {
        BlogPlatform.Naver => "네이버",
        _ => "티스토리",
    };

    public static string Of(TitlePattern pattern) => pattern switch
    {
        TitlePattern.Question => "질문형",
        TitlePattern.Timely => "시점형",
        _ => "정보 정리형",
    };
}

/// <param name="MainKeyword">메인 키워드. 원형 그대로 제목 맨 앞에 둔다.</param>
/// <param name="IntentKeywords">검색 의도 키워드 (최대 2개).</param>
/// <param name="Today">시점형 제목의 연도·월 기준일.</param>
public sealed record TitleRequest(string MainKeyword, IReadOnlyList<string> IntentKeywords, DateOnly Today);

public sealed record TitleCandidate(string Text, TitlePattern Pattern);

/// <summary>점수 한 항목. Points가 음수면 감점.</summary>
public sealed record ScoreReason(string Text, int Points);

public sealed record ScoredTitle(TitleCandidate Candidate, int Score, IReadOnlyList<ScoreReason> Reasons)
{
    public string Text => Candidate.Text;

    public TitlePattern Pattern => Candidate.Pattern;
}
