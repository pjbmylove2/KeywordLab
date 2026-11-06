using System.Text.Json;
using KeywordLab.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace KeywordLab.Infrastructure.Trends;

/// <summary>
/// 네이버 자동완성 (ac.search.naver.com). 공식 API가 아니라서 예고 없이 막힐 수 있다.
/// 실패하면 빈 목록을 돌려주고 로그만 남긴다 (docs/DECISIONS.md "자동완성").
/// </summary>
public sealed class NaverAutocompleteClient : IAutocompleteProvider
{
    public const string BaseUrl = "https://ac.search.naver.com/nx/ac";

    private readonly HttpClient _http;
    private readonly ILogger<NaverAutocompleteClient> _logger;

    public NaverAutocompleteClient(HttpClient http, ILogger<NaverAutocompleteClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<IReadOnlyList<string>> SuggestAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var url = $"{BaseUrl}?q={Uri.EscapeDataString(query.Trim())}&st=100&r_format=json&q_enc=UTF-8&r_enc=UTF-8";
        try
        {
            using var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("네이버 자동완성 응답 {Status}", (int)response.StatusCode);
                return [];
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return Parse(json, query);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException
                                   || (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            _logger.LogWarning(ex, "네이버 자동완성 실패: {Query}", query);
            return [];
        }
    }

    /// <summary>응답 형식: {"items":[[["전기요금 조회"],["전기요금 계산기"], ...]]}. 검색어 자신은 뺀다.</summary>
    public static IReadOnlyList<string> Parse(string json, string query)
    {
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array || items.GetArrayLength() == 0)
        {
            return [];
        }

        var first = items[0];
        if (first.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var self = query.Trim();
        var result = new List<string>();
        foreach (var entry in first.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Array || entry.GetArrayLength() == 0 || entry[0].ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var text = entry[0].GetString()!.Trim();
            if (text.Length > 0 && !string.Equals(text, self, StringComparison.OrdinalIgnoreCase) && !result.Contains(text))
            {
                result.Add(text);
            }
        }

        return result;
    }
}
