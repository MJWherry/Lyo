namespace Lyo.Authentication.Google.Tests;

public sealed class GoogleClaimMapperTests
{
    [Fact]
    public void Map_ReadsCanonicalGoogleClaims()
    {
        var result = GoogleClaimMapper.Map(
            new Dictionary<string, object?> {
                ["sub"] = "abc",
                ["name"] = "Alice Example",
                ["email"] = "alice@example.com",
                ["email_verified"] = true,
                ["picture"] = "https://example/avatar.png",
                ["locale"] = "en"
            });

        Assert.Equal("Alice Example", result.DisplayName);
        Assert.Equal("alice@example.com", result.Email);
        Assert.True(result.EmailVerified);
        Assert.Equal("https://example/avatar.png", result.AvatarUrl);
        Assert.Equal("en", result.PreferredLanguageBcp47);
        Assert.Empty(result.ProviderScopes);
    }

    [Fact]
    public void Map_FallsBackToEmailForDisplayName()
    {
        var result = GoogleClaimMapper.Map(new Dictionary<string, object?> { ["email"] = "alice@example.com" });
        Assert.Equal("alice@example.com", result.DisplayName);
        Assert.False(result.EmailVerified);
    }

    [Fact]
    public void Map_HandlesStringEmailVerified()
    {
        var result = GoogleClaimMapper.Map(new Dictionary<string, object?> { ["email"] = "bob@example.com", ["email_verified"] = "true" });
        Assert.True(result.EmailVerified);
    }

    [Fact]
    public void Map_FallsBackToConstantWhenEverythingMissing()
    {
        var result = GoogleClaimMapper.Map(new Dictionary<string, object?>());
        Assert.Equal("Google user", result.DisplayName);
        Assert.Null(result.Email);
    }
}