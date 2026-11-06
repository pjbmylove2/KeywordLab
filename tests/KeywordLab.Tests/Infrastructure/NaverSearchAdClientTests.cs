using System.Net;
using KeywordLab.Infrastructure.Naver;
using Microsoft.Extensions.Options;

namespace KeywordLab.Tests.Infrastructure;

public class NaverSearchAdClientTests
{
    private static NaverSearchAdOptions Options() => new()
    {
        BaseUrl = "https://api.example",
        CustomerId = "123",
        AccessLicense = "license",
        SecretKey = "secret",
    };

    [Fact]
    public async Task Sends_signed_headers_and_compact_hints()
    {
        var handler = new RecordingHandler("""{"keywordList":[{"relKeyword":"겨울철전기요금","monthlyPcQcCnt":10,"monthlyMobileQcCnt":20}]}""");
        var client = new NaverSearchAdClient(new HttpClient(handler), Microsoft.Extensions.Options.Options.Create(Options()), TimeProvider.System);

        var result = await client.GetVolumesAsync(new[] { "겨울철 전기요금" });

        var req = Assert.Single(handler.Requests);
        Assert.Equal("/keywordstool", req.RequestUri!.AbsolutePath);
        Assert.Contains("hintKeywords=" + Uri.EscapeDataString("겨울철전기요금"), req.RequestUri.Query);
        Assert.Equal("license", req.Headers.GetValues("X-API-KEY").Single());
        Assert.Equal("123", req.Headers.GetValues("X-Customer").Single());

        var ts = req.Headers.GetValues("X-Timestamp").Single();
        Assert.Equal(NaverSearchAdSigner.Sign(ts, "GET", "/keywordstool", "secret"), req.Headers.GetValues("X-Signature").Single());

        Assert.Equal(30, result.Single().TotalApprox);
    }

    [Fact]
    public async Task Splits_hints_into_batches_of_five_and_dedupes()
    {
        var handler = new RecordingHandler("""{"keywordList":[{"relKeyword":"공통","monthlyPcQcCnt":1,"monthlyMobileQcCnt":1}]}""");
        var client = new NaverSearchAdClient(new HttpClient(handler), Microsoft.Extensions.Options.Options.Create(Options()), TimeProvider.System);

        var hints = Enumerable.Range(1, 7).Select(i => $"키워드{i}").ToArray();
        var result = await client.GetVolumesAsync(hints);

        Assert.Equal(2, handler.Requests.Count);
        Assert.Single(result); // 두 배치에서 온 같은 키워드는 하나로 합친다
    }

    [Fact]
    public async Task Error_status_throws_naver_api_exception()
    {
        var handler = new RecordingHandler("""{"title":"Forbidden"}""", HttpStatusCode.Forbidden);
        var client = new NaverSearchAdClient(new HttpClient(handler), Microsoft.Extensions.Options.Options.Create(Options()), TimeProvider.System);

        var ex = await Assert.ThrowsAsync<NaverApiException>(() => client.GetVolumesAsync(new[] { "a" }));

        Assert.Equal(HttpStatusCode.Forbidden, ex.StatusCode);
    }

    [Fact]
    public async Task Missing_keys_throws_before_calling()
    {
        var handler = new RecordingHandler("{}");
        var client = new NaverSearchAdClient(new HttpClient(handler), Microsoft.Extensions.Options.Options.Create(new NaverSearchAdOptions()), TimeProvider.System);

        await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetVolumesAsync(new[] { "a" }));
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
