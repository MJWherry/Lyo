using Lyo.Exceptions.Models;

namespace Lyo.Exceptions.Tests;

public class UriHelpersTests
{
    [Fact]
    public void ThrowIfInvalidUri_ValidAbsolute_DoesNotThrow() => UriHelpers.ThrowIfInvalidUri("https://example.com");

    [Fact]
    public void ThrowIfInvalidUri_Null_ThrowsArgumentNull()
    {
        string? uri = null;
        Assert.Throws<ArgumentNullException>(() => UriHelpers.ThrowIfInvalidUri(uri));
    }

    [Fact]
    public void ThrowIfInvalidUri_Whitespace_ThrowsArgument()
        => Assert.Throws<ArgumentException>(() => UriHelpers.ThrowIfInvalidUri("   "));

    [Fact]
    public void ThrowIfInvalidUri_AbsoluteKind_Invalid_ThrowsInvalidFormat()
        => Assert.Throws<InvalidFormatException>(() => UriHelpers.ThrowIfInvalidUri("not a uri", uriKind: UriKind.Absolute));

    [Fact]
    public void ThrowIfInvalidAbsoluteUri_Relative_Throws()
    {
        // No leading slash: on Unix a rooted path parses as an absolute file URI.
        var ex = Assert.Throws<InvalidFormatException>(() => UriHelpers.ThrowIfInvalidAbsoluteUri("relative/path"));
        Assert.Contains("Invalid absolute URI format", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ThrowIfInvalidAbsoluteUri_Absolute_DoesNotThrow() => UriHelpers.ThrowIfInvalidAbsoluteUri("https://example.com/path");

    [Fact]
    public void GetValidUri_Absolute_ReturnsUri()
    {
        var uri = UriHelpers.GetValidUri("https://example.com/path");
        Assert.True(uri.IsAbsoluteUri);
        Assert.Equal("example.com", uri.Host);
    }

    [Fact]
    public void GetValidUri_InvalidAbsolute_Throws()
        => Assert.Throws<InvalidFormatException>(() => UriHelpers.GetValidUri("not a uri"));

    [Fact]
    public void GetValidRelativeUri_Relative_ReturnsUri()
    {
        var uri = UriHelpers.GetValidRelativeUri("/path/to/resource");
        Assert.False(uri.IsAbsoluteUri);
    }

    [Fact]
    public void GetValidRelativeUri_Absolute_Throws()
        => Assert.Throws<InvalidFormatException>(() => UriHelpers.GetValidRelativeUri("https://example.com"));

    [Fact]
    public void TryGetValidUri_Valid_ReturnsTrue()
    {
        Assert.True(UriHelpers.TryGetValidUri("https://example.com", out var uri));
        Assert.NotNull(uri);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not a uri")]
    public void TryGetValidUri_Invalid_ReturnsFalse(string? value)
    {
        Assert.False(UriHelpers.TryGetValidUri(value, out var uri));
        Assert.Null(uri);
    }

    [Fact]
    public void GetValidUriWithScheme_MatchingScheme_ReturnsUri()
        => Assert.Equal("ftp", UriHelpers.GetValidUriWithScheme("ftp://files.example.com", "ftp").Scheme);

    [Fact]
    public void GetValidUriWithScheme_SchemeCaseInsensitive_ReturnsUri()
        => UriHelpers.GetValidUriWithScheme("https://example.com", "HTTPS");

    [Fact]
    public void GetValidUriWithScheme_Mismatch_Throws()
    {
        var ex = Assert.Throws<InvalidFormatException>(() => UriHelpers.GetValidUriWithScheme("http://example.com", "https"));
        Assert.Contains("must use the 'https' scheme", ex.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("http://example.com")]
    [InlineData("https://example.com")]
    public void GetValidWebUri_HttpOrHttps_ReturnsUri(string uri) => UriHelpers.GetValidWebUri(uri);

    [Fact]
    public void GetValidWebUri_Ftp_Throws()
        => Assert.Throws<InvalidFormatException>(() => UriHelpers.GetValidWebUri("ftp://example.com"));

    [Fact]
    public void ValidateAbsoluteUri_Invalid_Throws()
        => Assert.Throws<InvalidFormatException>(() => UriHelpers.ValidateAbsoluteUri("relative/path"));

    [Fact]
    public void ValidateUri_Valid_DoesNotThrow() => UriHelpers.ValidateUri("https://example.com");

    [Theory]
    [InlineData("https://api.example.com", "users", "https://api.example.com/users")]
    [InlineData("https://api.example.com/", "/users", "https://api.example.com/users")]
    [InlineData("https://api.example.com", null, "https://api.example.com/")]
    [InlineData("https://api.example.com", "/", "https://api.example.com")]
    public void CombineUri_CombinesHandlingSlashes(string baseUri, string? path, string expected)
        => Assert.Equal(expected, UriHelpers.CombineUri(baseUri, path));

    [Fact]
    public void CombineUri_InvalidBase_Throws()
        => Assert.Throws<InvalidFormatException>(() => UriHelpers.CombineUri("not a uri", "users"));

    [Theory]
    [InlineData("https://x/p", "a=1", "https://x/p?a=1")]
    [InlineData("https://x/p?a=1", "b=2", "https://x/p?a=1&b=2")]
    [InlineData("https://x/p?", "a=1", "https://x/p?a=1")]
    [InlineData("https://x/p", null, "https://x/p")]
    [InlineData("https://x/p", "", "https://x/p")]
    public void AppendQueryString_AppendsWithCorrectSeparator(string uri, string? query, string expected)
        => Assert.Equal(expected, UriHelpers.AppendQueryString(uri, query));

    [Fact]
    public void AppendQueryString_NullUri_Throws()
        => Assert.Throws<ArgumentNullException>(() => UriHelpers.AppendQueryString(null, "a=1"));

    [Fact]
    public void TryCombineUri_ValidBase_ReturnsTrue()
    {
        Assert.True(UriHelpers.TryCombineUri("https://api.example.com", "users", out var combined));
        Assert.Equal("https://api.example.com/users", combined);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not a uri")]
    public void TryCombineUri_InvalidBase_ReturnsFalse(string? baseUri)
    {
        Assert.False(UriHelpers.TryCombineUri(baseUri, "users", out var combined));
        Assert.Null(combined);
    }
}
