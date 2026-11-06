using KeywordLab.Infrastructure.Naver;

namespace KeywordLab.Tests.Infrastructure;

public class SearchAdResponseParserTests
{
    private const string Sample = """
    {
      "keywordList": [
        { "relKeyword": "겨울철전기요금", "monthlyPcQcCnt": 3000, "monthlyMobileQcCnt": 11900, "compIdx": "중간" },
        { "relKeyword": "전기요금아끼는꿀팁", "monthlyPcQcCnt": "< 10", "monthlyMobileQcCnt": 40, "compIdx": "낮음" },
        { "relKeyword": "희귀키워드", "monthlyPcQcCnt": "< 10", "monthlyMobileQcCnt": "< 10", "compIdx": "낮음" },
        { "relKeyword": "", "monthlyPcQcCnt": 1, "monthlyMobileQcCnt": 1 }
      ]
    }
    """;

    [Fact]
    public void Parses_numbers_and_under_ten_strings()
    {
        var list = SearchAdResponseParser.Parse(Sample);

        Assert.Equal(3, list.Count); // 빈 키워드는 건너뛴다

        Assert.Equal(14_900, list[0].TotalApprox);
        Assert.Equal("14,900", list[0].TotalDisplay);

        Assert.True(list[1].Pc.IsUnderTen);
        Assert.Equal("< 10", list[1].Pc.ToString());
        Assert.Equal(45, list[1].TotalApprox); // "< 10"은 5로 근사

        Assert.Equal("< 20", list[2].TotalDisplay);
        Assert.Equal("낮음", list[2].CompetitionIndex);
    }

    [Fact]
    public void Missing_keyword_list_returns_empty()
    {
        Assert.Empty(SearchAdResponseParser.Parse("{}"));
    }

    [Fact]
    public void Parses_numeric_strings_with_commas()
    {
        var list = SearchAdResponseParser.Parse("""{"keywordList":[{"relKeyword":"a","monthlyPcQcCnt":"1,200","monthlyMobileQcCnt":"30"}]}""");

        Assert.Equal(1_230, list[0].TotalApprox);
    }
}
