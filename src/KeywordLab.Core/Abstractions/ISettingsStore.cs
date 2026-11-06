using KeywordLab.Core.Settings;

namespace KeywordLab.Core.Abstractions;

/// <summary>설정 화면 값 저장소. 파일이 작아서 동기로 읽고 쓴다 (DI 팩터리에서도 읽기 위해).</summary>
public interface ISettingsStore
{
    /// <summary>저장 위치 (화면 안내용).</summary>
    string Location { get; }

    SettingsSnapshot Load();

    void Save(LabSettings settings);
}
