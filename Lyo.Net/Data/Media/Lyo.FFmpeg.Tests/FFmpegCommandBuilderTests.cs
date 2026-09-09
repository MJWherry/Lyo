using Lyo.Common.Core.Records;
using Lyo.FFmpeg.Models;
using Lyo.Media.Models;

namespace Lyo.FFmpeg.Tests;

public class FFmpegCommandBuilderTests
{
    [Fact]
    public void Build_WithInputAndOutput_DoesNotForcePcmOrDropVideo()
    {
        var cmd = FFmpegCommandBuilder.New().WithInput("/tmp/input.mp3").WithOutput("/tmp/output.wav").Build();
        Assert.Contains("ffmpeg", cmd.ExecutablePath);
        Assert.Contains("/tmp/input.mp3", cmd.ArgumentList);
        Assert.Contains("/tmp/output.wav", cmd.ArgumentList);
        Assert.DoesNotContain("-vn", cmd.ArgumentList);
        Assert.DoesNotContain("-acodec", cmd.ArgumentList);
        Assert.DoesNotContain("-c:a", cmd.ArgumentList);
        Assert.Contains("-y", cmd.ArgumentList);
    }

    [Fact]
    public void Build_WithFormat_EmitsDashFBeforeOutput()
    {
        var cmd = FFmpegCommandBuilder.New().WithInput("in.wav").WithOutput("out.mp3").WithFormat(MediaContainer.Mp3).Build();
        var list = cmd.ArgumentList;
        var f = list.ToList().IndexOf("-f");
        Assert.True(f >= 0);
        Assert.Equal("mp3", list[f + 1]);
        Assert.Equal("out.mp3", list[^1]);
    }

    [Fact]
    public void Build_WithNoOverwrite_EmitsN()
    {
        var cmd = FFmpegCommandBuilder.New().WithInput("in.wav").WithOutput("out.wav").NoOverwrite().Build();
        Assert.Contains("-n", cmd.ArgumentList);
        Assert.DoesNotContain("-y", cmd.ArgumentList);
    }

    [Fact]
    public void Build_ForCompressVideo_EmitsCrfPresetAndCodec()
    {
        var cmd = FFmpegCommandBuilder.New()
            .WithInput("in.mov")
            .WithOutput("out.mp4")
            .ApplyOptions(VideoConversionOptions.ForCompressVideo())
            .Build();
        Assert.Contains("-crf", cmd.ArgumentList);
        Assert.Contains("23", cmd.ArgumentList);
        Assert.Contains("-preset", cmd.ArgumentList);
        Assert.Contains("medium", cmd.ArgumentList);
        Assert.Contains("-c:v", cmd.ArgumentList);
        Assert.Contains("libx264", cmd.ArgumentList);
        Assert.Contains("-f", cmd.ArgumentList);
        Assert.Contains("mp4", cmd.ArgumentList);
    }

    [Fact]
    public void Build_ForTranscode_EmitsFormatAndCodecs()
    {
        var cmd = FFmpegCommandBuilder.New()
            .WithInput("in.mkv")
            .WithOutput("out.webm")
            .ApplyOptions(VideoConversionOptions.ForTranscode(MediaContainer.WebM, AudioEncoder.Opus, VideoEncoder.Vp9))
            .Build();
        Assert.Contains("-f", cmd.ArgumentList);
        Assert.Contains("webm", cmd.ArgumentList);
        Assert.Contains("-c:a", cmd.ArgumentList);
        Assert.Contains("libopus", cmd.ArgumentList);
        Assert.Contains("-c:v", cmd.ArgumentList);
        Assert.Contains("libvpx-vp9", cmd.ArgumentList);
        Assert.DoesNotContain("-crf", cmd.ArgumentList);
        Assert.DoesNotContain("-vn", cmd.ArgumentList);
    }

    [Fact]
    public void Build_ForCompressAudio_EmitsBitrateAndDropVideo()
    {
        var cmd = FFmpegCommandBuilder.New()
            .WithInput("in.wav")
            .WithOutput("out.mp3")
            .ApplyOptions(AudioConversionOptions.ForCompressAudio())
            .Build();
        Assert.Contains("-b:a", cmd.ArgumentList);
        Assert.Contains("128k", cmd.ArgumentList);
        Assert.Contains("-c:a", cmd.ArgumentList);
        Assert.Contains("libmp3lame", cmd.ArgumentList);
        Assert.Contains("-vn", cmd.ArgumentList);
        Assert.Contains("mp3", cmd.ArgumentList);
    }

    [Fact]
    public void Build_CopyCodecs_WithSideAudioEncoder_EmitsBoth()
    {
        var cmd = FFmpegCommandBuilder.New()
            .WithInput("in.mp4")
            .WithOutput("out.mp4")
            .ApplyOptions(new VideoConversionOptions { CopyCodecs = true, AudioEncoder = AudioEncoder.Aac })
            .Build();
        Assert.Contains("-c", cmd.ArgumentList);
        Assert.Contains("copy", cmd.ArgumentList);
        Assert.Contains("-c:a", cmd.ArgumentList);
        Assert.Contains("aac", cmd.ArgumentList);
    }

