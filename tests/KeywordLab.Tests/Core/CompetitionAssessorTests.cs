using KeywordLab.Core.Assessment;

namespace KeywordLab.Tests.Core;

public class CompetitionAssessorTests
{
    private readonly CompetitionAssessor _sut = new();

    [Theory]
    [InlineData(30, 10, CompetitionLevel.LowDemand)]
    [InlineData(10_000, 40_000, CompetitionLevel.HighCompetition)]
    [InlineData(10_000, 3_000, CompetitionLevel.Easy)]
    [InlineData(10_000, 15_000, CompetitionLevel.Good)]
    [InlineData(10_000, 30_000, CompetitionLevel.Good)] // 정확히 3배는 아직 '적당'
    public void Assess_levels(int searches, long docs, CompetitionLevel expected)
    {
        Assert.Equal(expected, _sut.Assess(searches, docs).Level);
    }

    [Fact]
    public void Custom_rules_change_thresholds()
    {
        var strict = new CompetitionAssessor(new AssessmentRules { HighDocsPerSearch = 1.0 });

        Assert.Equal(CompetitionLevel.HighCompetition, strict.Assess(10_000, 15_000).Level);
    }

    [Fact]
    public void Recommended_flag_matches_level()
    {
        Assert.True(_sut.Assess(10_000, 3_000).IsRecommended);
        Assert.False(_sut.Assess(10_000, 50_000).IsRecommended);
    }
}
