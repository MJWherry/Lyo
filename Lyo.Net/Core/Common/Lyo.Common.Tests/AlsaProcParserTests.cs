using Lyo.SystemInformation;

namespace Lyo.Common.Tests;

public class AlsaProcParserTests
{
    private const string Cards = """
                                  0 [PCH            ]: HDA-Intel - HDA Intel PCH
                                                       Realtek ALC287 at 0x1234 irq 17
                                 """;

    [Fact]
    public void Parse_DuplexPcm_ReturnsOneDeviceWithCardName()
    {
        const string pcm = "00-00: ALC287 Analog : ALC287 Analog : playback 1 : capture 1\n";
        var device = Assert.Single(AlsaProcParser.Parse(Cards, pcm));
        Assert.Equal("ALC287 Analog", device.Name);
        Assert.Equal("Duplex", device.Kind);
        Assert.Equal("Realtek ALC287 at 0x1234 irq 17", device.Description);
        Assert.Null(device.FormFactor);
        Assert.Null(device.IsDefault);
    }

    [Fact]
    public void Parse_PlaybackOnlyPcm_KindIsPlayback()
    {
        const string pcm = "00-03: HDMI 0 : HDMI 0 : playback 1\n";
        var device = Assert.Single(AlsaProcParser.Parse("", pcm));
        Assert.Equal("HDMI 0", device.Name);
        Assert.Equal("Playback", device.Kind);
        Assert.Null(device.FormFactor);
        Assert.Null(device.IsDefault);
    }

    [Fact]
    public void Parse_EmptyPcm_ReturnsEmpty()
    {
        Assert.Empty(AlsaProcParser.Parse(Cards, ""));
        Assert.Empty(AlsaProcParser.Parse(Cards, null));
    }
}