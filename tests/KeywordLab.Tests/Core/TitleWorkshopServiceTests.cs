using KeywordLab.Core.Services;
using KeywordLab.Core.Titles;
using KeywordLab.Tests.Fakes;

namespace KeywordLab.Tests.Core;

public class TitleWorkshopServiceTests
{
    private static TitleWorkshopService Service(FakeVolumeProvider volumes, FakeBlogProvider blog) =>
        new(volumes, blog, new TemplateTitleGenerator(), new TitleScorer(), new FixedTimeProvider(new DateTimeOffset(2026, 11, 6, 9, 0, 0, TimeSpan.Zero)));

    [Fact]
    public async Task Prepares_volume_documents_intents_and_ranked_titles()
    {
        var volumes = new FakeVolumeProvider(
            V.Of("전기요금", 5610, 8590),
            V.Of("전기요금할인", 1310, 580),
            V.Of("탄소중립", 2530, 4610));
        var blog = new FakeBlogProvider(new() { ["전기요금"] = 3037700 });

        var w = await Service(volumes, blog).PrepareAsync("전기요금", "겨울철 전기요금 아끼는 법", BlogPlatform.Tistory);

        Assert.Equal(["전기요금"], volumes.Calls.Single());
        Assert.Equal(["전기요금"], blog.Queries);
        Assert.Equal(14200, w.Volume!.TotalApprox);
        Assert.Equal(3037700, w.BlogDocuments);
        Assert.Equal(["할인"], w.IntentKeywords);
        Assert.Equal(["탄소중립", "전기요금할인"], w.Related.Select(r => r.Keyword));
        Assert.Equal(new DateOnly(2026, 11, 6), w.Today);
        Assert.NotEmpty(w.Ranked);
        Assert.True(w.Ranked.Zip(w.Ranked.Skip(1)).All(p => p.First.Score >= p.Second.Score));
    }

    [Fact]
    public async Task Missing_volume_is_null_not_error()
    {
        var w = await Service(new FakeVolumeProvider(V.Of("다른키워드", 1, 1)), new FakeBlogProvider(new()))
            .PrepareAsync("전기요금", null, BlogPlatform.Naver);

        Assert.Null(w.Volume);
        Assert.NotEmpty(w.Ranked);
    }

    [Fact]
    public async Task Empty_keyword_throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            Service(new FakeVolumeProvider(), new FakeBlogProvider(new())).PrepareAsync(" ", null, BlogPlatform.Naver));
    }

    [Fact]
    public void PickDefaults_prefers_different_patterns_for_alternates()
    {
        var ranked = new[]
        {
            Scored("A", TitlePattern.Timely, 90),
            Scored("B", TitlePattern.Timely, 85),
            Scored("C", TitlePattern.Summary, 80),
            Scored("D", TitlePattern.Question, 70),
        };

        var (rec, alts) = TitleWorkshopService.PickDefaults(ranked);

        Assert.Equal("A", rec.Text);
        Assert.Equal(["C", "D"], alts.Select(a => a.Text));
    }

    [Fact]
    public void PickDefaults_fills_with_same_pattern_when_needed()
    {
        var ranked = new[]
        {
            Scored("A", TitlePattern.Summary, 90),
            Scored("B", TitlePattern.Summary, 85),
            Scored("C", TitlePattern.Summary, 80),
        };

        var (_, alts) = TitleWorkshopService.PickDefaults(ranked);

        Assert.Equal(["B", "C"], alts.Select(a => a.Text));
    }

    private static ScoredTitle Scored(string text, TitlePattern pattern, int score) =>
        new(new TitleCandidate(text, pattern), score, []);

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}
