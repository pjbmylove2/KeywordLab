using System.Text.Encodings.Web;
using System.Text.Unicode;
using KeywordLab.Infrastructure;
using Microsoft.Extensions.WebEncoders;

var builder = WebApplication.CreateBuilder(args);

// 개발 환경에서는 VS "사용자 비밀 관리"(secrets.json)의 값이 appsettings.json 위에 덮어씌워진다.
builder.Services.AddRazorPages();
// 한글이 HTML 소스에 &#xAC80; 같은 엔티티로 바뀌지 않도록 한다 (브라우저 F12에서 읽기 쉽게).
builder.Services.Configure<WebEncoderOptions>(o => o.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));
builder.Services.AddKeywordLab(builder.Configuration);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();

app.Run();
