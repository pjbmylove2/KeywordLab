using System.Globalization;
using System.Text;
using KeywordLab.Core.Models;

namespace KeywordLab.Core.Titles;

/// <param name="Volume">메인 키워드 검색량. 검색광고 API에 없으면 null.</param>
/// <param name="IssueBackground">이슈 배경 메모. 검증 전 참고용.</param>
public sealed record CodexBlockInput(
    DateOnly Date,
    BlogPlatform Platform,
    string Topic,
    string Keyword,
    KeywordVolume? Volume,
    long BlogDocuments,
    IReadOnlyList<string> Related,
    string RecommendedTitle,
    IReadOnlyList<string> AlternateTitles,
    string? IssueBackground);

/// <summary>Codex 마스터 프롬프트에 붙여 넣을 입력 블록 (docs/DECISIONS.md "입력 블록 형식").</summary>
public static class CodexBlockBuilder
{
    public static string Build(CodexBlockInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var inv = CultureInfo.InvariantCulture;
        var volume = input.Volume is null ? "확인 안 됨" : input.Volume.TotalDisplay;
        var topic = string.IsNullOrWhiteSpace(input.Topic) ? input.Keyword : input.Topic.Trim();
        var related = input.Related.Count == 0 ? "없음" : string.Join(", ", input.Related);
        var background = string.IsNullOrWhiteSpace(input.IssueBackground) ? "없음" : input.IssueBackground.Trim();

        var sb = new StringBuilder();
        sb.Append(inv, $"[키워드 데이터 - {input.Date:yyyy-MM-dd} 네이버 검색광고 API 확인]").Append('\n');
        sb.Append("플랫폼: ").Append(TitleLabels.Of(input.Platform)).Append('\n');
        sb.Append("주제: ").Append(topic).Append('\n');
        sb.Append(inv, $"핵심 키워드: {input.Keyword} (월간 검색량 {volume}, 블로그 문서 수 {input.BlogDocuments:N0})").Append('\n');
        sb.Append("관련 키워드: ").Append(related).Append('\n');
        sb.Append("추천 제목: ").Append(input.RecommendedTitle).Append('\n');
        sb.Append("대안 제목:").Append('\n');
        foreach (var alt in input.AlternateTitles)
        {
            sb.Append("- ").Append(alt).Append('\n');
        }

        sb.Append("이슈 배경(참고용, 검증 전): ").Append(background).Append('\n');
        sb.Append('\n');
        sb.Append("[적용 규칙]").Append('\n');
        sb.Append("- 핵심 키워드를 1단계 핵심 키워드로 사용").Append('\n');
        sb.Append("- 검색량은 확인된 데이터로 취급하되 본문에 수치 노출 금지").Append('\n');
        sb.Append("- 추천 제목 우선, 사실과 다르면 키워드 유지한 채 수정하고 이유 명시").Append('\n');
        sb.Append("- 이슈 배경 뉴스는 2단계에서 검증").Append('\n');
        return sb.ToString();
    }
}
