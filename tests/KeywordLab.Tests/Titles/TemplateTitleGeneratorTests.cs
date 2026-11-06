using KeywordLab.Core.Titles;

namespace KeywordLab.Tests.Titles;

public class TemplateTitleGeneratorTests
{
    private static readonly DateOnly Today = new(2026, 11, 6);

    [Fact]
    public async Task Every_title_starts_with_the_main_keyword_as_is()
    {
        var titles = await new TemplateTitleGenerator().GenerateAsync(new TitleRequest("겨울철 전기요금", ["할인", "절약"], Today));

        Assert.NotEmpty(titles);
        Assert.All(titles, t => Assert.StartsWith("겨울철 전기요금", t.Text));
    }

    [Fact]
    public async Task Makes_all_three_patterns()
    {
        var titles = await new TemplateTitleGenerator().GenerateAsync(new TitleRequest("전기요금", [], Today));

        Assert.Contains(titles, t => t.Pattern == TitlePattern.Summary);
        Assert.Contains(titles, t => t.Pattern == TitlePattern.Question);
        Assert.Contains(titles, t => t.Pattern == TitlePattern.Timely);
    }

    [Fact]
    public async Task Timely_titles_use_year_and_month_of_today()
    {
        var titles = await new TemplateTitleGenerator().GenerateAsync(new TitleRequest("전기요금", [], Today));

        Assert.All(titles.Where(t => t.Pattern == TitlePattern.Timely), t => Assert.Contains("2026년 11월", t.Text));
    }

    [Fact]
    public async Task Uses_at_most_two_intent_keywords()
    {
        var titles = await new TemplateTitleGenerator().GenerateAsync(new TitleRequest("전기요금", ["할인", "절약", "신청"], Today));

        Assert.Contains(titles, t => t.Text.StartsWith("전기요금 할인·절약", StringComparison.Ordinal));
        Assert.DoesNotContain(titles, t => t.Text.Contains("신청", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Titles_are_unique()
    {
        var titles = await new TemplateTitleGenerator().GenerateAsync(new TitleRequest("전기요금", ["할인", "할인"], Today));

        Assert.Equal(titles.Count, titles.Select(t => t.Text).Distinct().Count());
    }

    [Fact]
    public async Task Empty_keyword_gives_no_titles()
    {
        var titles = await new TemplateTitleGenerator().GenerateAsync(new TitleRequest("  ", ["할인"], Today));

        Assert.Empty(titles);
    }
}
