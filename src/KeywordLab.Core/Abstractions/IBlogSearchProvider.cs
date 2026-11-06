using KeywordLab.Core.Models;

namespace KeywordLab.Core.Abstractions;

/// <summary>블로그 문서 수와 상위 글 제목 제공자 (구현: 네이버 검색 API).</summary>
public interface IBlogSearchProvider
{
    Task<BlogSearchSummary> SearchAsync(string query, int display = 5, CancellationToken cancellationToken = default);
}
