using Lyo.Media.Models;

namespace Lyo.FFmpeg.Tests;

public class PcmFrameReaderTests
{
    [Fact]
    public async Task ReadFramesAsync_SplitsExactFrames()
    {
        var frameSize = new PcmFrameReader(Stream.Null).FrameSizeBytes;
        Assert.Equal(3840, frameSize);
        var payload = new byte[frameSize * 2];
        payload[0] = 1;
        payload[frameSize] = 2;
        var reader = new PcmFrameReader(new MemoryStream(payload, writable: false));
        var frames = new List<byte[]>();
        await foreach (var frame in reader.ReadFramesAsync(TestContext.Current.CancellationToken))
            frames.Add(frame);

        Assert.Equal(2, frames.Count);
        Assert.Equal(1, frames[0][0]);
        Assert.Equal(2, frames[1][0]);
        Assert.Equal(frameSize, frames[0].Length);
    }

    [Fact]
    public async Task ReadFramesAsync_YieldsPartialLastFrame()
    {
        var frameSize = new PcmFrameReader(Stream.Null, sampleRate: 8000, channels: 1, frameDuration: TimeSpan.FromMilliseconds(20)).FrameSizeBytes;
        var payload = new byte[frameSize + 10];
        var reader = new PcmFrameReader(new MemoryStream(payload, writable: false), 8000, 1, TimeSpan.FromMilliseconds(20));
        var frames = new List<byte[]>();
        await foreach (var frame in reader.ReadFramesAsync(TestContext.Current.CancellationToken))
            frames.Add(frame);

        Assert.Equal(2, frames.Count);
        Assert.Equal(frameSize, frames[0].Length);
        Assert.Equal(10, frames[1].Length);
    }
}
