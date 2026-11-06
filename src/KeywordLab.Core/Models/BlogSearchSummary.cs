namespace KeywordLab.Core.Models;

/// <summary>네이버 블로그 검색 결과 요약. Total이 경쟁 문서 수다.</summary>
public sealed record BlogSearchSummary(long Total, IReadOnlyList<BlogPostTitle> TopPosts)
{
    public static BlogSearchSummary Empty { get; } = new(0, Array.Empty<BlogPostTitle>());
}

public sealed record BlogPostTitle(string Title, string Link, string BloggerName, string PostDate);
