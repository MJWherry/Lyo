namespace Lyo.Rfid.Tonie.Tests;

public sealed class ToniePrivacyTests
{
    [Fact]
    public void NxpDefault_Is0f0f0f0f() => Assert.Equal(new byte[] { 0x0F, 0x0F, 0x0F, 0x0F }, ToniePrivacy.NxpDefault.ToArray());
}
