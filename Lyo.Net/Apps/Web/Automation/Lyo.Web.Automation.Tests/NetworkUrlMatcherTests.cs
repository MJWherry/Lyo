using Lyo.Web.Automation.Plan;

namespace Lyo.Web.Automation.Tests;

public sealed class NetworkUrlMatcherTests
{
    [Fact]
    public void Matches_BlankPattern_KeepsEveryUrl()
    {
        Assert.True(NetworkUrlMatcher.Matches(null, "https://cdn.example/a.jpg"));
        Assert.True(NetworkUrlMatcher.Matches("  ", "https://cdn.example/a.jpg"));
    }

    [Fact]
    public void Matches_Regex_KeepsOnlyMatchingUrls()
    {
        Assert.True(NetworkUrlMatcher.Matches(@"\.png$", "https://cdn.example/a.PNG"));
        Assert.False(NetworkUrlMatcher.Matches(@"\.png$", "https://cdn.example/a.jpg"));
    }

    [Fact]
    public void ThrowIfInvalid_BadPattern_Throws()
    {
        var error = Assert.Throws<InvalidOperationException>(() => NetworkUrlMatcher.ThrowIfInvalid("("));
        Assert.Contains("(", error.Message);
    }
}
