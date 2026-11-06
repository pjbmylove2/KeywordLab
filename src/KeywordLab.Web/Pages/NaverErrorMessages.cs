using System.Net;
using KeywordLab.Infrastructure.Naver;

namespace KeywordLab.Web.Pages;

/// <summary>네이버 API 오류를 화면 문구로 바꾼다 (무엇이 잘못됐고 어떻게 고치는지).</summary>
public static class NaverErrorMessages
{
    public static string For(NaverApiException ex) => ex.StatusCode switch
    {
        HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden =>
            $"{ex.Api}: 인증에 실패했습니다. 사용자 비밀의 키 값을 확인하세요. 블로그 검색이라면 NAVER API HUB Application에서 받은 Client ID·Secret인지(개발자센터 키는 안 됨), 사용 API에 '검색 - 블로그'가 들어 있는지도 확인하세요.",
        HttpStatusCode.TooManyRequests =>
            $"{ex.Api}: 호출 한도를 넘었습니다. 잠시 후 다시 조회하세요.",
        HttpStatusCode.BadRequest =>
            $"{ex.Api}: 요청 형식이 거부되었습니다. 응답: {ex.ResponseSnippet}",
        _ => ex.Message,
    };

    public const string Network = "네이버 서버에 연결하지 못했습니다. 인터넷 연결을 확인한 뒤 다시 조회하세요.";

    public const string Timeout = "네이버 응답이 너무 늦어 조회를 멈췄습니다. 다시 조회하세요.";
}
