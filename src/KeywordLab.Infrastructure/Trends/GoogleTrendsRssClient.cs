using System.Globalization;
using System.Xml.Linq;
using KeywordLab.Core.Abstractions;
using KeywordLab.Core.Trends;
using Microsoft.Extensions.Options;

namespace KeywordLab.Infrastructure.Trends;

public sealed class TrendsOptions
{
    public const string SectionName = "Trends";

    /// <summary>docs/DECISIONS.md "구글 트렌드 RSS" 참고. geo=KR만 붙인다.</summary>
    public string RssUrl { get; set; } = "https://trends.google.co.kr/trending/rss?geo=KR";
}

/// <summary>구글 트렌드 실시간 인기 검색어 RSS.</summary>
public sealed class GoogleTrendsRssClient : ITrendFeed
{
    private readonly HttpClient _http;
    private readonly TrendsOptions _options;

    public GoogleTrendsRssClient(HttpClient http, IOptions<TrendsOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<TrendItem>> FetchAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync(_options.RssUrl, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"구글 트렌드 RSS 응답이 {(int)response.StatusCode}입니다.", null, response.StatusCode);
        }

        var xml = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return GoogleTrendsRssParser.Parse(xml);
    }
}

public static class GoogleTrendsRssParser
{
    private static readonly XNamespace Ht = "https://trends.google.com/trending/rss";

    public static IReadOnlyList<TrendItem> Parse(string xml)
    {
        ArgumentNullException.ThrowIfNull(xml);
        if (string.IsNullOrWhiteSpace(xml))
        {
            return [];
        }

        var doc = XDocument.Parse(xml);
        var result = new List<TrendItem>();
        foreach (var item in doc.Descendants("item"))
        {
            var keyword = (item.Element("title")?.Value ?? string.Empty).Trim();
            if (keyword.Length == 0)
            {
                continue;
            }

            var news = item.Elements(Ht + "news_item")
                .Select(n => new TrendNews(
                    (n.Element(Ht + "news_item_title")?.Value ?? string.Empty).Trim(),
                    (n.Element(Ht + "news_item_url")?.Value ?? string.Empty).Trim(),
                    (n.Element(Ht + "news_item_source")?.Value ?? string.Empty).Trim()))
                .Where(n => n.Title.Length > 0)
                .ToList();

            result.Add(new TrendItem(
                keyword,
                ParseTraffic(item.Element(Ht + "approx_traffic")?.Value),
                ParseDate(item.Element("pubDate")?.Value),
                news));
        }

        return result;
    }

    /// <summary>"1,000+" → 1000. 숫자가 없으면 0.</summary>
    public static int ParseTraffic(string? value)
    {
        var digits = new string((value ?? string.Empty).Where(char.IsAsciiDigit).ToArray());
        return int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var n) ? n : 0;
    }

    /// <summary>"Thu, 5 Nov 2026 23:30:00 -0800" 형식. 못 읽으면 DateTimeOffset.MinValue.</summary>
    public static DateTimeOffset ParseDate(string? value)
    {
        return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var date)
            ? date
            : DateTimeOffset.MinValue;
    }
}
