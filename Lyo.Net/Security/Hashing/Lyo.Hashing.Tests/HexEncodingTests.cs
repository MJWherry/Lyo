using Lyo.Common.Core.Enums;

namespace Lyo.Hashing.Tests;

public sealed class HexEncodingTests
{
    [Fact]
    public void ToHexString_Empty_Yields_Empty()
    {
        Assert.Equal(string.Empty, HexEncoding.ToHexString(ReadOnlySpan<byte>.Empty));
        Assert.Equal(string.Empty, HexEncoding.ToHexString(Array.Empty<byte>()));
    }

    [Fact]
    public void ToHex_String_Round_Trip_Known_Byte()
    {
        var span = new byte[] { 0xab };
        Assert.Equal("AB", HexEncoding.ToHexString(span));
        Assert.Equal("ab", HexEncoding.ToHexString(span, TextLetterCase.Lower));
        Assert.Equal(span, HexEncoding.FromHex("AB"));
        Assert.Equal(span, HexEncoding.FromHex("ab"));
    }

    [Fact]
    public void TryDecodeHex_Odd_Length_Returns_False()
    {
        Span<byte> dest = stackalloc byte[4];
        Assert.False(HexEncoding.TryDecodeHex("abc", dest, out var w));
        Assert.Equal(0, w);
    }

    [Fact]
    public void TryDecodeHex_Destination_Too_Small_Returns_False()
    {
        Span<byte> dest = stackalloc byte[1];
        Assert.False(HexEncoding.TryDecodeHex("aabb", dest, out var _));
    }

    [Theory]
    [InlineData("zz")]
    [InlineData("a")]
    public void FromHex_InvalidInput_ThrowsFormatException(string hex) => Assert.Throws<FormatException>(() => HexEncoding.FromHex(hex));

    [Fact]
    public void FromHex_NullString_ThrowsArgumentNullException() => Assert.Throws<ArgumentNullException>(() => HexEncoding.FromHex(null!));
}