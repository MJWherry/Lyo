namespace Lyo.Rfid.Tonie.Tests;

public sealed class SlixPrivacyTests
{
    [Fact]
    public void XorPassword_NxpDefaultAndRandom1234_Is1d3b1d3b()
    {
        var xor = SlixPrivacy.XorPassword(ToniePrivacy.NxpDefault.Span, [0x12, 0x34]);
        Assert.Equal(new byte[] { 0x1D, 0x3B, 0x1D, 0x3B }, xor);
    }

    [Fact]
    public void XorPassword_WrongLengths_Throws() => Assert.Throws<ArgumentException>(() => SlixPrivacy.XorPassword(new byte[3], [0x12, 0x34]));
}