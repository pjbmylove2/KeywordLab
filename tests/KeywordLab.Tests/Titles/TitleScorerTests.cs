using KeywordLab.Core.Titles;

namespace KeywordLab.Tests.Titles;

public class TitleScorerTests
{
    private static readonly TitleRequest Request = new("전기요금", ["할인"], new DateOnly(2026, 11, 6));

    private static readonly TitleScorer Scorer = new(new TitleScoringRules { BannedPhrases = ["충격", "모르면 손해"] });

    private static ScoredTitle Score(string text, BlogPlatform platform) =>
        Scorer.Score(new TitleCandidate(text, TitlePattern.Summary), Request, platform);

    [Fact]
    public void Tistory_gives_points_for_short_title_and_date()
    {
        var withDate = Score("전기요금 할인 2026년 11월 기준 정리", BlogPlatform.Tistory);
        var withoutDate = Score("전기요금 할인 총정리", BlogPlatform.Tistory);

        Assert.Contains(withDate.Reasons, r => r.Text == "연도·월 표기" && r.Points > 0);
        Assert.Contains(withDate.Reasons, r => r.Text.StartsWith("40자 이내", StringComparison.Ordinal) && r.Points > 0);
        Assert.True(withDate.Score > withoutDate.Score);
    }

    [Fact]
    public void Tistory_penalizes_title_over_40_chars()
    {
        var text = "전기요금 할인 " + new string('가', 40);

        var scored = Score(text, BlogPlatform.Tistory);

        Assert.Contains(scored.Reasons, r => r.Text.StartsWith("40자 초과", StringComparison.Ordinal) && r.Points < 0);
    }

    [Fact]
    public void Naver_has_no_length_rule()
    {
        var scored = Score("전기요금 할인 " + new string('가', 40), BlogPlatform.Naver);

        Assert.DoesNotContain(scored.Reasons, r => r.Text.Contains("자 초과", StringComparison.Ordinal) || r.Text.Contains("자 이내", StringComparison.Ordinal));
    }

    [Fact]
    public void Naver_penalizes_repeated_words()
    {
        var scored = Score("전기요금 할인 전기요금 할인 정리", BlogPlatform.Naver);

        Assert.Contains(scored.Reasons, r => r.Text == "'전기요금' 반복" && r.Points == TitleScorer.RepeatPenalty);
        Assert.Contains(scored.Reasons, r => r.Text == "'할인' 반복");
    }

    [Fact]
    public void Naver_penalizes_too_many_special_chars()
    {
        var scored = Score("전기요금 할인!!! 꼭 보세요★", BlogPlatform.Naver);

        Assert.Contains(scored.Reasons, r => r.Text.StartsWith("특수문자", StringComparison.Ordinal) && r.Points < 0);
    }

    [Theory]
    [InlineData(BlogPlatform.Tistory)]
    [InlineData(BlogPlatform.Naver)]
    public void Banned_phrases_from_settings_are_penalized(BlogPlatform platform)
    {
        var clean = Score("전기요금 할인 총정리", platform);
        var banned = Score("전기요금 할인 모르면 손해", platform);

        Assert.Contains(banned.Reasons, r => r.Text == "금지 표현 '모르면 손해'" && r.Points == TitleScorer.BannedPhrasePenalty);
        Assert.True(banned.Score < clean.Score);
    }

    [Fact]
    public void No_banned_phrases_when_settings_are_empty()
    {
        var scored = new TitleScorer().Score(new TitleCandidate("전기요금 충격 정리", TitlePattern.Summary), Request, BlogPlatform.Naver);

        Assert.DoesNotContain(scored.Reasons, r => r.Text.StartsWith("금지 표현", StringComparison.Ordinal));
    }

    [Fact]
    public void Keyword_not_at_front_is_penalized()
    {
        var front = Score("전기요금 할인 총정리", BlogPlatform.Naver);
        var back = Score("할인 받는 전기요금 정리", BlogPlatform.Naver);

        Assert.True(front.Score > back.Score);
        Assert.Contains(back.Reasons, r => r.Text == "메인 키워드가 맨 앞에 없음");
    }

    [Fact]
    public void Intent_keyword_gives_points()
    {
        var with = Score("전기요금 할인 총정리", BlogPlatform.Naver);
        var without = Score("전기요금 총정리", BlogPlatform.Naver);

        Assert.True(with.Score > without.Score);
    }

    [Fact]
    public void Score_is_clamped_between_0_and_100()
    {
        var scorer = new TitleScorer(new TitleScoringRules { BannedPhrases = ["가", "나", "다"] });

        var scored = scorer.Score(new TitleCandidate("가나다 정리", TitlePattern.Summary), Request, BlogPlatform.Naver);

        Assert.Equal(0, scored.Score);
    }

    [Fact]
    public void Rank_orders_by_score_then_shorter()
    {
        var ranked = Scorer.Rank(
            [
                new TitleCandidate("전기요금 총정리", TitlePattern.Summary),
                new TitleCandidate("전기요금 할인 2026년 11월 기준 정리", TitlePattern.Timely),
                new TitleCandidate("전기요금 할인 총정리", TitlePattern.Summary),
            ],
            Request,
            BlogPlatform.Tistory);

        Assert.Equal("전기요금 할인 2026년 11월 기준 정리", ranked[0].Text);
        Assert.Equal("전기요금 총정리", ranked[^1].Text);
    }
}
