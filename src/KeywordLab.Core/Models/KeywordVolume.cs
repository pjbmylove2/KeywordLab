namespace KeywordLab.Core.Models;

/// <summary>키워드 하나의 월간 PC·모바일 검색 수.</summary>
/// <param name="Keyword">API가 돌려준 키워드 원문 (공백이 없는 형태).</param>
/// <param name="CompetitionIndex">광고 경쟁 정도 (낮음/중간/높음). 블로그 경쟁도와는 다르다.</param>
public sealed record KeywordVolume(string Keyword, VolumeCount Pc, VolumeCount Mobile, string? CompetitionIndex)
{
    public int TotalApprox => Pc.Approx + Mobile.Approx;

    public string TotalDisplay => Pc.IsUnderTen && Mobile.IsUnderTen
        ? "< 20"
        : TotalApprox.ToString("N0");
}
