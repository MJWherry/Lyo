using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Lyo.Screenshot.Tests;

public class ShotEncoderTests
{
    [Fact]
    public async Task Encode_TwoFrames_WritesLoopingGif()
    {
        using var first = Frames.Rows(8, 8, 0);
        using var second = Frames.Rows(8, 8, 8);
        var bytes = await ShotEncoder.EncodeGifAsync([first, second], 5, TestCt);
        Assert.Equal((byte)'G', bytes[0]);
        Assert.Equal((byte)'I', bytes[1]);
        Assert.Equal((byte)'F', bytes[2]);
        using var gif = Image.Load<Rgba32>(bytes);
        Assert.Equal(2, gif.Frames.Count);
        Assert.Equal((ushort)0, gif.Metadata.GetGifMetadata().RepeatCount);
        Assert.Equal(20, gif.Frames.RootFrame.Metadata.GetGifMetadata().FrameDelay);
        Assert.Equal(20, gif.Frames[1].Metadata.GetGifMetadata().FrameDelay);
    }

    [Fact]
    public async Task Encode_Paths_KeepsEachFramesColors()
    {
        var directory = Directory.CreateTempSubdirectory("lyo-shot-gif");
        try {
            var redPath = Path.Combine(directory.FullName, "red.png");
            var bluePath = Path.Combine(directory.FullName, "blue.png");
            using (var red = Frames.Flat(8, 8, new(255, 0, 0, 255)))
                await red.SaveAsPngAsync(redPath, TestCt);

            using (var blue = Frames.Flat(8, 8, new(0, 0, 255, 255)))
                await blue.SaveAsPngAsync(bluePath, TestCt);

            var bytes = await ShotEncoder.EncodeGifAsync([redPath, bluePath], 5, TestCt);
            using var gif = Image.Load<Rgba32>(bytes);
            Assert.Equal(2, gif.Frames.Count);
            Assert.True(gif.Frames.RootFrame[0, 0].R > gif.Frames.RootFrame[0, 0].B);
            Assert.Equal(gif.Frames.RootFrame[0, 0], gif.Frames.RootFrame[7, 7]);
            Assert.True(gif.Frames[1][0, 0].B > gif.Frames[1][0, 0].R);
            Assert.Equal(gif.Frames[1][0, 0], gif.Frames[1][7, 7]);
            var preview = ShotEncoder.FirstFramePng(bytes);
            using var png = Image.Load<Rgba32>(preview);
            Assert.Equal(1, png.Frames.Count);
            Assert.True(png[0, 0].R > png[0, 0].B);
        }
        finally {
            directory.Delete(true);
        }
    }

    [Fact]
    public async Task Encode_NoisyFrame_RoundTripsAfterTheColorTableFills()
    {
        var colors = new Rgba32[16];
        for (var i = 0; i < colors.Length; i++)
            colors[i] = new((byte)(i * 16), (byte)(255 - i * 8), (byte)(i * 8), 255);

        using var frame = new Image<Rgba32>(80, 80);
        var seed = 17;
        for (var y = 0; y < frame.Height; y++) {
            for (var x = 0; x < frame.Width; x++) {
                seed = unchecked(seed * 1103515245 + 12345);
                frame[x, y] = colors[(seed >> 16) & 15];
            }
        }

        var bytes = await ShotEncoder.EncodeGifAsync([frame], 10, TestCt);
        using var gif = Image.Load<Rgba32>(bytes);
        Assert.Equal(80, gif.Width);
        for (var y = 0; y < frame.Height; y++) {
            for (var x = 0; x < frame.Width; x++) {
                var expected = Quantized(frame[x, y]);
                var actual = gif[x, y];
                Assert.True(expected == actual, $"({x},{y}) source {frame[x, y]} expected {expected} actual {actual}");
            }
        }
    }

    [Fact]
    public async Task Encode_VariedFrame_KeepsEachRowAfterQuantize()
    {
        using var frame = Frames.Rows(48, 48, 0);
        var bytes = await ShotEncoder.EncodeGifAsync([frame], 5, TestCt);
        using var gif = Image.Load<Rgba32>(bytes);
        Assert.Equal(48, gif.Width);
        Assert.Equal(48, gif.Height);
        for (var y = 0; y < gif.Height; y++) {
            var expected = Quantized(frame[0, y]);
            for (var x = 0; x < gif.Width; x++)
                Assert.Equal(expected, gif[x, y]);
        }
    }

    private static Rgba32 Quantized(Rgba32 pixel) => new(Expand(pixel.R), Expand(pixel.G), Expand(pixel.B), 255);

    private static byte Expand(byte value)
    {
        var band = value >> 3;
        return (byte)((band << 3) | (band >> 2));
    }
}