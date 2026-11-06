namespace KeywordLab.Infrastructure.Naver;

/// <summary>
/// 네이버 검색광고 API 설정 (searchad.naver.com → 도구 → API 사용 관리).
/// 실제 값은 VS의 "사용자 비밀 관리"(secrets.json)에만 넣는다. 저장소에 커밋 금지.
/// </summary>
public sealed class NaverSearchAdOptions
{
    public const string SectionName = "Naver:SearchAd";

    public string BaseUrl { get; set; } = "https://api.searchad.naver.com";

    /// <summary>CUSTOMER_ID</summary>
    public string CustomerId { get; set; } = string.Empty;

    /// <summary>엑세스라이선스 (API_KEY)</summary>
    public string AccessLicense { get; set; } = string.Empty;

    /// <summary>비밀키 (SECRET_KEY)</summary>
    public string SecretKey { get; set; } = string.Empty;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(CustomerId) &&
        !string.IsNullOrWhiteSpace(AccessLicense) &&
        !string.IsNullOrWhiteSpace(SecretKey);
}

/// <summary>
/// NAVER API HUB 검색 API 설정 (네이버 클라우드 콘솔 → NAVER API HUB → Application 등록, 사용 API: 검색 - 블로그).
/// 개발자센터(developers.naver.com) 키는 API HUB에서 쓸 수 없다.
/// </summary>
public sealed class NaverOpenApiOptions
{
    public const string SectionName = "Naver:OpenApi";

    public string BaseUrl { get; set; } = "https://naverapihub.apigw.ntruss.com";

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ClientId) &&
        !string.IsNullOrWhiteSpace(ClientSecret);
}
