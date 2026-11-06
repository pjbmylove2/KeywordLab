using KeywordLab.Core.Trends;

namespace KeywordLab.Tests.Trends;

public class CategoryMatcherTests
{
    private static readonly BlogProfile Blog = new()
    {
        Name = "테스트",
        Categories =
        [
            new CategoryWords { Name = "생활정보", Words = ["요금", "과태료"] },
            new CategoryWords { Name = "경제", Words = ["금리", "ETF"] },
        ],
    };

    [Fact]
    public void Matches_word_in_keyword()
    {
        Assert.Equal("생활정보", CategoryMatcher.Match(Blog, ["전기요금"]));
    }

    [Fact]
    public void Matches_word_in_news_or_autocomplete()
    {
        Assert.Equal("경제", CategoryMatcher.Match(Blog, ["골드만삭스", "골드만삭스 금리 전망"]));
    }

    [Fact]
    public void Ignores_case_for_latin_words()
    {
        Assert.Equal("경제", CategoryMatcher.Match(Blog, ["미국 etf 추천"]));
    }

    [Fact]
    public void No_match_is_outside_category()
    {
        Assert.Null(CategoryMatcher.Match(Blog, ["나혜미", "에릭 아들 사진 공개"]));
    }

    [Fact]
    public void Blank_words_never_match()
    {
        var blog = new BlogProfile { Categories = [new CategoryWords { Name = "빈칸", Words = ["", " "] }] };

        Assert.Null(CategoryMatcher.Match(blog, ["아무 글"]));
    }
}
