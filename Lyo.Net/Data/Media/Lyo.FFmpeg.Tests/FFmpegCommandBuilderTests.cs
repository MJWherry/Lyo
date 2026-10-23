using Lyo.Common.Core.Records;
using Lyo.FFmpeg.Models;
using Lyo.FileTags;
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
        var cmd = FFmpegCommandBuilder.New().WithInput("in.mov").WithOutput("out.mp4").ApplyOptions(VideoConversionOptions.ForCompressVideo()).Build();
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
        var cmd = FFmpegCommandBuilder.New().WithInput("in.wav").WithOutput("out.mp3").ApplyOptions(AudioConversionOptions.ForCompressAudio()).Build();
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
            .ApplyOptions(new AudioConversionOptions { InputFormat = MediaContainer.S16le, InputSampleRate = AudioSampleRate.Hz48000, InputChannels = ChannelLayout.Stereo })
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
        var cmd = FFmpegCommandBuilder.New().WithInput("in.mp3").WithOutput("pipe:1").ApplyOptions(AudioConversionOptions.ForRawPcm()).Build();
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
        var cmd = FFmpegCommandBuilder.New().WithInput("in.mp4").WithOutput("pipe:1").ApplyOptions(VideoConversionOptions.ForStreamingVideo()).Build();
        Assert.Contains("-c", cmd.ArgumentList);
        Assert.Contains("copy", cmd.ArgumentList);
        Assert.Contains("-movflags", cmd.ArgumentList);
        Assert.Contains("+frag_keyframe+empty_moov+default_base_moof", cmd.ArgumentList);
        Assert.Contains("mp4", cmd.ArgumentList);
    }

    [Fact]
    public void Build_ForStreamingVideo_WebM_OmitsMovflags()
    {
        var cmd = FFmpegCommandBuilder.New().WithInput("in.webm").WithOutput("pipe:1").ApplyOptions(VideoConversionOptions.ForStreamingVideo(MediaContainer.WebM)).Build();
        Assert.Contains("-c", cmd.ArgumentList);
        Assert.Contains("copy", cmd.ArgumentList);
        Assert.Contains("webm", cmd.ArgumentList);
        Assert.DoesNotContain("-movflags", cmd.ArgumentList);
    }

    [Fact]
    public void Build_Trim_EmitsSsBeforeInputAndTAfter()
    {
        var cmd = FFmpegCommandBuilder.New().WithInput("in.mp4").WithOutput("out.mp4").WithStartTime(TimeSpan.FromSeconds(1.5)).WithDuration(TimeSpan.FromSeconds(3)).Build();
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
        var cmd = FFmpegCommandBuilder.New().WithInputFormat("lavfi").WithInput("testsrc=duration=1:size=160x120:rate=1").WithOutput("out.mp4").Build();
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

    [Fact]
    public void Build_VideoAudioKnobs_EmitsBitrateRateAndChannels()
    {
        var cmd = Built(
            new() {
                AudioEncoder = AudioEncoder.Aac,
                AudioBitRate = MediaBitRate.K128,
                SampleRate = AudioSampleRate.Hz48000,
                Channels = ChannelLayout.Stereo
            });

        var args = cmd.ArgumentList;
        Assert.Equal("aac", After(args, "-c:a"));
        Assert.Equal("128k", After(args, "-b:a"));
        Assert.Equal("48000", After(args, "-ar"));
        Assert.Equal("2", After(args, "-ac"));
    }

    [Fact]
    public void Build_Av1WithX264Preset_Throws()
    {
        var opts = new VideoConversionOptions { Encoder = VideoEncoder.Av1, Preset = EncoderPreset.Medium };
        Assert.Throws<ArgumentException>(() => Built(opts));
    }

    [Fact]
    public void Build_HostNvenc_ForCompressVideo_EmitsCudaAndNvenc()
    {
        var cmd = FFmpegCommandBuilder.New()
            .WithInput("in.mov")
            .WithOutput("out.mp4")
            .ApplyOptions(VideoConversionOptions.ForCompressVideo())
            .ApplyHost(new() { Hardware = "nvenc" })
            .Build();

        var args = cmd.ArgumentList;
        Assert.Equal("cuda", After(args, "-hwaccel"));
        Assert.Equal("h264_nvenc", After(args, "-c:v"));
        Assert.DoesNotContain("libx264", args);
        Assert.DoesNotContain("medium", args);
    }

    [Fact]
    public void Build_RequestNone_HostNvenc_StaysLibx264()
    {
        var cmd = FFmpegCommandBuilder.New()
            .WithInput("in.mov")
            .WithOutput("out.mp4")
            .ApplyOptions(VideoConversionOptions.ForCompressVideo() with { Hardware = HardwareAccel.None })
            .ApplyHost(new() { Hardware = "nvenc" })
            .Build();

        Assert.Equal("libx264", After(cmd.ArgumentList, "-c:v"));
        Assert.DoesNotContain("-hwaccel", cmd.ArgumentList);
    }

    [Fact]
    public void Build_CopyCodecs_HostNvenc_DoesNotSwapEncoder()
    {
        var cmd = FFmpegCommandBuilder.New()
            .WithInput("in.mp4")
            .WithOutput("out.mp4")
            .ApplyOptions(new VideoConversionOptions { CopyCodecs = true })
            .ApplyHost(new() { Hardware = "nvenc" })
            .Build();

        Assert.Equal("copy", After(cmd.ArgumentList, "-c"));
        Assert.DoesNotContain("h264_nvenc", cmd.ArgumentList);
        Assert.DoesNotContain("-hwaccel", cmd.ArgumentList);
    }

    [Fact]
    public void Build_NvencCrf_EmitsVbrCq()
    {
        var nvenc = Built(new() { Encoder = VideoEncoder.H264, Crf = ConstantRateFactor.Standard, Hardware = HardwareAccel.Nvenc });
        Assert.Equal("vbr", After(nvenc.ArgumentList, "-rc:v"));
        Assert.Equal("23", After(nvenc.ArgumentList, "-cq"));
        Assert.DoesNotContain("-crf", nvenc.ArgumentList);
        var qsv = Built(new() { Encoder = VideoEncoder.H264, Crf = ConstantRateFactor.Standard, Hardware = HardwareAccel.Qsv });
        Assert.Equal("23", After(qsv.ArgumentList, "-global_quality"));
        var vaapi = Built(new() { Encoder = VideoEncoder.H264, Crf = ConstantRateFactor.Standard, Hardware = HardwareAccel.Vaapi });
        Assert.Equal("vaapi", After(vaapi.ArgumentList, "-hwaccel"));
        Assert.Equal(HardwareDevice.RenderD128.Path, After(vaapi.ArgumentList, "-vaapi_device"));
        Assert.Equal("23", After(vaapi.ArgumentList, "-qp"));
        Assert.Contains("format=nv12,hwupload", After(vaapi.ArgumentList, "-vf"), StringComparison.Ordinal);
        var filtered = Built(
            new() {
                Encoder = VideoEncoder.H264,
                Crf = ConstantRateFactor.Standard,
                Hardware = HardwareAccel.Vaapi,
                VideoFilter = "eq=brightness=0.1"
            });

        Assert.DoesNotContain("hwupload", After(filtered.ArgumentList, "-vf"), StringComparison.Ordinal);
        Assert.Throws<ArgumentException>(() => Built(new() { Hardware = HardwareAccel.VideoToolbox, Crf = ConstantRateFactor.Standard }));
    }

    [Fact]
    public void Build_ScaleFit_EmitsForceOriginalAspectRatio()
    {
        var fit = Built(
            new() {
                Width = 1280,
                Height = 720,
                Scale = ScaleMode.Fit,
                VideoFilter = "eq=brightness=0.1",
                Subtitles = SubtitleMode.BurnIn,
                SubtitlePath = "caps.srt"
            });

        Assert.DoesNotContain("-s", fit.ArgumentList);
        var filter = After(fit.ArgumentList, "-vf");
        var scaleAt = filter.IndexOf("scale=1280:720:force_original_aspect_ratio=decrease", StringComparison.Ordinal);
        var eqAt = filter.IndexOf("eq=brightness=0.1", StringComparison.Ordinal);
        var subAt = filter.IndexOf("subtitles=", StringComparison.Ordinal);
        Assert.True(scaleAt >= 0 && scaleAt < eqAt && eqAt < subAt);
        var stretch = Built(new() { Width = 640, Height = 360, Scale = ScaleMode.Stretch });
        Assert.Equal("640x360", After(stretch.ArgumentList, "-s"));
        var widthOnly = Built(new() { Width = 640, Scale = ScaleMode.Fit });
        Assert.Equal("scale=640:-2", After(widthOnly.ArgumentList, "-vf"));
        Assert.Throws<ArgumentException>(() => new VideoConversionOptions { Width = 640, Scale = ScaleMode.Fill }.Validate());
    }

    [Fact]
    public void Build_MetadataAndMaps_RepeatFlags()
    {
        var cmd = Built(
            new() {
                ClearMetadata = true,
                Metadata = new Dictionary<FileTagKey, string> { [FileTagKey.Title] = "Show", [FileTagKey.Artist] = "Band" },
                Maps = [MediaStreamMap.Video(), MediaStreamMap.Audio()]
            });

        var args = cmd.ArgumentList.ToList();
        Assert.Equal(2, args.Count(static arg => arg == "-map"));
        Assert.Equal(2, args.Count(static arg => arg == "-metadata"));
        Assert.True(args.IndexOf("-map_metadata") < args.IndexOf("-metadata"));
        Assert.Contains("0:v:0", args);
        Assert.Contains("0:a:0", args);
        Assert.Contains("title=Show", args);
        Assert.Contains("artist=Band", args);
    }

    [Fact]
    public void Build_SubtitleBurnIn_EscapesPath()
    {
        var cmd = Built(new() { Subtitles = SubtitleMode.BurnIn, SubtitlePath = @"C:\a:b'c,d.srt" });
        Assert.Contains(@"subtitles=C\:\\a\:b\'c\,d.srt", After(cmd.ArgumentList, "-vf"), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("mp4", "mov_text")]
    [InlineData("mov", "mov_text")]
    [InlineData("webm", "webvtt")]
    [InlineData("matroska", "ass")]
    public void Build_SubtitleCopy_UsesContainerCodec(string format, string codec)
    {
        var container = MediaContainer.TryFromFormat(format);
        var cmd = Built(new() { Format = container, Subtitles = SubtitleMode.Copy, SubtitlePath = "caps.srt" });
        var args = cmd.ArgumentList.ToList();
        Assert.Equal(codec, After(args, "-c:s"));
        Assert.Equal(2, args.Count(static arg => arg == "-i"));
        Assert.Contains("0", args);
        Assert.Contains("1", args);
        Assert.Contains("-map", args);
    }

    [Fact]
    public void Build_ForHls_EmitsSegmentFlags()
    {
        var hls = FFmpegCommandBuilder.New().WithInput("in.mp4").WithOutput("/var/media/show.m3u8").ApplyOptions(VideoConversionOptions.ForHls()).Build();
        Assert.Equal("hls", After(hls.ArgumentList, "-f"));
        Assert.Equal("6", After(hls.ArgumentList, "-hls_time"));
        Assert.Equal("vod", After(hls.ArgumentList, "-hls_playlist_type"));
        Assert.Equal(Path.Combine("/var/media", "seg_%03d.ts"), After(hls.ArgumentList, "-hls_segment_filename"));
        var dash = FFmpegCommandBuilder.New().WithInput("in.mp4").WithOutput("/var/media/show.mpd").ApplyOptions(VideoConversionOptions.ForDash()).Build();
        Assert.Equal("dash", After(dash.ArgumentList, "-f"));
        Assert.Equal("4", After(dash.ArgumentList, "-seg_duration"));
        Assert.Equal(Path.Combine("/var/media", "init.m4s"), After(dash.ArgumentList, "-init_seg_name"));
        Assert.Equal(Path.Combine("/var/media", "chunk_%03d.m4s"), After(dash.ArgumentList, "-media_seg_name"));
    }

    private static FFmpegCommand Built(VideoConversionOptions options) => FFmpegCommandBuilder.New().WithInput("in.mov").WithOutput("out.mp4").ApplyOptions(options).Build();

    private static string After(IReadOnlyList<string> args, string flag)
    {
        var list = args as IList<string> ?? args.ToList();
        var index = list.IndexOf(flag);
        Assert.True(index >= 0 && index + 1 < list.Count, flag);
        return list[index + 1];
    }
}