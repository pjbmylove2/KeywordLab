using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using KeywordLab.Core.Models;

namespace KeywordLab.Infrastructure.Naver;

/// <summary>/v1/search/blog.json 응답 파싱. 제목의 &lt;b&gt; 강조 태그와 HTML 엔티티를 정리한다.</summary>
public static partial class BlogSearchResponseParser
{
    public static BlogSearchSummary Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var total = root.TryGetProperty("total", out var t) && t.TryGetInt64(out var n) ? n : 0;
        var posts = new List<BlogPostTitle>();

        if (root.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in items.EnumerateArray())
            {
                posts.Add(new BlogPostTitle(
                    CleanText(Get(item, "title")),
                    Get(item, "link"),
                    CleanText(Get(item, "bloggername")),
                    Get(item, "postdate")));
            }
        }

        return new BlogSearchSummary(total, posts);
    }

    public static string CleanText(string html) =>
        WebUtility.HtmlDecode(TagPattern().Replace(html, string.Empty)).Trim();

    private static string Get(JsonElement item, string name) =>
        item.TryGetProperty(name, out var v) ? v.GetString() ?? string.Empty : string.Empty;

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex TagPattern();
}
