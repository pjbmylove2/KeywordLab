using KeywordLab.Core.Trends;

namespace KeywordLab.Core.Abstractions;

/// <summary>지금 뜨는 키워드 목록 (구글 트렌드 RSS).</summary>
public interface ITrendFeed
{
    Task<IReadOnlyList<TrendItem>> FetchAsync(CancellationToken cancellationToken = default);
}

/// <summary>검색어 자동완성. 비공식 주소라서 실패하면 빈 목록을 돌려준다 (예외를 던지지 않는다).</summary>
public interface IAutocompleteProvider
{
    Task<IReadOnlyList<string>> SuggestAsync(string query, CancellationToken cancellationToken = default);
}

/// <summary>수집한 트렌드를 쌓아 두는 곳. 처음 본 시각을 기억한다.</summary>
public interface ITrendStore
{
    Task UpsertAsync(IReadOnlyList<TrendItem> items, DateTimeOffset seenAt, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StoredTrend>> GetSinceAsync(DateTimeOffset since, CancellationToken cancellationToken = default);
}
