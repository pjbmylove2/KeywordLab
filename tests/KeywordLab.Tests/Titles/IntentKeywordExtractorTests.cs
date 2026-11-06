using KeywordLab.Core.Titles;
using KeywordLab.Tests.Fakes;

namespace KeywordLab.Tests.Titles;

public class IntentKeywordExtractorTests
{
    [Fact]
    public void Takes_tails_of_related_keywords_that_start_with_main_keyword()
    {
        var related = new[]
        {
            V.Of("전기요금할인", 1310, 580),
            V.Of("겨울철전기요금", 100, 200),   // 앞에 붙는 말은 쓰지 않는다
            V.Of("전기요금절약", 50, 60),
            V.Of("탄소중립", 2530, 4610),       // 메인 키워드와 무관
        };

        var intents = IntentKeywordExtractor.Extract("전기요금", related);

        Assert.Equal(["할인", "절약"], intents);
    }

    [Fact]
    public void Main_keyword_with_spaces_matches_compact_related_keyword()
    {
        var intents = IntentKeywordExtractor.Extract("겨울철 전기요금", [V.Of("겨울철전기요금할인", 10, 10)]);

        Assert.Equal(["할인"], intents);
    }

    [Fact]
    public void Orders_by_volume_and_limits_count()
    {
        var related = new[]
        {
            V.Of("전기요금조회", 10, 10),
            V.Of("전기요금할인", 500, 500),
            V.Of("전기요금계산", 100, 100),
        };

        var intents = IntentKeywordExtractor.Extract("전기요금", related, max: 1);

        Assert.Equal(["할인"], intents);
    }

    [Fact]
    public void Skips_too_long_tails_and_same_keyword()
    {
        var related = new[]
        {
            V.Of("전기요금", 999, 999),
            V.Of("전기요금누진제구간별계산방법", 500, 500),
        };

        Assert.Empty(IntentKeywordExtractor.Extract("전기요금", related));
    }
}
