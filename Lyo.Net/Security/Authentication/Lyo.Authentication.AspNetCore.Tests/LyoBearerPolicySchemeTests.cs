using Lyo.Authentication.AspNetCore.Defaults;
using Lyo.Authentication.AspNetCore.Schemes.Bearer;

namespace Lyo.Authentication.AspNetCore.Tests;

public sealed class LyoBearerPolicySchemeTests
{
    [Fact]
    public void Dispatcher_PicksOpaque_ForLyoPrefixedCredential()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers.Authorization = "Bearer lyo_pat_live_AAAAAAAAAAA_AAAAAAAAAAAA";
        var scheme = LyoBearerPolicySchemeHandler.SelectScheme(ctx, new());
        Assert.Equal(LyoAuthenticationSchemes.OpaqueToken, scheme);
    }

    [Fact]
    public void Dispatcher_PicksJwt_ForOtherCredentials()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers.Authorization = "Bearer eyJhbGciOiJFZERTQSJ9.payload.sig";
        var scheme = LyoBearerPolicySchemeHandler.SelectScheme(ctx, new());
        Assert.Equal(LyoAuthenticationSchemes.LyoJwt, scheme);
    }

    [Fact]
    public void Dispatcher_PicksJwt_WhenNoCredentialPresent()
    {
        var ctx = new DefaultHttpContext();
        var scheme = LyoBearerPolicySchemeHandler.SelectScheme(ctx, new());
        Assert.Equal(LyoAuthenticationSchemes.LyoJwt, scheme);
    }

    [Fact]
    public void Dispatcher_PicksOpaque_FromXApiKey()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers["X-Api-Key"] = "lyo_pat_dev_AAAAAAAAAAA_AAAAAAAAAAA";
        var scheme = LyoBearerPolicySchemeHandler.SelectScheme(ctx, new());
        Assert.Equal(LyoAuthenticationSchemes.OpaqueToken, scheme);
    }
}