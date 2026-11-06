using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using KeywordLab.Core.Abstractions;
using KeywordLab.Core.Settings;
using KeywordLab.Core.Titles;
using KeywordLab.Core.Trends;
using Microsoft.Extensions.Options;

namespace KeywordLab.Infrastructure.Storage;

/// <summary>
/// 설정 화면 값을 DB와 같은 폴더의 settings.json에 저장한다 (저장소 밖).
/// 파일이 없으면 appsettings.json의 "Blogs"와 "Titles:BannedPhrases"를 기본값으로 쓴다.
/// </summary>
public sealed class JsonSettingsStore : ISettingsStore
{
    public const string FileName = "settings.json";

    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // 로컬 파일이라 한글을 그대로 쓴다
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string _path;
    private readonly BlogOptions _defaultBlogs;
    private readonly TitleScoringRules _defaultTitles;

    public JsonSettingsStore(IOptions<StorageOptions> storage, IOptions<BlogOptions> blogs, TitleScoringRules titleRules)
    {
        var dbPath = Path.GetFullPath(storage.Value.ResolvePath());
        _path = Path.Combine(Path.GetDirectoryName(dbPath) ?? string.Empty, FileName);
        _defaultBlogs = blogs.Value;
        _defaultTitles = titleRules;
    }

    public string Location => _path;

    public SettingsSnapshot Load()
    {
        if (!File.Exists(_path))
        {
            return new SettingsSnapshot(Defaults(), IsDefault: true, Error: null);
        }

        try
        {
            var settings = JsonSerializer.Deserialize<LabSettings>(File.ReadAllText(_path), Json);
            return settings is null
                ? new SettingsSnapshot(Defaults(), true, $"{_path} 내용이 비어 있어 기본값을 씁니다. 설정 화면에서 다시 저장하세요.")
                : new SettingsSnapshot(settings, false, null);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            return new SettingsSnapshot(Defaults(), true, $"{_path}을(를) 읽지 못해 기본값을 씁니다 ({ex.Message}). 설정 화면에서 다시 저장하면 고쳐집니다.");
        }
    }

    public void Save(LabSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var dir = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        // 쓰다 끊겨도 기존 파일이 깨지지 않게 임시 파일에 쓴 뒤 바꾼다.
        var temp = _path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(settings, Json));
        File.Move(temp, _path, overwrite: true);
    }

    private LabSettings Defaults() => new()
    {
        Blogs = _defaultBlogs.Items,
        BannedPhrases = _defaultTitles.BannedPhrases,
    };
}
