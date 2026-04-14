using Lyo.Ffmpeg.Models;

namespace Lyo.Ffmpeg.Tests;

public class FFmpegCommandBuilderTests
{
    [Fact]
    public void Build_WithInputAndOutput_ReturnsCommand()
    {
        var cmd = FFmpegCommandBuilder.New().WithInput("/tmp/input.mp3").WithOutput("/tmp/output.wav").Build();
        Assert.NotNull(cmd);
        Assert.Contains("ffmpeg", cmd.ExecutablePath);
        Assert.Contains("/tmp/input.mp3", cmd.Arguments);
        Assert.Contains("/tmp/output.wav", cmd.Arguments);
        Assert.Contains("-vn", cmd.Arguments);
        Assert.Contains("-acodec", cmd.Arguments);
    }

    [Fact]
    public void Build_WithCustomCodecAndSampleRate_IncludesInArguments()
    {
        var cmd = FFmpegCommandBuilder.New().WithInput("in.wav").WithOutput("out.mp3").WithCodec("libmp3lame").WithSampleRate(22050).WithChannels(1).Build();
        Assert.Contains("libmp3lame", cmd.Arguments);
        Assert.Contains("22050", cmd.Arguments);
        Assert.Contains("-ac", cmd.Arguments);
    }

    [Fact]
    public void Build_WithDefaults_UsesOptions()
    {
        var opts = new FfmpegOptions { DefaultSampleRate = 16000, DefaultChannels = 1, FfmpegPath = "/usr/bin/ffmpeg" };
        var cmd = FFmpegCommandBuilder.New().WithInput("a.wav").WithOutput("b.wav").WithDefaults(opts).Build();
        Assert.Equal("/usr/bin/ffmpeg", cmd.ExecutablePath);
        Assert.Contains("16000", cmd.Arguments);
    }

    [Fact]
    public void Build_WithOverwrite_IncludesYFlag()
    {
        var cmd = FFmpegCommandBuilder.New().WithInput("in.wav").WithOutput("out.wav").Overwrite().Build();
        Assert.Contains("-y", cmd.Arguments);
    }

    [Fact]
    public void Build_WithNoOverwrite_ExcludesYFlag()
    {
        var opts = new FfmpegOptions { DefaultOverwrite = false };
        var cmd = FFmpegCommandBuilder.New().WithInput("in.wav").WithOutput("out.wav").WithDefaults(opts).NoOverwrite().Build();
        Assert.DoesNotContain("-y", cmd.Arguments);
    }

    [Fact]
    public void Build_WithCustomArguments_IncludesThem()
    {
        var cmd = FFmpegCommandBuilder.New().WithInput("in.wav").WithOutput("out.wav").WithArgument("-b:a").WithArgument("128k").Build();
        Assert.Contains("128k", cmd.Arguments);
    }

    [Fact]
    public void Build_WithoutInput_Throws() => Assert.Throws<InvalidOperationException>(() => FFmpegCommandBuilder.New().WithOutput("out.wav").Build());

    [Fact]
    public void Build_WithoutOutput_Throws() => Assert.Throws<InvalidOperationException>(() => FFmpegCommandBuilder.New().WithInput("in.wav").Build());

    [Fact]
    public void ToString_IncludesInputAndOutput()
    {
        var str = FFmpegCommandBuilder.New().WithInput("in.mp3").WithOutput("out.wav").ToString();
        Assert.Contains("in.mp3", str);
        Assert.Contains("out.wav", str);
    }
}