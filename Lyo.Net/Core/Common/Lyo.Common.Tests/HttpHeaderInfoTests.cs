using Lyo.Common.Core.Enums;
using Lyo.Common.Core.Net;
using Lyo.Common.Metadata.Records;

namespace Lyo.Common.Tests;

public class HttpHeaderInfoTests
{
    [Fact]
    public void StaticRegistry_ContainsExpectedMetadata()
    {
        Assert.Equal("Accept", HttpHeaderInfo.Accept.Name);
        Assert.Equal(HttpHeaderCategory.Negotiation, HttpHeaderInfo.Accept.Category);
        Assert.Equal(HttpHeaderPresence.Request, HttpHeaderInfo.Accept.Presence);
        Assert.True(HttpHeaderInfo.Accept.IsRequest);
        Assert.False(HttpHeaderInfo.Accept.IsResponse);

        Assert.Equal("Content-Type", HttpHeaderInfo.ContentType.Name);
        Assert.Equal(HttpHeaderPresence.Both, HttpHeaderInfo.ContentType.Presence);
        Assert.True(HttpHeaderInfo.ContentType.IsRequest);
        Assert.True(HttpHeaderInfo.ContentType.IsResponse);

        Assert.Equal(HttpHeaderCategory.Cookie, HttpHeaderInfo.SetCookie.Category);
        Assert.Equal(HttpHeaderPresence.Response, HttpHeaderInfo.SetCookie.Presence);
        Assert.Contains("X-XSRF-TOKEN", HttpHeaderInfo.CsrfToken.Aliases);
        Assert.Contains("X-Idempotency-Key", HttpHeaderInfo.IdempotencyKey.Aliases);
    }

    [Fact]
    public void LyoContractRows_MatchLyoHttpHeaders()
    {
        Assert.Equal(LyoHttpHeaders.Authorization, HttpHeaderInfo.Authorization.Name);
        Assert.Equal(LyoHttpHeaders.ApiKey, HttpHeaderInfo.ApiKey.Name);
        Assert.Equal(LyoHttpHeaders.CorrelationId, HttpHeaderInfo.CorrelationId.Name);
        Assert.Equal(LyoHttpHeaders.RequestId, HttpHeaderInfo.RequestId.Name);
        Assert.Equal(HttpHeaderCategory.Auth, HttpHeaderInfo.ApiKey.Category);
        Assert.Equal(HttpHeaderCategory.Correlation, HttpHeaderInfo.CorrelationId.Category);
        Assert.Equal(HttpHeaderPresence.Both, HttpHeaderInfo.CorrelationId.Presence);
    }

    [Fact]
    public void ByCategory_ReturnsMatchingHeaders()
    {
        var correlation = HttpHeaderInfo.ByCategory(HttpHeaderCategory.Correlation).ToList();
        Assert.Contains(HttpHeaderInfo.CorrelationId, correlation);
        Assert.Contains(HttpHeaderInfo.RequestId, correlation);
        Assert.Contains(HttpHeaderInfo.TraceId, correlation);
        Assert.DoesNotContain(HttpHeaderInfo.ForwardedFor, correlation);

        var proxy = HttpHeaderInfo.ByCategory(HttpHeaderCategory.Proxy).ToList();
        Assert.Contains(HttpHeaderInfo.ForwardedFor, proxy);
        Assert.Contains(HttpHeaderInfo.ForwardedProto, proxy);
        Assert.Contains(HttpHeaderInfo.RealIp, proxy);
        Assert.DoesNotContain(HttpHeaderInfo.CorrelationId, proxy);
    }

    [Theory]
    [InlineData("user-agent", "User-Agent")]
    [InlineData("x-correlation-id", "X-Correlation-Id")]
    [InlineData("X-API-KEY", "X-Api-Key")]
    [InlineData("Accept-Language", "Accept-Language")]
    public void FromName_IsCaseInsensitive(string name, string expectedWireName)
    {
        var info = HttpHeaderInfo.FromName(name);
        Assert.Equal(expectedWireName, info.Name);
        Assert.NotEqual(HttpHeaderInfo.Unknown, info);
    }

    [Theory]
    [InlineData("X-Request-ID")]
    [InlineData("X-XSRF-TOKEN")]
    [InlineData("X-Idempotency-Key")]
    public void FromName_ResolvesAliases(string name)
    {
        var info = HttpHeaderInfo.FromName(name);
        Assert.NotEqual(HttpHeaderInfo.Unknown, info);
    }

    [Fact]
    public void FromName_Alias_MapsToCanonicalRow()
    {
        Assert.Equal(HttpHeaderInfo.RequestId, HttpHeaderInfo.FromName("X-Request-ID"));
        Assert.Equal(HttpHeaderInfo.CsrfToken, HttpHeaderInfo.FromName("X-XSRF-TOKEN"));
        Assert.Equal(HttpHeaderInfo.IdempotencyKey, HttpHeaderInfo.FromName("X-Idempotency-Key"));
        Assert.Equal(HttpHeaderInfo.ApiKey, HttpHeaderInfo.FromName("X-API-KEY"));
    }

    [Fact]
    public void FromName_Blank_ReturnsUnknown() => Assert.Equal(HttpHeaderInfo.Unknown, HttpHeaderInfo.FromName("  "));

    [Fact]
    public void FromName_Unknown_ReturnsUnknown() => Assert.Equal(HttpHeaderInfo.Unknown, HttpHeaderInfo.FromName("X-Twilio-Signature"));

    [Fact]
    public void TryFromName_Known_ReturnsTrue()
    {
        Assert.True(HttpHeaderInfo.TryFromName("cookie", out var info));
        Assert.Equal(HttpHeaderInfo.Cookie, info);
    }

    [Fact]
    public void TryFromName_Unknown_ReturnsFalse() => Assert.False(HttpHeaderInfo.TryFromName("X-FlareSolverr-Url", out _));

    [Fact]
    public void All_ExcludesUnknown()
    {
        Assert.DoesNotContain(HttpHeaderInfo.Unknown, HttpHeaderInfo.All);
        Assert.Contains(HttpHeaderInfo.UserAgent, HttpHeaderInfo.All);
        Assert.Contains(HttpHeaderInfo.CorrelationId, HttpHeaderInfo.All);
        Assert.Contains(HttpHeaderInfo.ForwardedFor, HttpHeaderInfo.All);
    }

    [Fact]
    public void ImplicitString_ReturnsWireName()
    {
        string name = HttpHeaderInfo.UserAgent;
        Assert.Equal("User-Agent", name);
    }

    [Fact]
    public void ToString_ReturnsWireName() => Assert.Equal("X-Correlation-Id", HttpHeaderInfo.CorrelationId.ToString());
}
