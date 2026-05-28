using System.Text;
using Lyo.Authentication.Format;
using Lyo.Authentication.Models.Format;

namespace Lyo.Authentication.Tests;

public class Base64UrlTests
{
    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("ab")]
    [InlineData("abc")]
    [InlineData("abcd")]
    [InlineData("Hello, world!")]
    public void EncodeDecode_RoundTripsBytes(string input)
    {
        var encoded = Base64Url.Encode(Encoding.UTF8.GetBytes(input));
        Assert.DoesNotContain("=", encoded);
        Assert.DoesNotContain("+", encoded);
        Assert.DoesNotContain("/", encoded);
        var decoded = Encoding.UTF8.GetString(Base64Url.Decode(encoded));
        Assert.Equal(input, decoded);
    }

    [Fact]
    public void IsValid_RejectsPaddingAndStandardChars()
    {
        Assert.False(Base64Url.IsValid(""));
        Assert.False(Base64Url.IsValid("ab=="));
        Assert.False(Base64Url.IsValid("ab+/"));
        Assert.True(Base64Url.IsValid("ab-_"));
        Assert.True(Base64Url.IsValid("HelloWorld"));
    }
}
