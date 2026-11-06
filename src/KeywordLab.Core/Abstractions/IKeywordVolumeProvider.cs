using KeywordLab.Core.Models;

namespace KeywordLab.Core.Abstractions;

/// <summary>키워드 월간 검색 수 제공자 (구현: 네이버 검색광고 API).</summary>
public interface IKeywordVolumeProvider
{
    /// <summary>
    /// 힌트 키워드들의 검색 수와 함께, API가 추천하는 연관 키워드까지 모두 돌려준다.
    /// </summary>
    Task<IReadOnlyList<KeywordVolume>> GetVolumesAsync(IReadOnlyList<string> hintKeywords, CancellationToken cancellationToken = default);
}
