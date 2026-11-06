namespace KeywordLab.Core.Assessment;

public enum CompetitionLevel
{
    /// <summary>검색량 대비 문서가 아주 적다.</summary>
    Easy,
    /// <summary>검색량 대비 문서 수가 적당하다.</summary>
    Good,
    /// <summary>문서가 검색량보다 훨씬 많다.</summary>
    HighCompetition,
    /// <summary>찾는 사람이 거의 없다.</summary>
    LowDemand,
}

public sealed record KeywordAssessment(CompetitionLevel Level, double DocsPerSearch, string Label, string Reason)
{
    public bool IsRecommended => Level is CompetitionLevel.Easy or CompetitionLevel.Good;
}

/// <summary>
/// 경쟁도 판단 기준. 처음 값은 임시 기준이며, 실제 글 성과를 보면서 조정한다.
/// </summary>
public sealed record AssessmentRules
{
    /// <summary>월간 검색 수가 이보다 작으면 수요 부족.</summary>
    public int MinMonthlySearches { get; init; } = 50;

    /// <summary>검색 1회당 문서 수가 이 이하면 진입 쉬움.</summary>
    public double EasyDocsPerSearch { get; init; } = 0.5;

    /// <summary>검색 1회당 문서 수가 이보다 크면 경쟁 높음.</summary>
    public double HighDocsPerSearch { get; init; } = 3.0;

    public static AssessmentRules Default { get; } = new();
}

public sealed class CompetitionAssessor
{
    private readonly AssessmentRules _rules;

    public CompetitionAssessor(AssessmentRules? rules = null)
    {
        _rules = rules ?? AssessmentRules.Default;
    }

    public KeywordAssessment Assess(int monthlySearches, long blogDocuments)
    {
        var ratio = blogDocuments / (double)Math.Max(monthlySearches, 1);

        if (monthlySearches < _rules.MinMonthlySearches)
        {
            return new(CompetitionLevel.LowDemand, ratio, "검색량 적음",
                $"월 검색이 {_rules.MinMonthlySearches}회보다 적어 유입을 기대하기 어렵습니다.");
        }

        if (ratio > _rules.HighDocsPerSearch)
        {
            return new(CompetitionLevel.HighCompetition, ratio, "경쟁 높음",
                "수요는 있지만 문서가 많아 상위 노출이 어렵습니다.");
        }

        if (ratio <= _rules.EasyDocsPerSearch)
        {
            return new(CompetitionLevel.Easy, ratio, "추천 · 진입 쉬움",
                "검색량에 비해 문서가 적어 먼저 쓰면 유리합니다.");
        }

        return new(CompetitionLevel.Good, ratio, "추천",
            "검색량 대비 문서 수가 적당합니다.");
    }
}
