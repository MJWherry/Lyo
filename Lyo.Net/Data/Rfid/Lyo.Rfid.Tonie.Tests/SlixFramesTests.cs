namespace Lyo.Rfid.Tonie.Tests;

public sealed class SlixFramesTests
{
    private static readonly byte[] Uid = Convert.FromHexString("E004035012345678");

    [Fact]
    public void GetRandom_Privacy_Frame() => Assert.Equal("22B20478563412500304E004D571", Convert.ToHexString(SlixFrames.GetRandom(Uid)));

    [Fact]
    public void SetPassword_XoredNxpDefault_Frame()
    {
        var xor = SlixPrivacy.XorPassword(ToniePrivacy.NxpDefault.Span, [0x12, 0x34]);
        Assert.Equal("22B30478563412500304E0041D3B1D3B9E49", Convert.ToHexString(SlixFrames.SetPassword(Uid, xor)));
    }

    [Fact]
    public void ReadMultipleBlocks_AllEight_Frame() =>
        Assert.Equal("222378563412500304E00007357A", Convert.ToHexString(SlixFrames.ReadMultipleBlocks(Uid, firstBlock: 0, blockCount: 8)));

    [Fact]
    public void EnablePrivacy_Frame()
    {
        var xor = SlixPrivacy.XorPassword(ToniePrivacy.NxpDefault.Span, [0x12, 0x34]);
        Assert.Equal("22BA0478563412500304E01D3B1D3BBE8F", Convert.ToHexString(SlixFrames.EnablePrivacy(Uid, xor)));
    }
}
