using KeywordLab.Core.Text;

namespace KeywordLab.Tests.Core;

public class KeywordNormalizerTests
{
    [Theory]
    [InlineData("겨울철 전기요금", "겨울철전기요금")]
    [InlineData("  갤럭시  s26 ", "갤럭시S26")]
    public void Compact_removes_spaces_and_uppercases(string input, string expected)
    {
        Assert.Equal(expected, KeywordNormalizer.Compact(input));
    }

    [Fact]
    public void Tidy_collapses_spaces()
    {
        Assert.Equal("전기요금 할인 대상", KeywordNormalizer.Tidy("  전기요금   할인 대상 "));
    }
}
