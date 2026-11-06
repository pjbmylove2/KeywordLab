using KeywordLab.Core.Assessment;
using KeywordLab.Core.Services;
using KeywordLab.Core.Text;
using KeywordLab.Tests.Fakes;

namespace KeywordLab.Tests.Core;

public class KeywordLookupServiceTests
{
    private static KeywordLookupService Create(FakeVolumeProvider volumes, FakeBlogProvider blog) =>
        new(volumes, blog, new CandidateGenerator(), new CompetitionAssessor());

    [Fact]
    public async Task Matches_api_keywords_without_spaces_to_candidates()
    {
        // 검색광고 API는 공백 없는 키워드를 돌려준다
        var volumes = new FakeVolumeProvider(
            V.Of("겨울철전기요금", 3_000, 11_900),
            V.Of("전기요금", 20_000, 78_000),
            V.Of("전기요금아끼는법", 1_200, 5_100),
            V.Of("전기요금계산", 4_000, 9_000));
        var blog = new FakeBlogProvider(new()
        {
            ["겨울철 전기요금"] = 8_200,
            ["전기요금"] = 210_000,
            ["전기요금 아끼는 법"] = 2_900,
        });

        var result = await Create(volumes, blog).LookupAsync("겨울철 전기요금 아끼는 법");

        var keywords = result.Candidates.Select(c => c.Keyword).ToList();
        Assert.Contains("겨울철 전기요금", keywords);
        Assert.Contains("전기요금", keywords);
        Assert.Contains("전기요금 아끼는 법", keywords);
        Assert.Contains("겨울철 전기요금 아끼는 법", result.NotFound);
    }

    [Fact]
    public async Task Recommended_candidates_come_before_high_competition()
    {
        var volumes = new FakeVolumeProvider(
            V.Of("전기요금", 20_000, 78_000),
            V.Of("겨울철전기요금", 3_000, 11_900));
        var blog = new FakeBlogProvider(new()
        {
            ["전기요금"] = 900_000,       // 경쟁 높음
            ["겨울철 전기요금"] = 8_200,  // 진입 쉬움
        });

        var result = await Create(volumes, blog).LookupAsync("겨울철 전기요금");

        Assert.Equal("겨울철 전기요금", result.Candidates[0].Keyword);
        Assert.Equal(CompetitionLevel.HighCompetition, result.Candidates[^1].Assessment.Level);
    }

    [Fact]
    public async Task Related_keywords_exclude_candidates()
    {
        var volumes = new FakeVolumeProvider(
            V.Of("전기요금", 20_000, 78_000),
            V.Of("전기요금계산", 4_000, 9_000),
            V.Of("전기요금누진제", 2_000, 6_000));
        var blog = new FakeBlogProvider(new() { ["전기요금"] = 100_000 });

        var result = await Create(volumes, blog).LookupAsync("전기요금");

        Assert.Equal(new[] { "전기요금계산", "전기요금누진제" }, result.Related.Select(r => r.Keyword));
    }

    [Fact]
    public async Task Blog_search_is_limited_to_top_candidates()
    {
        var many = Enumerable.Range(1, 8)
            .Select(i => V.Of($"키워드{i}", i * 100, i * 100))
            .ToArray();
        var volumes = new FakeVolumeProvider(many);
        var blog = new FakeBlogProvider(new());

        await Create(volumes, blog).LookupAsync("키워드1 키워드2 키워드3 키워드4 키워드5 키워드6 키워드7 키워드8");

        Assert.True(blog.Queries.Count <= KeywordLookupService.MaxBlogLookups);
    }

    [Fact]
    public async Task Empty_topic_does_not_call_apis()
    {
        var volumes = new FakeVolumeProvider();
        var blog = new FakeBlogProvider(new());

        var result = await Create(volumes, blog).LookupAsync("   ");

        Assert.Empty(result.Candidates);
        Assert.Empty(volumes.Calls);
        Assert.Empty(blog.Queries);
    }
}
