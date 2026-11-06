using KeywordLab.Core.Services;
using KeywordLab.Core.Trends;
using KeywordLab.Infrastructure.Naver;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace KeywordLab.Web.Pages;

public class IndexModel : PageModel
{
    private readonly IssueKeywordService _issues;
    private readonly BlogOptions _blogs;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IssueKeywordService issues, IssueKeywordScorer scorer, IOptions<BlogOptions> blogs, ILogger<IndexModel> logger)
    {
        _issues = issues;
        _blogs = blogs.Value;
        _logger = logger;
        WindowHours = scorer.Rules.WindowHours;
    }

    public double WindowHours { get; }

    /// <summary>appsettings.json "Blogs:Items"의 순번.</summary>
    [BindProperty(SupportsGet = true, Name = "blog")]
    public int BlogIndex { get; set; }

    public IReadOnlyList<BlogProfile> Blogs => _blogs.Items;

    public BlogProfile? Blog => BlogIndex >= 0 && BlogIndex < _blogs.Items.Length ? _blogs.Items[BlogIndex] : null;

    public IssueBoard? Board { get; private set; }

    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        try
        {
            Board = await _issues.GetBoardAsync(cancellationToken);
        }
        catch (NaverApiException ex)
        {
            _logger.LogWarning(ex, "네이버 API 오류");
            ErrorMessage = NaverErrorMessages.For(ex);
        }
        catch (InvalidOperationException ex)
        {
            // 네이버 키 미설정
            ErrorMessage = ex.Message;
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
        catch (SqliteException ex)
        {
            _logger.LogWarning(ex, "트렌드 DB 오류");
            ErrorMessage = "수집한 키워드를 저장하는 DB를 열지 못했습니다. appsettings.json의 Storage:DatabasePath 경로에 쓰기 권한이 있는지 확인하세요.";
        }
    }

    /// <summary>선택한 블로그의 허용 카테고리. 블로그 설정이 없으면 null (판별 안 함).</summary>
    public string? CategoryOf(IssueKeyword k) => Blog is null
        ? null
        : CategoryMatcher.Match(Blog, k.Autocomplete.Prepend(k.Keyword).Concat(k.Trend.News.Select(n => n.Title)));

    public static string AgeText(double hours) => hours switch
    {
        < 1 => "방금",
        < 24 => $"{(int)hours}시간 전",
        _ => $"{(int)(hours / 24)}일 전",
    };

    public static string DemandText(IssueKeyword k) => k.Score.Source == DemandSource.GoogleTraffic
        ? $"구글 예상 트래픽 {k.Trend.MaxTraffic:N0}+"
        : $"네이버 월검색 {k.Volume?.TotalDisplay}";
}
