using KeywordLab.Core.Text;

namespace KeywordLab.Tests.Core;

public class CandidateGeneratorTests
{
    private readonly CandidateGenerator _sut = new();

    [Fact]
    public void Generates_full_phrase_ngrams_and_nouns()
    {
        var result = _sut.Generate("겨울철 전기요금 아끼는 법");

        Assert.Equal("겨울철 전기요금 아끼는 법", result[0]);
        Assert.Contains("겨울철 전기요금", result);
        Assert.Contains("전기요금 아끼는 법", result);
        Assert.Contains("전기요금", result);
        Assert.DoesNotContain("법", result); // 한 글자 단독 후보는 만들지 않는다
    }

    [Fact]
    public void Strips_particles_before_combining()
    {
        var result = _sut.Generate("전기요금을 아끼는 법");

        Assert.Equal("전기요금 아끼는 법", result[0]);
        Assert.Contains("전기요금", result);
    }

    [Theory]
    [InlineData("전기요금을", "전기요금")]
    [InlineData("전기요금은", "전기요금")]
    [InlineData("겨울철에", "겨울철")]
    [InlineData("서울에서", "서울")]
    [InlineData("고양이", "고양이")] // 명사 끝 '이'는 남긴다
    [InlineData("아끼는", "아끼는")] // 동사 활용 '는'은 남긴다
    [InlineData("마을", "마을")]     // 남는 글자가 한 글자면 떼지 않는다
    public void StripParticle_rules(string token, string expected)
    {
        Assert.Equal(expected, CandidateGenerator.StripParticle(token));
    }

    [Fact]
    public void Removes_duplicates_ignoring_spaces()
    {
        var result = _sut.Generate("전기요금 전기요금");

        Assert.Single(result, r => KeywordNormalizer.Compact(r) == "전기요금");
    }

    [Fact]
    public void Respects_max_candidates()
    {
        var result = _sut.Generate("하나둘 셋넷 다섯여섯 일곱여덟 아홉열", maxCandidates: 4);

        Assert.Equal(4, result.Count);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_topic_returns_nothing(string topic)
    {
        Assert.Empty(_sut.Generate(topic));
    }

    [Fact]
    public void Skips_stop_words_as_single_candidates()
    {
        var result = _sut.Generate("요즘 전기요금");

        Assert.DoesNotContain("요즘", result);
        Assert.Contains("전기요금", result);
    }
}
