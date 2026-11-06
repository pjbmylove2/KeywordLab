using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KeywordLab.Web.Pages;

public class SettingsModel : PageModel
{
    [BindProperty(SupportsGet = true, Name = "kw")]
    public string? Keyword { get; set; }

    public void OnGet()
    {
    }
}
