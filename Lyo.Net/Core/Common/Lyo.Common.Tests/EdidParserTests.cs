using System.Text;
using Lyo.Common.SystemInformation;

namespace Lyo.Common.Tests;

public class EdidParserTests
{
    [Fact]
    public void IsValid_ValidBlob_ReturnsTrue() => Assert.True(EdidParser.IsValid(CreateEdid()));

    [Fact]
    public void IsValid_BadHeader_ReturnsFalse()
    {
        var edid = CreateEdid();
        edid[1] = 0x00;
        Assert.False(EdidParser.IsValid(edid));
    }

    [Fact]
    public void IsValid_Truncated_ReturnsFalse() => Assert.False(EdidParser.IsValid(new byte[64]));

    [Fact]
    public void IsValid_Null_ReturnsFalse() => Assert.False(EdidParser.IsValid(null));

    [Fact]
    public void GetManufacturerId_DecodesPackedLetters()
        =>
            // "AUO" packs to 0x06AF (A=1, U=21, O=15 as 5-bit letters, big-endian).
            Assert.Equal("AUO", EdidParser.GetManufacturerId(CreateEdid()));

    [Fact]
    public void GetManufacturerId_BadHeader_ReturnsNull()
    {
        var edid = CreateEdid();
        edid[0] = 0xFF;
        Assert.Null(EdidParser.GetManufacturerId(edid));
    }

    [Fact]
    public void GetManufacturerId_OutOfRangeLetter_ReturnsNull()
    {
        var edid = CreateEdid();
        edid[8] = 0x00;
        edid[9] = 0x00;
        Assert.Null(EdidParser.GetManufacturerId(edid));
    }

    [Fact]
    public void GetModelName_ReadsProductNameDescriptor() => Assert.Equal("TestPanel", EdidParser.GetModelName(CreateEdid()));

    [Fact]
    public void GetModelName_UsesLaterDescriptorBlocks()
    {
        var edid = CreateEdid(false);
        WriteModelDescriptor(edid, 108, "LatePanel");
        Assert.Equal("LatePanel", EdidParser.GetModelName(edid));
    }

    [Fact]
    public void GetModelName_NoDescriptor_ReturnsNull() => Assert.Null(EdidParser.GetModelName(CreateEdid(false)));

    [Fact]
    public void GetModelName_BadHeader_ReturnsNull()
    {
        var edid = CreateEdid();
        edid[7] = 0xFF;
        Assert.Null(EdidParser.GetModelName(edid));
    }

    private static byte[] CreateEdid(bool includeModelDescriptor = true)
    {
        var edid = new byte[128];
        edid[0] = 0x00;
        for (var i = 1; i <= 6; i++)
            edid[i] = 0xFF;

        edid[7] = 0x00;

        // Manufacturer "AUO"
        edid[8] = 0x06;
        edid[9] = 0xAF;
        if (includeModelDescriptor)
            WriteModelDescriptor(edid, 54, "TestPanel");

        return edid;
    }

    private static void WriteModelDescriptor(byte[] edid, int offset, string name)
    {
        edid[offset] = 0x00;
        edid[offset + 1] = 0x00;
        edid[offset + 2] = 0x00;
        edid[offset + 3] = 0xFC;
        edid[offset + 4] = 0x00;
        var bytes = Encoding.ASCII.GetBytes(name);
        Array.Copy(bytes, 0, edid, offset + 5, bytes.Length);
        edid[offset + 5 + bytes.Length] = 0x0A;
        for (var i = offset + 6 + bytes.Length; i < offset + 18; i++)
            edid[i] = 0x20;
    }
}