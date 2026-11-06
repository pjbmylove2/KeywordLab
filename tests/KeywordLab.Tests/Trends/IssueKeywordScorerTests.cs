using KeywordLab.Core.Trends;
using KeywordLab.Tests.Fakes;

namespace KeywordLab.Tests.Trends;

public class IssueKeywordScorerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 6, 12, 0, 0, TimeSpan.Zero);

    private static StoredTrend Trend(double hoursAgo, int traffic) =>
        new("키워드", Now.AddHours(-hoursAgo), Now, traffic, []);

    [Fact]
    public void Fresh_keyword_uses_google_traffic_as_demand()
    {
        var score = new IssueKeywordScorer().Score(Trend(0, 500), V.Of("키워드", 3000, 6000), 0, Now);

        Assert.Equal(DemandSource.GoogleTraffic, score.Source);
        Assert.Equal(500, score.Demand);
        Assert.Equal(1, score.TimeWeight, 6);
        Assert.Equal(500, score.Score, 6);
    }

    [Fact]
    public void Older_keyword_uses_naver_monthly_per_day()
    {
        var score = new IssueKeywordScorer().Score(Trend(60, 500), V.Of("키워드", 3000, 6000), 0, Now);

        Assert.Equal(DemandSource.NaverMonthly, score.Source);
        Assert.Equal(300, score.Demand, 6); // 9000 / 30
    }

    [Fact]
    public void Older_keyword_without_naver_volume_falls_back_to_google()
    {
        var score = new IssueKeywordScorer().Score(Trend(60, 500), null, 0, Now);

        Assert.Equal(DemandSource.GoogleTraffic, score.Source);
    }

    [Fact]
    public void Time_weight_halves_every_half_life()
    {
        var scorer = new IssueKeywordScorer(new IssueScoringRules { HalfLifeHours = 24 });

        Assert.Equal(0.5, scorer.Score(Trend(24, 100), null, 0, Now).TimeWeight, 6);
        Assert.Equal(0.25, scorer.Score(Trend(48, 100), null, 0, Now).TimeWeight, 6);
    }

    [Fact]
    public void More_documents_per_search_lowers_score()
    {
        var scorer = new IssueKeywordScorer();
        var volume = V.Of("키워드", 500, 500);

        var few = scorer.Score(Trend(1, 200), volume, 100, Now);
        var many = scorer.Score(Trend(1, 200), volume, 100_000, Now);

        Assert.True(many.Score < few.Score);
    }

    [Fact]
    public void Default_doc_penalty_is_plain_ratio()
    {
        // 오래된 키워드: 월검색 1,000 기준, 문서 4,000 → 비율 4 → 보정 1/5
        var score = new IssueKeywordScorer().Score(Trend(60, 100), V.Of("키워드", 500, 500), 4000, Now);

        Assert.Equal(1 / 5.0, score.DocFactor, 6);
    }

    [Fact]
    public void Smaller_exponent_softens_doc_penalty()
    {
        var scorer = new IssueKeywordScorer(new IssueScoringRules { DocPenaltyExponent = 0.5 });

        // 비율 4 → 제곱근 2 → 보정 1/3
        var score = scorer.Score(Trend(60, 100), V.Of("키워드", 500, 500), 4000, Now);

        Assert.Equal(1 / 3.0, score.DocFactor, 6);
    }

    [Fact]
    public void Fresh_keyword_compares_documents_with_larger_of_naver_and_google_month()
    {
        var scorer = new IssueKeywordScorer(new IssueScoringRules { DocPenaltyExponent = 1 });

        // 구글 500 × 30 = 15,000 > 네이버 1,730 → 문서 15,000 / 15,000 = 1 → 보정 1/2
        var fresh = scorer.Score(Trend(1, 500), V.Of("키워드", 730, 1000), 15_000, Now);
        // 신규가 지나면 네이버 월검색만 쓴다 → 15,000 / 1,730
        var old = scorer.Score(Trend(60, 500), V.Of("키워드", 730, 1000), 15_000, Now);

        Assert.Equal(0.5, fresh.DocFactor, 6);
        Assert.Equal(1 / (1 + 15_000 / 1730.0), old.DocFactor, 6);
    }

    [Fact]
    public void Future_first_seen_counts_as_zero_age()
    {
        var score = new IssueKeywordScorer().Score(Trend(-5, 100), null, 0, Now);

        Assert.Equal(0, score.AgeHours);
    }
}
