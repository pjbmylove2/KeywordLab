using KeywordLab.Core.Abstractions;
using KeywordLab.Core.Assessment;
using KeywordLab.Core.Services;
using KeywordLab.Core.Text;
using KeywordLab.Infrastructure.Naver;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KeywordLab.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKeywordLab(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<NaverSearchAdOptions>(configuration.GetSection(NaverSearchAdOptions.SectionName));
        services.Configure<NaverOpenApiOptions>(configuration.GetSection(NaverOpenApiOptions.SectionName));

        services.AddSingleton(TimeProvider.System);
        services.AddHttpClient<IKeywordVolumeProvider, NaverSearchAdClient>(c => c.Timeout = TimeSpan.FromSeconds(15));
        services.AddHttpClient<IBlogSearchProvider, NaverBlogSearchClient>(c => c.Timeout = TimeSpan.FromSeconds(10));

        services.AddSingleton<CandidateGenerator>();
        services.AddSingleton<CompetitionAssessor>();
        services.AddScoped<KeywordLookupService>();

        return services;
    }
}