    [Fact]
    public void Build_InputKnobs_EmitBeforeDashI()
    {
        var cmd = FFmpegCommandBuilder.New()
            .WithInput("pipe:0")
            .WithOutput("out.wav")
            .ApplyOptions(new AudioConversionOptions { InputFormat = MediaContainer.S16le, InputSampleRate = 48000, InputChannels = 2 })
            .Build();
        var list = cmd.ArgumentList.ToList();
        var i = list.IndexOf("-i");
        var f = list.IndexOf("-f");
        var ar = list.IndexOf("-ar");
        var ac = list.IndexOf("-ac");
        Assert.True(f < i);
        Assert.True(ar < i);
        Assert.True(ac < i);
        Assert.Equal("s16le", list[f + 1]);
        Assert.Equal("48000", list[ar + 1]);
        Assert.Equal("2", list[ac + 1]);
    }

    [Fact]
    public void Build_WithSize_EmitsDashS()
    {
        var cmd = FFmpegCommandBuilder.New().WithInput("in").WithOutput("out").WithSize(640, 360).Build();
        Assert.Contains("-s", cmd.ArgumentList);
        Assert.Contains("640x360", cmd.ArgumentList);
    }

    [Fact]
    public void Build_CrfAndBitrate_AreSeparateFlags()
    {
        var cmd = FFmpegCommandBuilder.New().WithInput("in").WithOutput("out").WithCrf(18).WithVideoBitRate("2M").Build();
        Assert.Contains("-crf", cmd.ArgumentList);
        Assert.Contains("-b:v", cmd.ArgumentList);
        Assert.Contains("2M", cmd.ArgumentList);
    }

    [Fact]
    public void Build_ForRawPcm_EmitsPipeShapedAudio()
    {
        var cmd = FFmpegCommandBuilder.New()
            .WithInput("in.mp3")
            .WithOutput("pipe:1")
            .ApplyOptions(AudioConversionOptions.ForRawPcm())
            .Build();
        Assert.Contains("-vn", cmd.ArgumentList);
        Assert.Contains("-c:a", cmd.ArgumentList);
        Assert.Contains("pcm_s16le", cmd.ArgumentList);
        Assert.Contains("s16le", cmd.ArgumentList);
        Assert.Contains("48000", cmd.ArgumentList);
        Assert.Contains("pipe:1", cmd.ArgumentList);
    }

    [Fact]
    public void Build_ForStreamingVideo_EmitsCopyAndMovflags()
    {
        var cmd = FFmpegCommandBuilder.New()
            .WithInput("in.mp4")
            .WithOutput("pipe:1")
            .ApplyOptions(VideoConversionOptions.ForStreamingVideo())
            .Build();
        Assert.Contains("-c", cmd.ArgumentList);
        Assert.Contains("copy", cmd.ArgumentList);
        Assert.Contains("-movflags", cmd.ArgumentList);
        Assert.Contains("+frag_keyframe+empty_moov+default_base_moof", cmd.ArgumentList);
        Assert.Contains("mp4", cmd.ArgumentList);
    }

    [Fact]
    public void Build_ForStreamingVideo_WebM_OmitsMovflags()
    {
        var cmd = FFmpegCommandBuilder.New()
            .WithInput("in.webm")
            .WithOutput("pipe:1")
            .ApplyOptions(VideoConversionOptions.ForStreamingVideo(MediaContainer.WebM))
            .Build();
        Assert.Contains("-c", cmd.ArgumentList);
        Assert.Contains("copy", cmd.ArgumentList);
        Assert.Contains("webm", cmd.ArgumentList);
        Assert.DoesNotContain("-movflags", cmd.ArgumentList);
    }

    [Fact]
    public void Build_Trim_EmitsSsBeforeInputAndTAfter()
    {
        var cmd = FFmpegCommandBuilder.New()
            .WithInput("in.mp4")
            .WithOutput("out.mp4")
            .WithStartTime(TimeSpan.FromSeconds(1.5))
            .WithDuration(TimeSpan.FromSeconds(3))
            .Build();
        var list = cmd.ArgumentList.ToList();
        var ss = list.IndexOf("-ss");
        var i = list.IndexOf("-i");
        var t = list.IndexOf("-t");
        Assert.True(ss < i);
        Assert.True(t > i);
    }

    [Fact]
    public void Build_LavfiInput_DoesNotRequireFile()
    {
        var cmd = FFmpegCommandBuilder.New()
            .WithInputFormat("lavfi")
            .WithInput("testsrc=duration=1:size=160x120:rate=1")
            .WithOutput("out.mp4")
            .Build();
        Assert.Contains("-f", cmd.ArgumentList);
        Assert.Contains("lavfi", cmd.ArgumentList);
        Assert.Contains("testsrc=duration=1:size=160x120:rate=1", cmd.ArgumentList);
    }

    [Fact]
    public void Build_WithDefaults_UsesExecutablePathNotDefaultCodec()
    {
        var opts = new FFmpegOptions { DefaultSampleRate = 16000, FFmpegPath = "/usr/bin/ffmpeg" };
        var cmd = FFmpegCommandBuilder.New().WithInput("a.wav").WithOutput("b.wav").WithDefaults(opts).Build();
        Assert.Equal("/usr/bin/ffmpeg", cmd.ExecutablePath);
        Assert.DoesNotContain("16000", cmd.ArgumentList);
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
