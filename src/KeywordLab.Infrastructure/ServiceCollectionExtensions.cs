using KeywordLab.Core.Abstractions;
using KeywordLab.Core.Assessment;
using KeywordLab.Core.Services;
using KeywordLab.Core.Text;
using KeywordLab.Core.Titles;
using KeywordLab.Infrastructure.Naver;
using KeywordLab.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KeywordLab.Infrastructure;

public static class ServiceCollectionExtensions
{
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
        services.AddSingleton(new TitleScorer(titleRules));
        services.AddSingleton<ITitleGenerator, TemplateTitleGenerator>();
        services.AddScoped<TitleWorkshopService>();
        services.AddSingleton<IHandoffLog, SqliteHandoffLog>();

        return services;
    }
}
