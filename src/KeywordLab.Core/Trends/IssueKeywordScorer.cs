using KeywordLab.Core.Models;

namespace KeywordLab.Core.Trends;

/// <summary>자동 모드 점수 기준. 처음 값은 임시 기준이며 써 보면서 조정한다 (appsettings.json "IssueScoring").</summary>
public sealed class IssueScoringRules
{
    public const string SectionName = "IssueScoring";

    /// <summary>등장 후 이 시간 안이면 신규 키워드로 보고 구글 예상 트래픽을 수요로 쓴다.</summary>
    public double FreshHours { get; set; } = 48;

    /// <summary>경과 시간 가중치가 절반이 되는 시간.</summary>
    public double HalfLifeHours { get; set; } = 24;

    /// <summary>이 시간 안에 등장한 키워드만 오늘의 이슈 키워드로 본다.</summary>
    public double WindowHours { get; set; } = 72;

    /// <summary>
    /// 문서 수 보정의 세기. 보정 = 1 / (1 + (문서 수 / 월검색) ^ 이 값).
    /// 1이면 비율 그대로(세게), 0.5면 제곱근(완만하게). 문서가 넘치는 일반 단어("죽음" 등)를 확실히 내리려고 1로 시작한다.
    /// </summary>
    public double DocPenaltyExponent { get; set; } = 1;
}

public enum DemandSource
{
    GoogleTraffic,
    NaverMonthly,
}

/// <param name="Demand">하루 기준 수요. 네이버 월검색은 30으로 나눠 구글 예상 트래픽과 단위를 맞춘다.</param>
/// <param name="DocFactor">문서 수 보정 = 1 / (1 + (블로그 문서 수 / 월검색) ^ DocPenaltyExponent).</param>
public sealed record IssueScore(double Score, DemandSource Source, double Demand, double AgeHours, double TimeWeight, double DocFactor);

/// <summary>점수 = 수요 × 경과 시간 가중치 × 문서 수 보정 (docs/DECISIONS.md "자동 모드 키워드 점수").</summary>
public sealed class IssueKeywordScorer
{
    public const double DaysPerMonth = 30;

    private readonly IssueScoringRules _rules;

    public IssueKeywordScorer(IssueScoringRules? rules = null)
    {
        _rules = rules ?? new IssueScoringRules();
    }

    public IssueScoringRules Rules => _rules;

    /// <param name="naver">네이버 검색량. 검색광고 API에 아직 없으면 null.</param>
    public IssueScore Score(StoredTrend trend, KeywordVolume? naver, long blogDocuments, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(trend);

        var ageHours = Math.Max(0, (now - trend.FirstSeenAt).TotalHours);

        var useGoogle = ageHours < _rules.FreshHours || naver is null;
        var demand = useGoogle ? trend.MaxTraffic : naver!.TotalApprox / DaysPerMonth;

        var timeWeight = Math.Pow(0.5, ageHours / _rules.HalfLifeHours);

        // 네이버 월검색은 지난달 기준이라 막 뜬 키워드의 수요를 못 따라간다.
        // 신규 키워드는 구글 트래픽을 한 달치로 늘린 값과 월검색 중 큰 쪽으로 비교한다.
        var googleMonthly = trend.MaxTraffic * DaysPerMonth;
        var monthly = naver is null ? googleMonthly
            : useGoogle ? Math.Max(naver.TotalApprox, googleMonthly)
            : naver.TotalApprox;
        var ratio = blogDocuments / Math.Max(monthly, 1);
        var docFactor = 1 / (1 + Math.Pow(ratio, _rules.DocPenaltyExponent));

        return new IssueScore(
            demand * timeWeight * docFactor,
            useGoogle ? DemandSource.GoogleTraffic : DemandSource.NaverMonthly,
            demand,
            ageHours,
            timeWeight,
            docFactor);
    }

    /// <summary>네이버 조회 전에 후보를 줄일 때 쓰는 임시 순위값 (구글 트래픽 × 경과 시간 가중치).</summary>
    public double Provisional(StoredTrend trend, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(trend);
        var ageHours = Math.Max(0, (now - trend.FirstSeenAt).TotalHours);
        return trend.MaxTraffic * Math.Pow(0.5, ageHours / _rules.HalfLifeHours);
    }
}
