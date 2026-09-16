namespace Lyo.Nfc.Pcsc.Tests;

public sealed class PcscNtagCommandsTests
{
    [Fact]
    public void GetUid_Command_IsFfCa000000() => Assert.Equal("FFCA000000", Convert.ToHexString(PcscNtagCommands.GetUid()));

    [Fact]
    public void ReadPages_StartPage4_IsFfB0000410() => Assert.Equal("FFB0000410", Convert.ToHexString(PcscNtagCommands.ReadPages(4)));

    [Fact]
    public void WritePage_Page4_IsFfD6000404PlusData()
    {
        var command = PcscNtagCommands.WritePage(4, [0x11, 0x22, 0x33, 0x44]);
        Assert.Equal("FFD600040411223344", Convert.ToHexString(command));
    }
}
