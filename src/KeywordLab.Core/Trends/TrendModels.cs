namespace KeywordLab.Core.Trends;

public sealed record TrendNews(string Title, string Url, string Source);

/// <summary>구글 트렌드 RSS 항목 하나.</summary>
/// <param name="ApproxTraffic">예상 트래픽 ("200+" → 200).</param>
/// <param name="PublishedAt">RSS의 pubDate.</param>
public sealed record TrendItem(string Keyword, int ApproxTraffic, DateTimeOffset PublishedAt, IReadOnlyList<TrendNews> News);

/// <summary>여러 번 수집한 결과를 키워드별로 합친 것.</summary>
/// <param name="FirstSeenAt">처음 등장한 시각 (경과 시간 가중치의 기준).</param>
/// <param name="MaxTraffic">지금까지 본 예상 트래픽 중 최댓값.</param>
public sealed record StoredTrend(string Keyword, DateTimeOffset FirstSeenAt, DateTimeOffset LastSeenAt, int MaxTraffic, IReadOnlyList<TrendNews> News);
