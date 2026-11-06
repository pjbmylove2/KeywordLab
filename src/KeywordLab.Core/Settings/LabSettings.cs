using KeywordLab.Core.Trends;

namespace KeywordLab.Core.Settings;

/// <summary>설정 화면에서 고치는 값. 저장소 밖(%LOCALAPPDATA%\KeywordLab\settings.json)에 저장한다.</summary>
public sealed class LabSettings
{
    /// <summary>블로그별 허용 카테고리.</summary>
    public BlogProfile[] Blogs { get; set; } = [];

    /// <summary>제목 금지 표현.</summary>
    public string[] BannedPhrases { get; set; } = [];
}

/// <param name="IsDefault">settings.json이 없어 appsettings.json 기본값을 쓰는 중.</param>
/// <param name="Error">settings.json을 읽지 못한 이유. 이때도 기본값을 돌려준다.</param>
public sealed record SettingsSnapshot(LabSettings Settings, bool IsDefault, string? Error);
