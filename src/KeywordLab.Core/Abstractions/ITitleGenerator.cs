using KeywordLab.Core.Titles;

namespace KeywordLab.Core.Abstractions;

/// <summary>
/// 제목 후보 생성기. 지금은 템플릿 조합(<see cref="TemplateTitleGenerator"/>)을 쓰고,
/// 나중에 LLM 생성기로 바꿔 끼울 수 있게 인터페이스로 둔다. 점수는 생성기와 별개로 <see cref="TitleScorer"/>가 매긴다.
/// </summary>
public interface ITitleGenerator
{
    Task<IReadOnlyList<TitleCandidate>> GenerateAsync(TitleRequest request, CancellationToken cancellationToken = default);
}
