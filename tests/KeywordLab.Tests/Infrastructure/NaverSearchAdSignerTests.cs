using KeywordLab.Infrastructure.Naver;

namespace KeywordLab.Tests.Infrastructure;

public class NaverSearchAdSignerTests
{
    [Fact]
    public void Sign_matches_known_hmac_sha256_vector()
    {
        // 기대값은 Python hmac 모듈로 따로 계산한 값:
        // base64(hmac_sha256("test-secret", "1700000000000.GET./keywordstool"))
        var signature = NaverSearchAdSigner.Sign("1700000000000", "GET", "/keywordstool", "test-secret");

        Assert.Equal("pLnZJtUUxfdXitHXWo/EvKzookF5hlb/Rs2Fuw1W4js=", signature);
    }

    [Fact]
    public void Method_is_uppercased()
    {
        Assert.Equal(
            NaverSearchAdSigner.Sign("1", "GET", "/keywordstool", "s"),
            NaverSearchAdSigner.Sign("1", "get", "/keywordstool", "s"));
    }

    [Fact]
    public void Timestamp_is_unix_milliseconds()
    {
        var clock = new FixedClock(DateTimeOffset.FromUnixTimeMilliseconds(1_700_000_000_123));

        Assert.Equal("1700000000123", NaverSearchAdSigner.NowTimestamp(clock));
    }

    private sealed class FixedClock : TimeProvider
    {
        private readonly DateTimeOffset _now;

        public FixedClock(DateTimeOffset now) => _now = now;

        public override DateTimeOffset GetUtcNow() => _now;
    }
}
