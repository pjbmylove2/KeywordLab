using System.Net;
using KeywordLab.Infrastructure.Naver;

namespace KeywordLab.Tests.Infrastructure;

public class NaverBlogSearchClientTests
{
    private static NaverOpenApiOptions Options() => new()
    {
        ClientId = "id",
        ClientSecret = "secret",
    };

    [Fact]
    public async Task Calls_api_hub_blog_endpoint_with_ncp_headers()
    {
        var handler = new RecordingHandler("""{"total":1234,"items":[{"title":"<b>커피</b> 추천"}]}""");
        var client = new NaverBlogSearchClient(new HttpClient(handler), Microsoft.Extensions.Options.Options.Create(Options()));

        var result = await client.SearchAsync("커피", 3);

        var req = Assert.Single(handler.Requests);
        Assert.Equal("naverapihub.apigw.ntruss.com", req.RequestUri!.Host);
        Assert.Equal("/search/v1/blog", req.RequestUri.AbsolutePath);
        Assert.Contains("query=" + Uri.EscapeDataString("커피"), req.RequestUri.Query);
        Assert.Contains("display=3", req.RequestUri.Query);
        Assert.Equal("id", req.Headers.GetValues("X-NCP-APIGW-API-KEY-ID").Single());
        Assert.Equal("secret", req.Headers.GetValues("X-NCP-APIGW-API-KEY").Single());
        Assert.False(req.Headers.Contains("X-Naver-Client-Id"));
        Assert.Equal(1234, result.Total);
    }

    [Fact]
    public async Task Error_status_throws_naver_api_exception()
    {
        var handler = new RecordingHandler("""{"error":{"errorCode":"200","message":"Authentication Failed"}}""", HttpStatusCode.Unauthorized);
        var client = new NaverBlogSearchClient(new HttpClient(handler), Microsoft.Extensions.Options.Options.Create(Options()));

        var ex = await Assert.ThrowsAsync<NaverApiException>(() => client.SearchAsync("커피"));

        Assert.Equal(HttpStatusCode.Unauthorized, ex.StatusCode);
    }

    [Fact]
    public async Task Missing_keys_throws_before_calling()
    {
        var handler = new RecordingHandler("{}");
        var client = new NaverBlogSearchClient(new HttpClient(handler), Microsoft.Extensions.Options.Options.Create(new NaverOpenApiOptions()));

        await Assert.ThrowsAsync<InvalidOperationException>(() => client.SearchAsync("커피"));
        Assert.Empty(handler.Requests);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly string _body;
        private readonly HttpStatusCode _status;

        public RecordingHandler(string body, HttpStatusCode status = HttpStatusCode.OK)
        {
            _body = body;
            _status = status;
        }

        public List<HttpRequestMessage> Requests { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(new HttpResponseMessage(_status) { Content = new StringContent(_body) });
        }
    }
}
