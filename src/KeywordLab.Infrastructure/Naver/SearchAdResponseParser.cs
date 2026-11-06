using System.Globalization;
using System.Text.Json;
using KeywordLab.Core.Models;

namespace KeywordLab.Infrastructure.Naver;

/// <summary>
/// /keywordstool 응답 파싱.
/// 검색 수 필드는 보통 숫자지만, 10 미만이면 "&lt; 10" 문자열로 온다.
/// </summary>
public static class SearchAdResponseParser
{
    public static IReadOnlyList<KeywordVolume> Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("keywordList", out var list) || list.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<KeywordVolume>();
        }

        var result = new List<KeywordVolume>(list.GetArrayLength());
        foreach (var item in list.EnumerateArray())
        {
            var keyword = item.TryGetProperty("relKeyword", out var k) ? k.GetString() : null;
            if (string.IsNullOrWhiteSpace(keyword))
            {
                continue;
            }

            result.Add(new KeywordVolume(
                keyword,
                ReadCount(item, "monthlyPcQcCnt"),
                ReadCount(item, "monthlyMobileQcCnt"),
                item.TryGetProperty("compIdx", out var c) ? c.GetString() : null));
        }

        return result;
    }

    internal static VolumeCount ReadCount(JsonElement item, string property)
    {
        if (!item.TryGetProperty(property, out var value))
        {
            return VolumeCount.Of(0);
        }

        switch (value.ValueKind)
        {
            case JsonValueKind.Number:
                return VolumeCount.Of(value.TryGetInt32(out var n) ? n : (int)Math.Min(value.GetDouble(), int.MaxValue));
            case JsonValueKind.String:
                var s = value.GetString()?.Trim() ?? string.Empty;
                if (s.StartsWith('<'))
                {
                    return VolumeCount.UnderTen;
                }

                return int.TryParse(s.Replace(",", string.Empty), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
                    ? VolumeCount.Of(parsed)
                    : VolumeCount.Of(0);
            default:
                return VolumeCount.Of(0);
        }
    }
}
