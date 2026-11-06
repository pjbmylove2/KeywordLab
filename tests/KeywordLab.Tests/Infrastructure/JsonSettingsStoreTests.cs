using KeywordLab.Core.Settings;
using KeywordLab.Core.Titles;
using KeywordLab.Core.Trends;
using KeywordLab.Infrastructure.Storage;

namespace KeywordLab.Tests.Infrastructure;

public sealed class JsonSettingsStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "keywordlab-tests-" + Guid.NewGuid().ToString("N"));

    private JsonSettingsStore Store() => new(
        Microsoft.Extensions.Options.Options.Create(new StorageOptions { DatabasePath = Path.Combine(_dir, "keywordlab.db") }),
        Microsoft.Extensions.Options.Options.Create(new BlogOptions
        {
            Items = [new BlogProfile { Name = "기본 블로그", Categories = [new CategoryWords { Name = "생활정보", Words = ["요금"] }] }],
        }),
        new TitleScoringRules { BannedPhrases = ["충격"] });

    [Fact]
    public void Missing_file_returns_appsettings_defaults()
    {
        var snapshot = Store().Load();

        Assert.True(snapshot.IsDefault);
        Assert.Null(snapshot.Error);
        Assert.Equal("기본 블로그", Assert.Single(snapshot.Settings.Blogs).Name);
        Assert.Equal(["충격"], snapshot.Settings.BannedPhrases);
    }

    [Fact]
    public void Saves_next_to_database_and_loads_back()
    {
        var store = Store();
        store.Save(new LabSettings
        {
            Blogs = [new BlogProfile { Name = "네이버 블로그", Platform = BlogPlatform.Naver, Categories = [new CategoryWords { Name = "경제", Words = ["금리", "환율"] }] }],
            BannedPhrases = ["경악"],
        });

        var snapshot = store.Load();

        Assert.Equal(Path.Combine(_dir, JsonSettingsStore.FileName), store.Location);
        Assert.False(snapshot.IsDefault);
        var blog = Assert.Single(snapshot.Settings.Blogs);
        Assert.Equal(BlogPlatform.Naver, blog.Platform);
        Assert.Equal(["금리", "환율"], Assert.Single(blog.Categories).Words);
        Assert.Equal(["경악"], snapshot.Settings.BannedPhrases);
        Assert.Contains("\"Naver\"", File.ReadAllText(store.Location)); // 사람이 읽을 수 있게 이름으로 저장
        Assert.Contains("금리", File.ReadAllText(store.Location));
    }

    [Fact]
    public void Broken_file_falls_back_to_defaults_with_error()
    {
        var store = Store();
        Directory.CreateDirectory(_dir);
        File.WriteAllText(store.Location, "{ 깨진 json");

        var snapshot = store.Load();

        Assert.True(snapshot.IsDefault);
        Assert.NotNull(snapshot.Error);
        Assert.Equal("기본 블로그", Assert.Single(snapshot.Settings.Blogs).Name);
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }
}
