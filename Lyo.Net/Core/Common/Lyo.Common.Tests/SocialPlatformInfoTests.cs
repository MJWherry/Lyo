using System.Text.Json;
using Lyo.Common.JsonConverters;
using Lyo.Common.Records;

namespace Lyo.Common.Tests;

public class SocialPlatformInfoTests
{
    [Fact]
    public void StaticRegistry_ContainsExpectedMetadata()
    {
        var info = SocialPlatformInfo.LinkedIn;
        Assert.Equal("LinkedIn", info.Name);
        Assert.Equal("linkedin", info.Slug);
        Assert.Equal("https://www.linkedin.com", info.WebsiteUri);
        Assert.Equal("https://www.linkedin.com/in/{username}", info.ProfileUriTemplate);
        Assert.False(info.IsFederated);
    }

    [Fact]
    public void FromSlug_ResolvesKnownPlatforms()
    {
        Assert.Equal(SocialPlatformInfo.LinkedIn, SocialPlatformInfo.FromSlug("linkedin"));
        Assert.Equal(SocialPlatformInfo.GitHub, SocialPlatformInfo.FromSlug("github"));
    }

    [Theory]
    [InlineData("twitter")]
    [InlineData("x")]
    public void FromAlias_ResolvesX(string alias)
    {
        Assert.Equal(SocialPlatformInfo.X, SocialPlatformInfo.FromAlias(alias));
    }

    [Fact]
    public void FromSlug_UnknownSlug_ReturnsUnknown()
    {
        Assert.Equal(SocialPlatformInfo.Unknown, SocialPlatformInfo.FromSlug("not-a-platform"));
    }

    [Fact]
    public void All_ExcludesUnknown()
    {
        Assert.DoesNotContain(SocialPlatformInfo.Unknown, SocialPlatformInfo.All);
        Assert.Contains(SocialPlatformInfo.LinkedIn, SocialPlatformInfo.All);
        Assert.Contains(SocialPlatformInfo.Other, SocialPlatformInfo.All);
    }

    [Fact]
    public void TryBuildProfileUri_GitHub_BuildsExpectedUrl()
    {
        Assert.Equal("https://github.com/octocat", SocialPlatformInfo.GitHub.TryBuildProfileUri("octocat"));
    }

    [Fact]
    public void TryBuildProfileUri_StripsLeadingAtSign()
    {
        Assert.Equal("https://github.com/octocat", SocialPlatformInfo.GitHub.TryBuildProfileUri("@octocat"));
    }

    [Fact]
    public void TryBuildProfileUri_MastodonFederated_BuildsExpectedUrl()
    {
        Assert.Equal(
            "https://mastodon.social/@alice",
            SocialPlatformInfo.Mastodon.TryBuildProfileUri("alice@mastodon.social"));
    }

    [Fact]
    public void TryBuildProfileUri_MastodonIncompleteHandle_ReturnsNull()
    {
        Assert.Null(SocialPlatformInfo.Mastodon.TryBuildProfileUri("alice"));
    }

    [Fact]
    public void TryBuildProfileUri_Discord_ReturnsNull()
    {
        Assert.Null(SocialPlatformInfo.Discord.TryBuildProfileUri("someuser"));
    }

    [Fact]
    public void TryBuildProfileUri_BlankUsername_ReturnsNull()
    {
        Assert.Null(SocialPlatformInfo.GitHub.TryBuildProfileUri("   "));
    }

    [Fact]
    public void JsonConverter_RoundTripsViaSlug()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new SocialPlatformInfoJsonConverter());

        var json = JsonSerializer.Serialize(SocialPlatformInfo.LinkedIn, options);
        Assert.Equal("\"linkedin\"", json);

        var restored = JsonSerializer.Deserialize<SocialPlatformInfo>(json, options);
        Assert.Equal(SocialPlatformInfo.LinkedIn, restored);
    }

    [Fact]
    public void JsonConverter_UnknownSerializesAsNull()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new SocialPlatformInfoJsonConverter());

        var json = JsonSerializer.Serialize(SocialPlatformInfo.Unknown, options);
        Assert.Equal("null", json);
    }
}
