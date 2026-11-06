using KeywordLab.Core.Models;
using KeywordLab.Core.Titles;

namespace KeywordLab.Tests.Titles;

public class CodexBlockBuilderTests
{
    private static CodexBlockInput Input(KeywordVolume? volume = null, string? background = null, string topic = "겨울철 전기요금 아끼는 법") => new(
        new DateOnly(2026, 11, 6),
        BlogPlatform.Naver,
        topic,
        "전기요금",
        volume ?? new KeywordVolume("전기요금", VolumeCount.Of(5610), VolumeCount.Of(8590), "높음"),
        3037700,
        ["전기요금할인", "전기세할인"],
        "전기요금 할인 총정리",
        ["전기요금 할인, 어떻게 하면 될까?", "전기요금 할인 2026년 11월 기준 정리"],
        background);

    [Fact]
    public void Builds_block_in_decided_format()
    {
        var block = CodexBlockBuilder.Build(Input(background: "11월 요금 인상 발표"));

        var expected = """
            [키워드 데이터 - 2026-11-06 네이버 검색광고 API 확인]
            플랫폼: 네이버
            주제: 겨울철 전기요금 아끼는 법
            핵심 키워드: 전기요금 (월간 검색량 14,200, 블로그 문서 수 3,037,700)
            관련 키워드: 전기요금할인, 전기세할인
            추천 제목: 전기요금 할인 총정리
            대안 제목:
            - 전기요금 할인, 어떻게 하면 될까?
            - 전기요금 할인 2026년 11월 기준 정리
            이슈 배경(참고용, 검증 전): 11월 요금 인상 발표

            [적용 규칙]
            - 핵심 키워드를 1단계 핵심 키워드로 사용
            - 검색량은 확인된 데이터로 취급하되 본문에 수치 노출 금지
            - 추천 제목 우선, 사실과 다르면 키워드 유지한 채 수정하고 이유 명시
            - 이슈 배경 뉴스는 2단계에서 검증

            """;
        Assert.Equal(expected.ReplaceLineEndings("\n"), block);
    }

    [Fact]
    public void Missing_values_are_written_explicitly()
    {
        var input = Input(topic: " ") with { Volume = null, Related = [] };

        var block = CodexBlockBuilder.Build(input);

        Assert.Contains("주제: 전기요금\n", block);
        Assert.Contains("월간 검색량 확인 안 됨", block);
        Assert.Contains("관련 키워드: 없음\n", block);
        Assert.Contains("이슈 배경(참고용, 검증 전): 없음\n", block);
    }

    [Fact]
    public void Under_ten_volume_is_shown_as_api_reports()
    {
        var block = CodexBlockBuilder.Build(Input(volume: new KeywordVolume("전기요금", VolumeCount.UnderTen, VolumeCount.UnderTen, null)));

        Assert.Contains("월간 검색량 < 20", block);
    }
}
