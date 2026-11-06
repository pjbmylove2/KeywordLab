using KeywordLab.Core.Abstractions;
using KeywordLab.Core.Models;
using KeywordLab.Core.Text;
using Microsoft.Extensions.Options;

namespace KeywordLab.Infrastructure.Naver;

/// <summary>네이버 검색광고 API 키워드 도구(/keywordstool) 클라이언트.</summary>
public sealed class NaverSearchAdClient : IKeywordVolumeProvider
{
    /// <summary>API가 한 번에 받는 힌트 키워드 최대 개수.</summary>
    public const int MaxHintsPerRequest = 5;

    private const string KeywordToolPath = "/keywordstool";

    /// <summary>연속 호출 사이 간격. 호출 제한에 걸리지 않게 여유를 둔다.</summary>
    private static readonly TimeSpan DelayBetweenBatches = TimeSpan.FromMilliseconds(300);

    private readonly HttpClient _http;
    private readonly NaverSearchAdOptions _options;
    private readonly TimeProvider _clock;

    public NaverSearchAdClient(HttpClient http, IOptions<NaverSearchAdOptions> options, TimeProvider clock)
    {
        _http = http;
        _options = options.Value;
        _clock = clock;
    }

    public async Task<IReadOnlyList<KeywordVolume>> GetVolumesAsync(IReadOnlyList<string> hintKeywords, CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured)
        {
            throw new InvalidOperationException("네이버 검색광고 API 키가 설정되지 않았습니다. README의 '사용자 비밀 설정'을 참고하세요.");
        }

        // API는 공백이 들어간 키워드를 받지 않으므로 공백을 제거해서 보낸다.
        var hints = hintKeywords
            .Select(KeywordNormalizer.Compact)
            .Where(h => h.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var all = new List<KeywordVolume>();
        for (var i = 0; i < hints.Count; i += MaxHintsPerRequest)
        {
            if (i > 0)
            {
                await Task.Delay(DelayBetweenBatches, cancellationToken).ConfigureAwait(false);
            }

            var batch = hints.Skip(i).Take(MaxHintsPerRequest);
            all.AddRange(await RequestAsync(batch, cancellationToken).ConfigureAwait(false));
        }

        // 여러 배치에서 같은 연관 키워드가 중복으로 올 수 있다.
        return all
            .GroupBy(v => KeywordNormalizer.Compact(v.Keyword), StringComparer.Ordinal)
            .Select(g => g.First())
            .ToList();
    }

    private async Task<IReadOnlyList<KeywordVolume>> RequestAsync(IEnumerable<string> hints, CancellationToken ct)
    {
        var query = "hintKeywords=" + Uri.EscapeDataString(string.Join(',', hints)) + "&showDetail=1";
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{_options.BaseUrl.TrimEnd('/')}{KeywordToolPath}?{query}");

        var timestamp = NaverSearchAdSigner.NowTimestamp(_clock);
        request.Headers.Add("X-Timestamp", timestamp);
        request.Headers.Add("X-API-KEY", _options.AccessLicense);
        request.Headers.Add("X-Customer", _options.CustomerId);
        request.Headers.Add("X-Signature", NaverSearchAdSigner.Sign(timestamp, "GET", KeywordToolPath, _options.SecretKey));

        using var response = await _http.SendAsync(request, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw await NaverApiException.FromResponseAsync("검색광고 키워드 도구", response, ct).ConfigureAwait(false);
        }

        var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        return SearchAdResponseParser.Parse(json);
    }
}
