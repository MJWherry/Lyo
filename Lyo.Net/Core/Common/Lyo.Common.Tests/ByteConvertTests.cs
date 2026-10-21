using System.Text;
using Lyo.Common.Core.Conversion;
using Lyo.Common.Core.Enums;

namespace Lyo.Common.Tests;

public class ByteConvertTests
{
    [Fact]
    public void ToHex_Empty_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, ByteConvert.ToHex(ReadOnlySpan<byte>.Empty));
        Assert.Equal(string.Empty, ByteConvert.ToHex([]));
    }

    [Fact]
    public void ToHex_KnownByte_MatchesCase()
    {
        var bytes = new byte[] { 0xAB, 0x0F };
        Assert.Equal("AB0F", ByteConvert.ToHex(bytes));
        Assert.Equal("ab0f", ByteConvert.ToHex(bytes, TextLetterCase.Lower));
    }

    [Fact]
    public void ToHex_NullArray_ThrowsArgumentNullException() => Assert.Throws<ArgumentNullException>(() => ByteConvert.ToHex(null!));

    [Fact]
    public void TryToHex_ShortDestination_ReturnsFalse()
    {
        Span<char> destination = stackalloc char[1];
        Assert.False(ByteConvert.TryToHex([0xAB], destination, out var written));
        Assert.Equal(0, written);
    }

    [Fact]
    public void TryToHex_KnownByte_WritesCase()
    {
        var bytes = new byte[] { 0xAB, 0x0F };
        Span<char> upper = stackalloc char[4];
        Assert.True(ByteConvert.TryToHex(bytes, upper, out var written));
        Assert.Equal(4, written);
        Assert.Equal("AB0F", new(upper));
        Span<char> lower = stackalloc char[4];
        Assert.True(ByteConvert.TryToHex(bytes, lower, out written, TextLetterCase.Lower));
        Assert.Equal("ab0f", new(lower));
    }

    [Fact]
    public void FromHex_Empty_ReturnsEmpty() => Assert.Empty(ByteConvert.FromHex(ReadOnlySpan<char>.Empty));

    [Fact]
    public void FromHex_RoundTrip_AcceptsEitherCase()
    {
        var bytes = new byte[] { 0xAB, 0x0F };
        Assert.Equal(bytes, ByteConvert.FromHex("AB0F"));
        Assert.Equal(bytes, ByteConvert.FromHex("ab0f"));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("zz")]
    [InlineData("A B")]
    public void FromHex_Invalid_ThrowsFormatException(string hex) => Assert.Throws<FormatException>(() => ByteConvert.FromHex(hex));

    [Fact]
    public void FromHex_Null_ThrowsArgumentNullException() => Assert.Throws<ArgumentNullException>(() => ByteConvert.FromHex(null!));

    [Fact]
    public void TryFromHex_OddLength_ReturnsFalse()
    {
        Span<byte> destination = stackalloc byte[4];
        Assert.False(ByteConvert.TryFromHex("abc", destination, out var written));
        Assert.Equal(0, written);
    }

    [Fact]
    public void TryFromHex_ShortDestination_ReturnsFalse()
    {
        Span<byte> destination = stackalloc byte[1];
        Assert.False(ByteConvert.TryFromHex("aabb", destination, out var written));
        Assert.Equal(0, written);
    }

    [Fact]
    public void GetBytes_Utf8_RoundTrips()
    {
        var bytes = ByteConvert.GetBytes("café");
        Assert.Equal("café", ByteConvert.GetString(bytes));
        Assert.Equal("café", ByteConvert.GetString(bytes.AsSpan()));
    }

    [Fact]
    public void GetBytes_Ascii_ReplacesNonAscii()
    {
        var bytes = ByteConvert.GetBytes("Aé", Encoding.ASCII);
        Assert.Equal(new byte[] { 0x41, 0x3F }, bytes);
        Assert.Equal("A?", ByteConvert.GetString(bytes, Encoding.ASCII));
    }

    [Fact]
    public void GetBytes_Unicode_RoundTrips()
    {
        var bytes = ByteConvert.GetBytes("hi", Encoding.Unicode);
        Assert.Equal(new byte[] { 0x68, 0x00, 0x69, 0x00 }, bytes);
        Assert.Equal("hi", ByteConvert.GetString(bytes, Encoding.Unicode));
    }

    [Fact]
    public void TryGetBytes_Unicode_ShortDestination_ReturnsFalse()
    {
        Span<byte> destination = stackalloc byte[1];
        Assert.False(ByteConvert.TryGetBytes("hi", destination, out var written, Encoding.Unicode));
        Assert.Equal(0, written);
    }

    [Fact]
    public void GetBytes_Empty_ReturnsEmpty()
    {
        Assert.Empty(ByteConvert.GetBytes(string.Empty));
        Assert.Equal(string.Empty, ByteConvert.GetString(ReadOnlySpan<byte>.Empty));
    }

    [Fact]
    public void GetBytes_Null_ThrowsArgumentNullException() => Assert.Throws<ArgumentNullException>(() => ByteConvert.GetBytes(null!));

    [Fact]
    public void TryGetBytes_ShortDestination_ReturnsFalse()
    {
        Span<byte> destination = stackalloc byte[1];
        Assert.False(ByteConvert.TryGetBytes("café", destination, out var written));
        Assert.Equal(0, written);
    }

    [Fact]
    public void TryGetString_RoundTrips()
    {
        var bytes = ByteConvert.GetBytes("hi");
        Span<char> destination = stackalloc char[2];
        Assert.True(ByteConvert.TryGetString(bytes, destination, out var written));
        Assert.Equal(2, written);
        Assert.Equal("hi", new(destination));
    }
}