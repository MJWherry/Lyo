using Lyo.Authentication.Models.Records;

namespace Lyo.Authentication.Tests;

public class LyoOAuthTokenResponseTests
{
    [Fact]
    public void FromTokens_ComputesRefreshExpiresIn()
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var response = LyoOAuthTokenResponse.FromTokens("access", now.AddSeconds(60), "refresh", now.AddSeconds(120), now);
        Assert.Equal(60, response.ExpiresIn);
        Assert.Equal(120, response.RefreshExpiresIn);
        Assert.Equal(now.AddSeconds(120), response.RefreshExpiresAtUtc(now));
    }

    [Fact]
    public void FromTokens_OmitsRefreshExpiryWhenMissing()
    {
        var now = DateTime.UtcNow;
        var response = LyoOAuthTokenResponse.FromTokens("access", now.AddSeconds(30), null, null, now);
        Assert.Null(response.RefreshExpiresIn);
        Assert.Null(response.RefreshExpiresAtUtc(now));
    }
}
