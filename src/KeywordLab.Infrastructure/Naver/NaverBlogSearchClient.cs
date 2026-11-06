using KeywordLab.Core.Abstractions;
using KeywordLab.Core.Models;
using Microsoft.Extensions.Options;

namespace KeywordLab.Infrastructure.Naver;

/// <summary>네이버 검색 API 블로그 검색 클라이언트. 문서 수(total)와 상위 글 제목을 가져온다.</summary>
public sealed class NaverBlogSearchClient : IBlogSearchProvider
{
    private readonly HttpClient _http;
    private readonly NaverOpenApiOptions _options;

    public NaverBlogSearchClient(HttpClient http, IOptions<NaverOpenApiOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public async Task<BlogSearchSummary> SearchAsync(string query, int display = 5, CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured)
        {
            throw new InvalidOperationException("네이버 검색 API 키가 설정되지 않았습니다. README의 '사용자 비밀 설정'을 참고하세요.");
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            return BlogSearchSummary.Empty;
        }

        var size = Math.Clamp(display, 1, 100);
        var url = $"{_options.BaseUrl.TrimEnd('/')}/v1/search/blog.json?query={Uri.EscapeDataString(query)}&display={size}&sort=sim";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("X-Naver-Client-Id", _options.ClientId);
        request.Headers.Add("X-Naver-Client-Secret", _options.ClientSecret);

        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw await NaverApiException.FromResponseAsync("블로그 검색", response, cancellationToken).ConfigureAwait(false);
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return BlogSearchResponseParser.Parse(json);
    }
}
