using Lyo.Authentication.Format;

namespace Lyo.Authentication.Tests;

public class Base32CrockfordTests
{
    [Fact]
    public void EncodeRandom11_ProducesExactly11Chars()
    {
        var bytes = new byte[] { 0xde, 0xad, 0xbe, 0xef, 0xca, 0xfe, 0xba, 0xbe };
        var encoded = Base32Crockford.EncodeRandom11(bytes);
        Assert.Equal(11, encoded.Length);
        foreach (var c in encoded) {
            Assert.True(
                (c >= '0' && c <= '9') || (c is >= 'a' and <= 'z' && c is not 'i' and not 'l' and not 'o' and not 'u'),
                $"Unexpected char '{c}' in encoded id '{encoded}'.");
        }
    }

    [Fact]
    public void EncodeRandom11_RequiresExactly8Bytes()
    {
        Assert.Throws<System.ArgumentException>(() => Base32Crockford.EncodeRandom11(new byte[7]));
        Assert.Throws<System.ArgumentException>(() => Base32Crockford.EncodeRandom11(new byte[9]));
    }

    [Fact]
    public void IsValidId_AcceptsExactlyEncodedLength()
    {
        Assert.True(Base32Crockford.IsValidId(Base32Crockford.EncodeRandom11(new byte[8])));
        Assert.False(Base32Crockford.IsValidId(""));
        Assert.False(Base32Crockford.IsValidId("short"));
        Assert.False(Base32Crockford.IsValidId(new string('1', 12)));
    }
}
