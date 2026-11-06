using KeywordLab.Core.Trends;

namespace KeywordLab.Core.Settings;

/// <summary>
/// 설정 화면의 글 입력 ↔ 설정값 변환.
/// 카테고리: 한 줄에 하나, "카테고리 이름: 단어, 단어". 금지 표현: 쉼표나 줄바꿈으로 구분.
/// </summary>
public static class SettingsText
{
    private static readonly char[] WordSeparators = [',', '，', '\n', '\r'];

    public static string FormatCategories(IEnumerable<CategoryWords> categories) =>
        string.Join("\n", categories.Select(c => $"{c.Name}: {string.Join(", ", c.Words)}"));

    /// <returns>읽은 카테고리와, 잘못된 줄마다 "n번째 줄: 이유" 형식의 오류.</returns>
    public static (CategoryWords[] Categories, IReadOnlyList<string> Errors) ParseCategories(string? text)
    {
        var categories = new List<CategoryWords>();
        var errors = new List<string>();
        var lines = (text ?? string.Empty).Replace("\r\n", "\n").Split('\n');

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (line.Length == 0)
            {
                continue;
            }

            var colon = line.IndexOf(':');
            if (colon <= 0)
            {
                errors.Add($"{i + 1}번째 줄: \"카테고리 이름: 단어, 단어\" 형식으로 적으세요. (콜론 ':'이 없거나 이름이 비었습니다)");
                continue;
            }

            var name = line[..colon].Trim();
            var words = SplitWords(line[(colon + 1)..]);
            if (words.Length == 0)
            {
                errors.Add($"{i + 1}번째 줄: '{name}' 카테고리에 단어가 없습니다. 쉼표로 구분해 하나 이상 적으세요.");
                continue;
            }

            if (categories.Any(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                errors.Add($"{i + 1}번째 줄: '{name}' 카테고리가 두 번 있습니다. 한 줄로 합치세요.");
                continue;
            }

            categories.Add(new CategoryWords { Name = name, Words = words });
        }

        return (categories.ToArray(), errors);
    }

    public static string FormatPhrases(IEnumerable<string> phrases) => string.Join(", ", phrases);

    public static string[] ParsePhrases(string? text) => SplitWords(text ?? string.Empty);

    private static string[] SplitWords(string text) =>
        text.Split(WordSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
}
