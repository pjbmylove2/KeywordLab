using System.Net;
using KeywordLab.Infrastructure.Trends;
using Microsoft.Extensions.Logging.Abstractions;

namespace KeywordLab.Tests.Infrastructure;

public class TrendClientsTests
{
    // 2026-11-06 실제 응답 구조를 줄인 것
    private const string Rss = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <rss xmlns:atom="http://www.w3.org/2005/Atom" xmlns:ht="https://trends.google.com/trending/rss" version="2.0">
          <channel>
            <title>Daily Search Trends</title>
            <item>
              <title>과징금</title>
              <ht:approx_traffic>1,000+</ht:approx_traffic>
              <pubDate>Thu, 5 Nov 2026 23:30:00 -0800</pubDate>
              <ht:news_item>
                <ht:news_item_title>&apos;과징금&apos; 부과 발표</ht:news_item_title>
                <ht:news_item_url>https://news.example/1</ht:news_item_url>
                <ht:news_item_source>머니투데이</ht:news_item_source>
              </ht:news_item>
            </item>
            <item>
              <title>무덤</title>
              <ht:approx_traffic>200+</ht:approx_traffic>
              <pubDate>잘못된 날짜</pubDate>
            </item>
            <item><title> </title></item>
          </channel>
        </rss>
        """;

    [Fact]
    public void Parses_rss_items()
    {
        var items = GoogleTrendsRssParser.Parse(Rss);

        Assert.Equal(2, items.Count);
        var first = items[0];
        Assert.Equal("과징금", first.Keyword);
        Assert.Equal(1000, first.ApproxTraffic);
        Assert.Equal(new DateTimeOffset(2026, 11, 6, 7, 30, 0, TimeSpan.Zero), first.PublishedAt.ToUniversalTime());
        var news = Assert.Single(first.News);
        Assert.Equal("'과징금' 부과 발표", news.Title);
        Assert.Equal("머니투데이", news.Source);

        Assert.Equal(DateTimeOffset.MinValue, items[1].PublishedAt);
        Assert.Empty(items[1].News);
    }

    [Theory]
    [InlineData("200+", 200)]
    [InlineData("1,000+", 1000)]
    [InlineData("", 0)]
    [InlineData(null, 0)]
    public void Parses_traffic(string? value, int expected)
    {
        Assert.Equal(expected, GoogleTrendsRssParser.ParseTraffic(value));
    }

    [Fact]
    public void Empty_rss_gives_no_items()
    {
        Assert.Empty(GoogleTrendsRssParser.Parse(""));
    }

    [Fact]
    public void Parses_naver_autocomplete_and_drops_query_itself()
    {
        const string json = """{"query":["전기요금"],"items":[[["전기요금 조회"],["전기요금"],["전기요금 할인"]]]}""";

        var list = NaverAutocompleteClient.Parse(json, "전기요금");

        Assert.Equal(["전기요금 조회", "전기요금 할인"], list);
    }

    [Fact]
    public void Unexpected_autocomplete_shape_gives_empty()
    {
        Assert.Empty(NaverAutocompleteClient.Parse("""{"items":[]}""", "a"));
        Assert.Empty(NaverAutocompleteClient.Parse("""{"other":1}""", "a"));
    }

    [Fact]
    public async Task Autocomplete_failure_returns_empty_instead_of_throwing()
    {
        var client = new NaverAutocompleteClient(new HttpClient(new StatusHandler(HttpStatusCode.Forbidden, "")), NullLogger<NaverAutocompleteClient>.Instance);

        Assert.Empty(await client.SuggestAsync("전기요금"));
    }

    [Fact]
    public async Task Autocomplete_broken_json_returns_empty()
    {
        var client = new NaverAutocompleteClient(new HttpClient(new StatusHandler(HttpStatusCode.OK, "<html>")), NullLogger<NaverAutocompleteClient>.Instance);

        Assert.Empty(await client.SuggestAsync("전기요금"));
    }

    [Fact]
    public async Task Rss_error_status_throws_http_exception()
    {
        var client = new GoogleTrendsRssClient(new HttpClient(new StatusHandler(HttpStatusCode.TooManyRequests, "")), Microsoft.Extensions.Options.Options.Create(new TrendsOptions()));

        await Assert.ThrowsAsync<HttpRequestException>(() => client.FetchAsync());
    }

    private sealed class StatusHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
    }
}
