namespace KeywordLab.Core.Text;

/// <summary>
/// 키워드 비교용 정규화. 검색광고 API는 공백 없는 키워드를 받고 돌려주므로
/// "겨울철 전기요금"과 "겨울철전기요금"을 같은 키워드로 맞추는 데 쓴다.
/// </summary>
public static class KeywordNormalizer
{
    public static string Compact(string keyword)
    {
        ArgumentNullException.ThrowIfNull(keyword);
        var chars = keyword.Where(c => !char.IsWhiteSpace(c)).ToArray();
        return new string(chars).ToUpperInvariant();
    }

    /// <summary>앞뒤 공백 제거, 연속 공백을 한 칸으로.</summary>
    public static string Tidy(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var parts = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', parts);
    }
}
