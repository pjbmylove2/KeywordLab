using System.Security.Cryptography;
using System.Text;

namespace KeywordLab.Infrastructure.Naver;

/// <summary>
/// 검색광고 API 요청 서명.
/// X-Signature = Base64( HMAC-SHA256( 비밀키, "{타임스탬프}.{메서드}.{경로}" ) )
/// 경로에는 쿼리 문자열을 넣지 않는다 (예: "/keywordstool").
/// </summary>
public static class NaverSearchAdSigner
{
    public static string Sign(string timestampMs, string method, string path, string secretKey)
    {
        ArgumentException.ThrowIfNullOrEmpty(timestampMs);
        ArgumentException.ThrowIfNullOrEmpty(method);
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentException.ThrowIfNullOrEmpty(secretKey);

        var message = $"{timestampMs}.{method.ToUpperInvariant()}.{path}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
        return Convert.ToBase64String(hash);
    }

    public static string NowTimestamp(TimeProvider? clock = null) =>
        (clock ?? TimeProvider.System).GetUtcNow().ToUnixTimeMilliseconds().ToString(System.Globalization.CultureInfo.InvariantCulture);
}
