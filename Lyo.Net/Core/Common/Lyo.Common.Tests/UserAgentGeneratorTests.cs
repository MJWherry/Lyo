using Lyo.Common.Core.Net;

namespace Lyo.Common.Tests;

public sealed class UserAgentGeneratorTests
{
    [Fact]
    public void Generate_Chrome_UsesHostOs()
    {
        var ua = UserAgentGenerator.Generate(UserAgentFormat.Chromium, UserAgentGenerator.ChromeVersion);
        Assert.Contains($"Chrome/{UserAgentGenerator.ChromeVersion}", ua, StringComparison.Ordinal);
        Assert.Contains("Safari/537.36", ua, StringComparison.Ordinal);
        Assert.Contains("X11; Linux x86_64", ua, StringComparison.Ordinal);
        Assert.DoesNotContain("Edg/", ua, StringComparison.Ordinal);
        Assert.DoesNotContain("Firefox/", ua, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_Edge_AppendsEdg()
    {
        var ua = UserAgentGenerator.Generate(UserAgentFormat.Edge, UserAgentGenerator.ChromeVersion);
        Assert.Contains($"Chrome/{UserAgentGenerator.ChromeVersion}", ua, StringComparison.Ordinal);
        Assert.Contains($"Edg/{UserAgentGenerator.ChromeVersion}", ua, StringComparison.Ordinal);
        Assert.Contains("Safari/537.36", ua, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_Firefox_UsesGecko()
    {
        var ua = UserAgentGenerator.Generate(UserAgentFormat.Firefox, UserAgentGenerator.FirefoxVersion);
        Assert.Contains($"rv:{UserAgentGenerator.FirefoxVersion}", ua, StringComparison.Ordinal);
        Assert.Contains($"Firefox/{UserAgentGenerator.FirefoxVersion}", ua, StringComparison.Ordinal);
        Assert.DoesNotContain("Chrome/", ua, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_WebKit_UsesSafariVersion()
    {
        var ua = UserAgentGenerator.Generate(UserAgentFormat.Safari, UserAgentGenerator.SafariVersion);
        Assert.Contains($"Version/{UserAgentGenerator.SafariVersion}", ua, StringComparison.Ordinal);
        Assert.Contains("Safari/605.1.15", ua, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_VersionOverride_ReplacesTemplateVersion()
    {
        var ua = UserAgentGenerator.Generate(UserAgentFormat.Chromium, "99.0.1.2");
        Assert.Contains("Chrome/99.0.1.2", ua, StringComparison.Ordinal);
        Assert.DoesNotContain(UserAgentGenerator.ChromeVersion, ua, StringComparison.Ordinal);
    }
}