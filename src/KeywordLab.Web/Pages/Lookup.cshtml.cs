using KeywordLab.Core.Assessment;
using KeywordLab.Core.Services;
using KeywordLab.Infrastructure.Naver;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace KeywordLab.Web.Pages;

public class LookupModel : PageModel
{
    public const int MaxTopicLength = 100;

    private readonly KeywordLookupService _lookup;
    private readonly NaverSearchAdOptions _searchAd;
    private readonly NaverOpenApiOptions _openApi;
    private readonly ILogger<LookupModel> _logger;

    public LookupModel(
        KeywordLookupService lookup,
        IOptions<NaverSearchAdOptions> searchAd,
        IOptions<NaverOpenApiOptions> openApi,
        ILogger<LookupModel> logger)
    {
        _lookup = lookup;
        _searchAd = searchAd.Value;
        _openApi = openApi.Value;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true, Name = "q")]
    public string? Query { get; set; }

    public LookupResult? Result { get; private set; }

    public string? ErrorMessage { get; private set; }

    public IReadOnlyList<string> MissingSettings { get; private set; } = Array.Empty<string>();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        MissingSettings = FindMissingSettings();

        if (string.IsNullOrWhiteSpace(Query) || MissingSettings.Count > 0)
        {
            return;
        }

        if (Query.Length > MaxTopicLength)
        {
            ErrorMessage = $"주제는 {MaxTopicLength}자 이내로 입력하세요.";
            return;
        }

        try
        {
            Result = await _lookup.LookupAsync(Query, cancellationToken);
        }
        catch (NaverApiException ex)
        {
            _logger.LogWarning(ex, "네이버 API 오류");
            ErrorMessage = NaverErrorMessages.For(ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "네트워크 오류");
            ErrorMessage = NaverErrorMessages.Network;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            ErrorMessage = NaverErrorMessages.Timeout;
        }
    }

    public static string TagClass(CompetitionLevel level) => level switch
    {
        CompetitionLevel.Easy => "kl-tag is-easy",
        CompetitionLevel.Good => "kl-tag is-good",
        CompetitionLevel.HighCompetition => "kl-tag is-high",
        _ => "kl-tag is-low",
    };

    private IReadOnlyList<string> FindMissingSettings()
    {
        var missing = new List<string>();
        if (!_searchAd.IsConfigured)
        {
            missing.Add("Naver:SearchAd (CustomerId, AccessLicense, SecretKey)");
        }

        if (!_openApi.IsConfigured)
        {
            missing.Add("Naver:OpenApi (ClientId, ClientSecret)");
        }

        return missing;
    }
}
