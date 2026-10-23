namespace Lyo.Rfid.Tonie.Tests;

public sealed class TonieContentPathTests
{
    [Fact]
    public void ContentPath_ExampleUid_FileAndDirectory()
    {
        var path = TonieContentPath.FromUid(Convert.FromHexString("E004035012345678"));
        Assert.Equal("500304E0", path.FileName);
        Assert.Equal("78563412", path.DirectoryName);
    }

    [Fact]
    public void ContentPath_PrefixNotSlixL_Throws() => Assert.Throws<RfidException>(() => TonieContentPath.FromUid(Convert.FromHexString("E004025012345678")));
}