using Lyo.Media.Models;

namespace Lyo.FFmpeg.Tests;

public class EncoderCatalogTests
{
    [Fact]
    public void TryFromName_Parallel_DoesNotThrow()
        => Parallel.For(0, 64, _ => {
            Assert.Same(AudioEncoder.Aac, AudioEncoder.TryFromName("Aac"));
            Assert.Same(VideoEncoder.Mpeg4, VideoEncoder.TryFromName("Mpeg4"));
            Assert.Same(PixelFormat.Yuv420p, PixelFormat.TryFromName("Yuv420p"));
            Assert.Same(EncoderPreset.Medium, EncoderPreset.TryFromName("Medium"));
        });

    [Fact]
    public void Custom_ReusesRegisteredInstance()
    {
        Assert.Same(VideoEncoder.H264, VideoEncoder.Custom("h264"));
        var first = VideoEncoder.Custom("libx265");
        var second = VideoEncoder.Custom("LIBX265");
        Assert.Same(first, second);
    }

    [Fact]
    public void TryFromId_Aac_ReturnsBuiltIn()
        => Assert.Same(AudioEncoder.Aac, AudioEncoder.TryFromId("aac"));
}
