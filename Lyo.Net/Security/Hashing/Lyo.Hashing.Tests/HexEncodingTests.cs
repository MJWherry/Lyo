using Lyo.Common.Enums;

namespace Lyo.Hashing.Tests;

public sealed class HexEncodingTests
{
    [Fact]
    public void ToHexString_empty_yields_empty()
    {
        Assert.Equal(string.Empty, HexEncoding.ToHexString(ReadOnlySpan<byte>.Empty));
        Assert.Equal(string.Empty, HexEncoding.ToHexString(Array.Empty<byte>()));
    }

    [Fact]
    public void ToHex_string_round_trip_known_byte()
    {
        var span = new byte[] { 0xab };
        Assert.Equal("AB", HexEncoding.ToHexString(span));
        Assert.Equal("ab", HexEncoding.ToHexString(span, TextLetterCase.Lower));
        Assert.Equal(span, HexEncoding.FromHex("AB"));
        Assert.Equal(span, HexEncoding.FromHex("ab"));
    }

    [Fact]
    public void TryDecodeHex_odd_length_returns_false()
    {
        Span<byte> dest = stackalloc byte[4];
        Assert.False(HexEncoding.TryDecodeHex("abc", dest, out var w));
        Assert.Equal(0, w);
    }

    [Fact]
    public void TryDecodeHex_destination_too_small_returns_false()
    {
        Span<byte> dest = stackalloc byte[1];
        Assert.False(HexEncoding.TryDecodeHex("aabb", dest, out var _));
    }

    [Fact]
    public void FromHex_invalid_char_throws_FormatException() => Assert.Throws<FormatException>(() => HexEncoding.FromHex("zz"));

    [Fact]
    public void FromHex_odd_length_throws_FormatException() => Assert.Throws<FormatException>(() => HexEncoding.FromHex("a"));

    [Fact]
    public void FromHex_null_string_throws_ArgumentNullException() => Assert.Throws<ArgumentNullException>(() => HexEncoding.FromHex(null!));
}