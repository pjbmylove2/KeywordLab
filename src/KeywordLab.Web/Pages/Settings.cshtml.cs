using KeywordLab.Core.Abstractions;
using KeywordLab.Core.Settings;
using KeywordLab.Core.Titles;
using KeywordLab.Core.Trends;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KeywordLab.Web.Pages;

public class SettingsModel : PageModel
{
    public const int MaxBlogs = 10;
    public const int MaxTextLength = 5000;

    private readonly ISettingsStore _store;
    private readonly ILogger<SettingsModel> _logger;

    public SettingsModel(ISettingsStore store, ILogger<SettingsModel> logger)
    {
        _store = store;
        _logger = logger;
    }

    /// <summary>화면 입력용 블로그 한 칸. 카테고리는 "이름: 단어, 단어" 줄 글.</summary>
    public sealed class BlogInput
    {
        public string? Name { get; set; }

        public BlogPlatform Platform { get; set; } = BlogPlatform.Tistory;

        public string? Categories { get; set; }
    }

    [BindProperty]
    public List<BlogInput> BlogInputs { get; set; } = [];

    [BindProperty]
    public string? BannedPhrases { get; set; }

    [BindProperty(SupportsGet = true, Name = "saved")]
    public bool Saved { get; set; }

    public string Location => _store.Location;

    public bool IsDefault { get; private set; }

    public string? LoadError { get; private set; }

    public List<string> Errors { get; } = [];

    public void OnGet()
    {
        var snapshot = _store.Load();
        IsDefault = snapshot.IsDefault;
        LoadError = snapshot.Error;

        BlogInputs = snapshot.Settings.Blogs
            .Select(b => new BlogInput { Name = b.Name, Platform = b.Platform, Categories = SettingsText.FormatCategories(b.Categories) })
            .ToList();
        BannedPhrases = SettingsText.FormatPhrases(snapshot.Settings.BannedPhrases);
        AddEmptySlot();
    }

    public IActionResult OnPost()
    {
        var blogs = new List<BlogProfile>();
        foreach (var (input, index) in BlogInputs.Take(MaxBlogs + 1).Select((b, i) => (b, i + 1)))
        {
            var name = input.Name?.Trim() ?? string.Empty;
            var text = input.Categories ?? string.Empty;
            if (name.Length == 0 && string.IsNullOrWhiteSpace(text))
            {
                continue; // 빈 칸 = 블로그 삭제
            }

            if (name.Length == 0)
            {
                Errors.Add($"{index}번째 블로그: 이름이 비었습니다. 이름을 적거나, 블로그를 지우려면 카테고리 칸도 비우세요.");
                continue;
            }

            if (text.Length > MaxTextLength)
            {
                Errors.Add($"'{name}': 카테고리 글이 너무 깁니다. {MaxTextLength}자 이내로 줄이세요.");
                continue;
            }

            var (categories, errors) = SettingsText.ParseCategories(text);
            Errors.AddRange(errors.Select(e => $"'{name}' {e}"));
            if (categories.Length == 0 && errors.Count == 0)
            {
                Errors.Add($"'{name}': 허용 카테고리가 없습니다. 한 줄 이상 적으세요. (없으면 모든 키워드가 카테고리 밖으로 흐리게 보입니다)");
            }

            if (blogs.Any(b => string.Equals(b.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                Errors.Add($"'{name}' 블로그가 두 번 있습니다. 이름을 다르게 하세요.");
            }

            blogs.Add(new BlogProfile { Name = name, Platform = input.Platform, Categories = categories });
        }

        if (blogs.Count > MaxBlogs)
        {
            Errors.Add($"블로그는 {MaxBlogs}개까지 둘 수 있습니다.");
        }

        if ((BannedPhrases?.Length ?? 0) > MaxTextLength)
        {
            Errors.Add($"금지 표현이 너무 깁니다. {MaxTextLength}자 이내로 줄이세요.");
        }

        if (Errors.Count > 0)
        {
            if (BlogInputs.Count == 0 || !string.IsNullOrWhiteSpace(BlogInputs[^1].Name))
            {
                AddEmptySlot();
            }

            return Page();
        }

        try
        {
            _store.Save(new LabSettings { Blogs = blogs.ToArray(), BannedPhrases = SettingsText.ParsePhrases(BannedPhrases) });
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "설정 저장 실패");
            Errors.Add($"설정을 저장하지 못했습니다. {Location} 폴더에 쓰기 권한이 있는지, 다른 프로그램이 파일을 열고 있지 않은지 확인하세요.");
            return Page();
        }

        return RedirectToPage(new { saved = true });
    }

    private void AddEmptySlot()
    {
        if (BlogInputs.Count < MaxBlogs)
        {
            BlogInputs.Add(new BlogInput());
        }
    }
}
