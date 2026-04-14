using Lyo.Encryption.Security;

namespace Lyo.Encryption.Tests;

public class SecurityUtilitiesTests
{
    [Fact]
    public void ConstantTimeEquals_BothNull_ReturnsTrue() => Assert.True(SecurityUtilities.ConstantTimeEquals(null, null));

    [Fact]
    public void ConstantTimeEquals_OneNull_ReturnsFalse()
    {
        var a = new byte[] { 1, 2, 3 };
        Assert.False(SecurityUtilities.ConstantTimeEquals(a, null));
        Assert.False(SecurityUtilities.ConstantTimeEquals(null, a));
    }

    [Fact]
    public void ConstantTimeEquals_EqualArrays_ReturnsTrue()
    {
        var a = new byte[] { 1, 2, 3, 4, 5 };
        var b = new byte[] { 1, 2, 3, 4, 5 };
        Assert.True(SecurityUtilities.ConstantTimeEquals(a, b));
    }

    [Fact]
    public void ConstantTimeEquals_DifferentLengths_ReturnsFalse()
    {
        var a = new byte[] { 1, 2, 3 };
        var b = new byte[] { 1, 2, 3, 4 };
        Assert.False(SecurityUtilities.ConstantTimeEquals(a, b));
    }

    [Fact]
    public void ConstantTimeEquals_DifferentContent_ReturnsFalse()
    {
        var a = new byte[] { 1, 2, 3 };
        var b = new byte[] { 1, 2, 4 };
        Assert.False(SecurityUtilities.ConstantTimeEquals(a, b));
    }

    [Fact]
    public void ConstantTimeEquals_EmptyArrays_ReturnsTrue()
    {
        var a = Array.Empty<byte>();
        var b = Array.Empty<byte>();
        Assert.True(SecurityUtilities.ConstantTimeEquals(a, b));
    }

    [Fact]
    public void ConstantTimeEquals_Span_Equal_ReturnsTrue()
    {
        ReadOnlySpan<byte> a = "hello"u8.ToArray();
        ReadOnlySpan<byte> b = "hello"u8.ToArray();
        Assert.True(SecurityUtilities.ConstantTimeEquals(a, b));
    }

    [Fact]
    public void ConstantTimeEquals_Span_Different_ReturnsFalse()
    {
        ReadOnlySpan<byte> a = "hello"u8.ToArray();
        ReadOnlySpan<byte> b = "world"u8.ToArray();
        Assert.False(SecurityUtilities.ConstantTimeEquals(a, b));
    }

    [Fact]
    public void ConstantTimeEquals_Span_DifferentLength_ReturnsFalse()
    {
        ReadOnlySpan<byte> a = "hi"u8.ToArray();
        ReadOnlySpan<byte> b = "hello"u8.ToArray();
        Assert.False(SecurityUtilities.ConstantTimeEquals(a, b));
    }

    [Fact]
    public void Clear_ByteArray_ZeroesArray()
    {
        var data = new byte[] { 1, 2, 3, 4, 5 };
        SecurityUtilities.Clear(data);
        Assert.All(data, b => Assert.Equal(0, b));
    }

    [Fact]
    public void Clear_Null_DoesNotThrow() => SecurityUtilities.Clear(null);

    [Fact]
    public void Clear_EmptyArray_DoesNotThrow()
    {
        var data = Array.Empty<byte>();
        SecurityUtilities.Clear(data);
    }
}