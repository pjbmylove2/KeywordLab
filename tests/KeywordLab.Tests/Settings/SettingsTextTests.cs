using KeywordLab.Core.Settings;
using KeywordLab.Core.Trends;

namespace KeywordLab.Tests.Settings;

public class SettingsTextTests
{
    [Fact]
    public void Parses_category_lines()
    {
        var (categories, errors) = SettingsText.ParseCategories("생활정보: 요금, 할인 ,지원금\r\n\n경제: 금리");

        Assert.Empty(errors);
        Assert.Equal(2, categories.Length);
        Assert.Equal("생활정보", categories[0].Name);
        Assert.Equal(["요금", "할인", "지원금"], categories[0].Words);
        Assert.Equal(["금리"], categories[1].Words);
    }

    [Fact]
    public void Reports_line_number_for_bad_lines()
    {
        var (categories, errors) = SettingsText.ParseCategories("생활정보: 요금\n콜론없음\n경제:\n: 이름없음\n생활정보: 할인");

        Assert.Single(categories);
        Assert.Equal(4, errors.Count);
        Assert.StartsWith("2번째 줄", errors[0]);
        Assert.StartsWith("3번째 줄", errors[1]);
        Assert.StartsWith("4번째 줄", errors[2]);
        Assert.Contains("두 번", errors[3]);
    }

    [Fact]
    public void Format_and_parse_round_trip()
    {
        CategoryWords[] original =
        [
            new() { Name = "생활정보", Words = ["요금", "할인"] },
            new() { Name = "경제", Words = ["금리"] },
        ];

        var (parsed, errors) = SettingsText.ParseCategories(SettingsText.FormatCategories(original));

        Assert.Empty(errors);
        Assert.Equal(original.Select(c => c.Name), parsed.Select(c => c.Name));
        Assert.Equal(original.SelectMany(c => c.Words), parsed.SelectMany(c => c.Words));
    }

    [Fact]
    public void Parses_phrases_by_comma_or_newline_without_duplicates()
    {
        var phrases = SettingsText.ParsePhrases("충격, 경악\n모르면 손해,, 충격 ");

        Assert.Equal(["충격", "경악", "모르면 손해"], phrases);
    }
}
