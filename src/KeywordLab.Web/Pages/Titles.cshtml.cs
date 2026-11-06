using KeywordLab.Core.Abstractions;
using KeywordLab.Core.Services;
using KeywordLab.Core.Titles;
using KeywordLab.Infrastructure.Naver;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.Sqlite;

namespace KeywordLab.Web.Pages;

public class TitlesModel : PageModel
{
    public const int MaxBackgroundLength = 1000;

    private readonly TitleWorkshopService _workshop;
    private readonly IHandoffLog _log;
    private readonly TimeProvider _time;
    private readonly ILogger<TitlesModel> _logger;

    public TitlesModel(TitleWorkshopService workshop, IHandoffLog log, TimeProvider time, ILogger<TitlesModel> logger)
    {
        _workshop = workshop;
        _log = log;
        _time = time;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true, Name = "kw")]
    public string? Keyword { get; set; }

    [BindProperty(SupportsGet = true, Name = "topic")]
    public string? Topic { get; set; }

    [BindProperty(SupportsGet = true, Name = "platform")]
    public string? PlatformValue { get; set; }

    [BindProperty(Name = "rec")]
    public string? RecommendedText { get; set; }

    [BindProperty(Name = "alt")]
    public List<string> AlternateTexts { get; set; } = [];

    [BindProperty(Name = "bg")]
    public string? IssueBackground { get; set; }

    public BlogPlatform Platform => ParsePlatform(PlatformValue);

    public TitleWorkshop? Workshop { get; private set; }

    public ScoredTitle? Recommended { get; private set; }

    public IReadOnlyList<ScoredTitle> Alternates { get; private set; } = [];

    public string? Block { get; private set; }

    public string? ErrorMessage { get; private set; }

    public Task OnGetAsync(CancellationToken cancellationToken) => LoadAsync(useSelection: false, cancellationToken);

    /// <summary>추천·대안 선택을 바꾸거나 이슈 배경을 적은 뒤 블록을 다시 만든다.</summary>
    public Task OnPostAsync(CancellationToken cancellationToken) => LoadAsync(useSelection: true, cancellationToken);

    /// <summary>입력 블록을 복사할 때 호출된다. 키워드·플랫폼·제목·패턴·시각을 저장한다.</summary>
    public async Task<IActionResult> OnPostSaveAsync(string? keyword, string? platform, string? title, string? pattern, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(keyword) || string.IsNullOrWhiteSpace(title) || !Enum.TryParse<TitlePattern>(pattern, out var parsedPattern))
        {
            return BadRequest(new { ok = false, message = "저장할 키워드·제목·패턴이 비어 있습니다. 페이지를 새로 고친 뒤 다시 복사하세요." });
        }

        var record = new HandoffRecord(keyword.Trim(), ParsePlatform(platform), title.Trim(), parsedPattern, _time.GetLocalNow());
        try
        {
            await _log.SaveAsync(record, cancellationToken);
            return new JsonResult(new { ok = true });
        }
        catch (Exception ex) when (ex is SqliteException or IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "넘긴 기록 저장 실패");
            return StatusCode(500, new { ok = false, message = "복사는 됐지만 기록을 저장하지 못했습니다. appsettings.json의 Storage:DatabasePath 경로에 쓰기 권한이 있는지 확인하세요." });
        }
    }

    public static BlogPlatform ParsePlatform(string? value) =>
        string.Equals(value, "naver", StringComparison.OrdinalIgnoreCase) ? BlogPlatform.Naver : BlogPlatform.Tistory;

    public static string PlatformValueOf(BlogPlatform platform) => platform == BlogPlatform.Naver ? "naver" : "tistory";

    private async Task LoadAsync(bool useSelection, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Keyword))
        {
            return;
        }

        if (IssueBackground is { Length: > MaxBackgroundLength })
        {
            ErrorMessage = $"이슈 배경은 {MaxBackgroundLength}자 이내로 적으세요.";
            return;
        }

        try
        {
            Workshop = await _workshop.PrepareAsync(Keyword, Topic, Platform, cancellationToken);
        }
        catch (NaverApiException ex)
        {
            _logger.LogWarning(ex, "네이버 API 오류");
            ErrorMessage = NaverErrorMessages.For(ex);
            return;
        }
        catch (InvalidOperationException ex)
        {
            // 키 미설정
            ErrorMessage = ex.Message;
            return;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "네트워크 오류");
            ErrorMessage = NaverErrorMessages.Network;
            return;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            ErrorMessage = NaverErrorMessages.Timeout;
            return;
        }

        if (Workshop.Ranked.Count == 0)
        {
            ErrorMessage = "제목 후보를 만들지 못했습니다. 키워드를 확인하세요.";
            return;
        }

        (Recommended, Alternates) = useSelection ? ResolveSelection(Workshop.Ranked) : TitleWorkshopService.PickDefaults(Workshop.Ranked);
        Block = CodexBlockBuilder.Build(TitleWorkshopService.ToBlockInput(Workshop, Recommended, Alternates, IssueBackground));
    }

    /// <summary>폼에서 고른 제목을 찾는다. 없거나 부족하면 기본 선택으로 채운다.</summary>
    private (ScoredTitle, IReadOnlyList<ScoredTitle>) ResolveSelection(IReadOnlyList<ScoredTitle> ranked)
    {
        var defaults = TitleWorkshopService.PickDefaults(ranked);
        var rec = ranked.FirstOrDefault(t => t.Text == RecommendedText) ?? defaults.Recommended;

        var alts = AlternateTexts
            .Select(text => ranked.FirstOrDefault(t => t.Text == text))
            .OfType<ScoredTitle>()
            .Where(t => t != rec)
            .Distinct()
            .Take(TitleWorkshopService.AlternateCount)
            .ToList();

        foreach (var t in defaults.Alternates.Append(defaults.Recommended).Concat(ranked))
        {
            if (alts.Count == TitleWorkshopService.AlternateCount)
            {
                break;
            }

            if (t != rec && !alts.Contains(t))
            {
                alts.Add(t);
            }
        }

        return (rec, alts);
    }
}
