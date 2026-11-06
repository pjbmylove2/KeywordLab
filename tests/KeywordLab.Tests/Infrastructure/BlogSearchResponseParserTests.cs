using KeywordLab.Infrastructure.Naver;

namespace KeywordLab.Tests.Infrastructure;

public class BlogSearchResponseParserTests
{
    [Fact]
    public void Parses_total_and_cleans_titles()
    {
        const string json = """
        {
          "total": 8200,
          "items": [
            { "title": "<b>겨울철 전기요금</b> 아끼는 법 &amp; 할인", "link": "https://blog.naver.com/a/1", "bloggername": "생활<b>정보</b>", "postdate": "20261101" }
          ]
        }
        """;

        var result = BlogSearchResponseParser.Parse(json);

        Assert.Equal(8200, result.Total);
        Assert.Equal("겨울철 전기요금 아끼는 법 & 할인", result.TopPosts[0].Title);
        Assert.Equal("생활정보", result.TopPosts[0].BloggerName);
    }

    [Fact]
    public void Missing_items_returns_zero()
    {
        var result = BlogSearchResponseParser.Parse("{}");

        Assert.Equal(0, result.Total);
        Assert.Empty(result.TopPosts);
    }
}
