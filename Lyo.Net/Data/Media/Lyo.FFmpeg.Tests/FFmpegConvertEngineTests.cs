using Lyo.Common.Core.Records;
using Lyo.FFmpeg.Models;
using Lyo.Media.Models;

namespace Lyo.FFmpeg.Tests;

public class FFmpegConvertEngineTests
{
    [Fact]
    public void ExtractFrame_WithThumbnailSize_EmitsScaleAndOneFrame()
    {
        var opts = ConvertKnobs.FromVideo(VideoConversionOptions.ForThumbnail(320, 180) with { PixelFormat = PixelFormat.Yuv420p });
        var cmd = FFmpegConvertEngine.BuildExtractFrame(new(), "in.mp4", "frame.jpg", TimeSpan.Zero, opts);
        var args = cmd.ArgumentList.ToList();
        Assert.Contains("-frames:v", args);
        Assert.Equal("1", args[args.IndexOf("-frames:v") + 1]);
        Assert.Contains("scale=320:180:force_original_aspect_ratio=decrease", string.Join(' ', args), StringComparison.Ordinal);
        Assert.Contains("yuv420p", args);
        Assert.DoesNotContain("-hwaccel", args);
    }

    [Fact]
    public void Concat_WritesDemuxerList_AndCopies()
    {
        Assert.Equal("file 'a'\\''b.wav'", FFmpegConcatList.FormatLine("a'b.wav"));
        var knobs = FFmpegConvertEngine.PrepareConcat(ConvertKnobs.FromVideo(new()));
        var cmd = FFmpegCommandBuilder.New().WithInput("list.txt").WithOutput("out.mp4").ApplyKnobs(knobs).Build();
        var args = cmd.ArgumentList.ToList();
        var input = args.IndexOf("-i");
        Assert.True(args.IndexOf("-f") < input);
        Assert.True(args.IndexOf("-safe") < input);
        Assert.Equal("concat", args[args.IndexOf("-f") + 1]);
        Assert.Equal("0", args[args.IndexOf("-safe") + 1]);
        Assert.Contains("copy", args);
    }

    [Fact]
    public void ForGif_UsesPaletteTwoStep()
    {
        var opts = ConvertKnobs.FromVideo(VideoConversionOptions.ForGif());
        var (palette, encode) = FFmpegConvertEngine.BuildGif(new(), "in.mp4", "pal.png", "out.gif", opts);
        Assert.Contains("palettegen", string.Join(' ', palette.ArgumentList), StringComparison.Ordinal);
        Assert.Contains("paletteuse", string.Join(' ', encode.ArgumentList), StringComparison.Ordinal);
        Assert.Throws<ArgumentException>(() => FFmpegConvertEngine.ThrowIfBlocked(opts, new(), true));
        Assert.Throws<ArgumentException>(() => FFmpegConvertEngine.ThrowIfSessionBlocked(opts));
    }

    [Fact]
    public void Loudness_MeasurePass_IgnoresWarningLogLevel()
    {
        var opts = ConvertKnobs.FromVideo(new() { Loudness = LoudnessTarget.Streaming });
        var host = new FFmpegOptions { GlobalArguments = ["-loglevel", "warning"] };
        var cmd = FFmpegConvertEngine.BuildLoudnessMeasure(host, "in.mp4", opts);
        var args = cmd.ArgumentList.ToList();
        Assert.Equal("info", args[args.LastIndexOf("-loglevel") + 1]);
        Assert.Contains("-vn", args);
        Assert.DoesNotContain("-progress", args);
        const string stderr = "warning\n{ \"input_i\": \"-20.0\", \"input_tp\": \"-1.0\", \"input_lra\": \"8.0\", \"input_thresh\": \"-30.0\" }\n";
        Assert.True(FFmpegLoudness.TryParse(stderr, out var measured));
        Assert.Equal("-20.0", measured.Integrated);
        Assert.False(FFmpegLoudness.TryParse("{ \"input_i\": \"-inf\", \"input_tp\": \"-1.0\", \"input_lra\": \"8.0\", \"input_thresh\": \"-30.0\" }", out var _));
    }

    [Fact]
    public void TwoPass_EmitsPass1NullThenPass2()
    {
        var progress = new Progress<MediaProgress>(_ => { });
        var opts = ConvertKnobs.FromVideo(
            new() {
                Encoder = VideoEncoder.H264,
                VideoBitRate = MediaBitRate.M2,
                Format = MediaContainer.Mp4,
                TwoPass = true,
                Progress = progress
            });

        var (pass1, pass2) = FFmpegConvertEngine.BuildTwoPass(new(), "in.mov", "out.mp4", "/tmp/lyo-pass", opts);
        var first = pass1.ArgumentList.ToList();
        var second = pass2.ArgumentList.ToList();
        Assert.Contains("-an", first);
        Assert.Equal("null", first[first.IndexOf("-f") + 1]);
        Assert.Equal("-", first[^1]);
        Assert.Equal("1", first[first.IndexOf("-pass") + 1]);
        Assert.DoesNotContain("-progress", first);
        Assert.DoesNotContain("mp4", first);
        Assert.Equal("out.mp4", second[^1]);
        Assert.Equal("2", second[second.IndexOf("-pass") + 1]);
        Assert.Contains("-progress", second);
        Assert.Equal(["/tmp/lyo-pass-0.log", "/tmp/lyo-pass-0.log.mbtree"], FFmpegConvertEngine.TwoPassLogs("/tmp/lyo-pass"));
        Assert.Throws<ArgumentException>(() => FFmpegConvertEngine.ThrowIfBlocked(opts, new(), true));
        Assert.Throws<ArgumentException>(() => FFmpegConvertEngine.ThrowIfSessionBlocked(opts));
        Assert.Throws<ArgumentException>(() => FFmpegConvertEngine.ThrowIfTwoPass(opts, new() { Hardware = "nvenc" }));
    }
}