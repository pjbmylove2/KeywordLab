using KeywordLab.Core.Abstractions;
using KeywordLab.Core.Assessment;
using KeywordLab.Core.Services;
using KeywordLab.Core.Text;
using KeywordLab.Core.Titles;
using KeywordLab.Core.Trends;
using KeywordLab.Infrastructure.Naver;
using KeywordLab.Infrastructure.Storage;
using KeywordLab.Infrastructure.Trends;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KeywordLab.Infrastructure;

public static class ServiceCollectionExtensions
{
    /// <summary>RSS·자동완성 요청에 붙이는 이름. 개인 정보는 넣지 않는다.</summary>
    private const string UserAgent = "Mozilla/5.0 (compatible; KeywordLab/1.0)";

    public static IServiceCollection AddKeywordLab(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<NaverSearchAdOptions>(configuration.GetSection(NaverSearchAdOptions.SectionName));
        services.Configure<NaverOpenApiOptions>(configuration.GetSection(NaverOpenApiOptions.SectionName));
        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));

        services.AddSingleton(TimeProvider.System);
        services.AddHttpClient<IKeywordVolumeProvider, NaverSearchAdClient>(c => c.Timeout = TimeSpan.FromSeconds(15));
        services.AddHttpClient<IBlogSearchProvider, NaverBlogSearchClient>(c => c.Timeout = TimeSpan.FromSeconds(10));

        services.AddSingleton<CandidateGenerator>();
        services.AddSingleton<CompetitionAssessor>();
        services.AddScoped<KeywordLookupService>();

        var titleRules = configuration.GetSection(TitleScoringRules.SectionName).Get<TitleScoringRules>() ?? new TitleScoringRules();
        services.AddSingleton(titleRules);
        services.AddSingleton<ISettingsStore, JsonSettingsStore>();
        // 금지 표현은 설정 화면에서 바뀌므로 요청마다 settings.json 값을 읽어 점수기를 만든다.
        services.AddScoped(sp => new TitleScorer(new TitleScoringRules
        {
            BannedPhrases = sp.GetRequiredService<ISettingsStore>().Load().Settings.BannedPhrases,
            TistoryMaxLength = titleRules.TistoryMaxLength,
            MaxSpecialChars = titleRules.MaxSpecialChars,
        }));
        services.AddSingleton<ITitleGenerator, TemplateTitleGenerator>();
        services.AddScoped<TitleWorkshopService>();
        services.AddSingleton<IHandoffLog, SqliteHandoffLog>();

        // 3단계: 오늘의 이슈 키워드
        services.Configure<TrendsOptions>(configuration.GetSection(TrendsOptions.SectionName));
        services.Configure<BlogOptions>(configuration.GetSection(BlogOptions.SectionName));
        services.AddHttpClient<ITrendFeed, GoogleTrendsRssClient>(c =>
        {
            c.Timeout = TimeSpan.FromSeconds(10);
            c.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        });
        services.AddHttpClient<IAutocompleteProvider, NaverAutocompleteClient>(c =>
        {
            c.Timeout = TimeSpan.FromSeconds(5);
            c.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        });
        services.AddSingleton<ITrendStore, SqliteTrendStore>();
        var issueRules = configuration.GetSection(IssueScoringRules.SectionName).Get<IssueScoringRules>() ?? new IssueScoringRules();
        services.AddSingleton(new IssueKeywordScorer(issueRules));
        services.AddScoped<IssueKeywordService>();

        return services;
    }
}
