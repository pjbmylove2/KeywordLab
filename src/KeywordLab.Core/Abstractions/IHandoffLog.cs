using KeywordLab.Core.Titles;

namespace KeywordLab.Core.Abstractions;

/// <summary>Codex로 넘긴 기록 한 건. 입력 블록을 복사할 때 저장한다.</summary>
public sealed record HandoffRecord(string Keyword, BlogPlatform Platform, string Title, TitlePattern Pattern, DateTimeOffset CreatedAt);

public interface IHandoffLog
{
    Task SaveAsync(HandoffRecord record, CancellationToken cancellationToken = default);
}
