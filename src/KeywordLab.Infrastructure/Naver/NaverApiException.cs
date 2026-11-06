using System.Net;

namespace KeywordLab.Infrastructure.Naver;

public sealed class NaverApiException : Exception
{
    public NaverApiException(string api, HttpStatusCode statusCode, string responseSnippet)
        : base($"{api} 호출 실패: {(int)statusCode} {statusCode}. 응답: {responseSnippet}")
    {
        Api = api;
        StatusCode = statusCode;
        ResponseSnippet = responseSnippet;
    }

    public string Api { get; }

    public HttpStatusCode StatusCode { get; }

    public string ResponseSnippet { get; }

    internal static async Task<NaverApiException> FromResponseAsync(string api, HttpResponseMessage response, CancellationToken ct)
    {
        var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        var snippet = body.Length > 300 ? body[..300] + "…" : body;
        return new NaverApiException(api, response.StatusCode, snippet);
    }
}
