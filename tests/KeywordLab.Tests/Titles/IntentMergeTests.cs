using KeywordLab.Core.Titles;

namespace KeywordLab.Tests.Titles;

public class IntentMergeTests
{
    [Fact]
    public void Takes_spaced_tail_after_main_keyword()
    {
        var intents = IntentKeywordExtractor.Merge("전기요금", ["전기요금 조회", "한전 전기요금", "전기요금 계산기"], []);

        Assert.Equal(["조회", "계산기"], intents);
    }

    [Fact]
    public void Autocomplete_first_then_related_without_duplicates()
    {
        var intents = IntentKeywordExtractor.Merge("전기요금", ["전기요금 할인"], ["할인", "절약"]);

        Assert.Equal(["할인", "절약"], intents);
    }

    [Fact]
    public void Skips_too_long_tail()
    {
        var intents = IntentKeywordExtractor.Merge("전기요금", ["전기요금 고객센터 전화번호 안내"], ["할인"]);

        Assert.Equal(["할인"], intents);
    }

    [Fact]
    public void Works_with_spaced_main_keyword()
    {
        var intents = IntentKeywordExtractor.Merge("겨울철 전기요금", ["겨울철  전기요금 절약"], []);

        Assert.Equal(["절약"], intents);
    }
}
