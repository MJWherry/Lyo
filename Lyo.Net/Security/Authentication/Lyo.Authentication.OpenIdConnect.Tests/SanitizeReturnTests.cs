using Lyo.Authentication.OpenIdConnect.Endpoints;

namespace Lyo.Authentication.OpenIdConnect.Tests;

public sealed class SanitizeReturnTests
{
    private static readonly string[] GatewayOrigins = ["http://localhost:5138", "https://localhost:5138"];

    [Fact]
    public void IsAllowedReturnUrl_HttpsLaunchProfilePort_IsRejectedUntilAllowListed()
    {
        Assert.False(AuthEndpointsMapper.IsAllowedReturnUrl("https://localhost:7116/auth/handoff", GatewayOrigins));
        Assert.True(AuthEndpointsMapper.IsAllowedReturnUrl("https://localhost:7116/auth/handoff", [.. GatewayOrigins, "https://localhost:7116"]));
    }

    [Fact]
    public void IsAllowedReturnUrl_DocumentedHttpGateway_IsAllowed()
        => Assert.True(AuthEndpointsMapper.IsAllowedReturnUrl("http://localhost:5138/auth/handoff", GatewayOrigins));

    [Fact]
    public void IsAllowedReturnUrl_RelativePath_IsAllowed()
        => Assert.True(AuthEndpointsMapper.IsAllowedReturnUrl("/auth/handoff", GatewayOrigins));

    [Fact]
    public void SanitizeReturn_OffListAbsolute_FallsBackToDefault()
        => Assert.Equal("/", AuthEndpointsMapper.SanitizeReturn("https://localhost:7116/auth/handoff", GatewayOrigins, "/"));

    [Fact]
    public void SanitizeReturn_AllowListedAbsolute_KeepsHandoffUrl()
        => Assert.Equal(
            "https://localhost:7116/auth/handoff", AuthEndpointsMapper.SanitizeReturn("https://localhost:7116/auth/handoff", [.. GatewayOrigins, "https://localhost:7116"], "/"));
}
