namespace KeywordLab.Core.Models;

/// <summary>
/// 네이버 검색광고 API의 월간 검색 수 한 칸.
/// 검색 수가 10 미만이면 API가 숫자 대신 "&lt; 10" 문자열을 돌려주므로 그 상태를 따로 기억한다.
/// </summary>
public readonly record struct VolumeCount(int Value, bool IsUnderTen)
{
    /// <summary>"&lt; 10"을 순위 계산에 쓸 때의 대략값.</summary>
    public const int UnderTenApproximation = 5;

    public static VolumeCount UnderTen => new(0, true);

    public static VolumeCount Of(int value) => new(value, false);

    /// <summary>정렬·점수 계산용 근사치. "&lt; 10"은 5로 본다.</summary>
    public int Approx => IsUnderTen ? UnderTenApproximation : Value;

    public override string ToString() => IsUnderTen ? "< 10" : Value.ToString("N0");
}
