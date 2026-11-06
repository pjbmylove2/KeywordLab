using KeywordLab.Core.Titles;

namespace KeywordLab.Core.Trends;

/// <summary>카테고리 하나와 그 카테고리로 볼 단어 목록.</summary>
public sealed class CategoryWords
{
    public string Name { get; set; } = string.Empty;

    public string[] Words { get; set; } = [];
}

/// <summary>블로그 하나와 허용 카테고리. 네이버는 C-Rank 주제 일관성 때문에 좁게 잡는다.</summary>
public sealed class BlogProfile
{
    public string Name { get; set; } = string.Empty;

    public BlogPlatform Platform { get; set; } = BlogPlatform.Tistory;

    public CategoryWords[] Categories { get; set; } = [];
}

/// <summary>블로그 목록 (appsettings.json "Blogs"). 3-2단계에서 설정 화면으로 옮긴다.</summary>
public sealed class BlogOptions
{
    public const string SectionName = "Blogs";

    public BlogProfile[] Items { get; set; } = [];
}

/// <summary>키워드가 블로그의 허용 카테고리에 드는지 단어 목록으로 판별한다 (LLM 없음).</summary>
public static class CategoryMatcher
{
    /// <param name="texts">키워드, 자동완성어, 관련 뉴스 제목 등 판별에 쓸 글.</param>
    /// <returns>처음 걸린 카테고리 이름. 어느 것에도 안 걸리면 null (카테고리 밖).</returns>
    public static string? Match(BlogProfile blog, IEnumerable<string> texts)
    {
        ArgumentNullException.ThrowIfNull(blog);
        ArgumentNullException.ThrowIfNull(texts);

        var list = texts.Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
        foreach (var category in blog.Categories)
        {
            foreach (var word in category.Words.Where(w => !string.IsNullOrWhiteSpace(w)))
            {
                if (list.Any(t => t.Contains(word.Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    return category.Name;
                }
            }
        }

        return null;
    }
}
