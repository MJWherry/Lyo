using System.Text;
using CliWrap;
using Lyo.Common.Core.Identifiers;
using Lyo.Common.Core.Records;
using Lyo.Media.Models;

namespace Lyo.FFmpeg.Tests;

public class FFmpegIntegrationTests
{
    private static readonly Lazy<string> EncoderList = new(LoadEncoderList);

    private static bool HasFfmpeg { get; } = ProbeCli("ffmpeg");

    private static bool HasFfprobe { get; } = ProbeCli("ffprobe");

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Play_NoDisplay_Wav()
    {
        Assert.SkipUnless(ProbeCli("ffplay") && HasFfmpeg, "ffplay/ffmpeg are not on PATH.");
        var dir = CreateWorkDir();
        var src = Path.Combine(dir, "src.wav");
        await GenerateSineWavAsync(src);
        var player = new FFmpegAudioPlayer();
        var result = await player.PlayAsync(src, new() { NoDisplay = true, AutoExit = true }, TestCt);
        Assert.True(result.IsSuccess, string.Join("; ", result.Errors?.Select(e => e.Message) ?? []));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Play_WindowedVideo_SkippedWithoutDisplay()
    {
        Assert.SkipUnless(ProbeCli("ffplay") && HasFfmpeg, "ffplay/ffmpeg are not on PATH.");
        Assert.SkipUnless(!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DISPLAY")), "No DISPLAY; windowed play skipped.");
        var dir = CreateWorkDir();
        var src = Path.Combine(dir, "clip.mp4");
        await GenerateTestsrcMp4Async(src);
        var player = new FFmpegVideoPlayer();
        var result = await player.PlayAsync(src, new() { NoDisplay = false, AutoExit = true, Duration = TimeSpan.FromMilliseconds(200) }, TestCt);
        Assert.True(result.IsSuccess, string.Join("; ", result.Errors?.Select(e => e.Message) ?? []));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ConvertFileToFile_ForAudio_WritesWav()
    {
        Assert.SkipUnless(HasFfmpeg, "ffmpeg is not on PATH.");
        var dir = CreateWorkDir();
        var src = Path.Combine(dir, "src.wav");
        var dst = Path.Combine(dir, "out.wav");
        await GenerateSineWavAsync(src);
        var converter = new FFmpegAudioConverter();
        var result = await converter.ConvertFileToFileAsync(src, dst, AudioConversionOptions.ForAudio(), TestCt);
        Assert.True(result.IsSuccess, string.Join("; ", result.Errors?.Select(e => e.Message) ?? []));
        Assert.True(new FileInfo(dst).Length > 0);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ConvertLavfiTestsrc_ToMp4()
    {
        Assert.SkipUnless(HasFfmpeg, "ffmpeg is not on PATH.");
        var dir = CreateWorkDir();
        var dst = Path.Combine(dir, "out.mp4");
        var converter = new FFmpegVideoConverter();
        var opts = new VideoConversionOptions {
            InputFormat = MediaContainer.Custom("lavfi"),
            Format = MediaContainer.Mp4,
            Encoder = FirstVideoEncoder(),
            DropAudio = true,
            PixelFormat = PixelFormat.Yuv420p
        };

        var result = await converter.ConvertFileToFileAsync("testsrc=duration=1:size=160x120:rate=10", dst, opts, TestCt);
        Assert.True(result.IsSuccess, string.Join("; ", result.Errors?.Select(e => e.Message) ?? []));
        Assert.True(new FileInfo(dst).Length > 0);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ConvertFileToFile_ForCompressVideo_WritesMp4()
    {
        Assert.SkipUnless(HasFfmpeg, "ffmpeg is not on PATH.");
        var dir = CreateWorkDir();
        var src = Path.Combine(dir, "clip.mp4");
        var dst = Path.Combine(dir, "out.mp4");
        await GenerateTestsrcMp4Async(src);
        var converter = new FFmpegVideoConverter();
        var opts = HasEncoder("libx264")
            ? VideoConversionOptions.ForCompressVideo()
            : new() { Encoder = FirstVideoEncoder(), VideoBitRate = MediaBitRate.Custom("200k"), Format = MediaContainer.Mp4 };

        var result = await converter.ConvertFileToFileAsync(src, dst, opts, TestCt);
        Assert.True(result.IsSuccess, string.Join("; ", result.Errors?.Select(e => e.Message) ?? []));
        Assert.True(new FileInfo(dst).Length > 0);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ConvertFileToFile_ForCompressAudio_WritesMp3()
    {
        Assert.SkipUnless(HasFfmpeg, "ffmpeg is not on PATH.");
        var dir = CreateWorkDir();
        var src = Path.Combine(dir, "src.wav");
        await GenerateSineWavAsync(src);
        var converter = new FFmpegAudioConverter();
        var opts = HasEncoder("libmp3lame")
            ? AudioConversionOptions.ForCompressAudio()
            : AudioConversionOptions.ForCompressAudio(MediaBitRate.Custom("64k"), AudioEncoder.Aac, MediaContainer.Mp4);

        var dst = Path.Combine(dir, HasEncoder("libmp3lame") ? "out.mp3" : "out.m4a");
        var result = await converter.ConvertFileToFileAsync(src, dst, opts, TestCt);
        Assert.True(result.IsSuccess, string.Join("; ", result.Errors?.Select(e => e.Message) ?? []));
        Assert.True(new FileInfo(dst).Length > 0);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ExtractFrame_WritesJpeg()
    {
        Assert.SkipUnless(HasFfmpeg, "ffmpeg is not on PATH.");
        var dir = CreateWorkDir();
        var src = Path.Combine(dir, "clip.mp4");
        var dst = Path.Combine(dir, "frame.jpg");
        await GenerateTestsrcMp4Async(src);
        var converter = new FFmpegVideoConverter();
        var result = await converter.ExtractFrameAsync(src, dst, TimeSpan.Zero, ct: TestCt);
        Assert.True(result.IsSuccess, string.Join("; ", result.Errors?.Select(e => e.Message) ?? []));
        Assert.True(new FileInfo(dst).Length > 0);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Probe_Video_FillsWidthAndFps()
    {
        Assert.SkipUnless(HasFfprobe && HasFfmpeg, "ffmpeg/ffprobe are not on PATH.");
        var dir = CreateWorkDir();
        var src = Path.Combine(dir, "clip.mp4");
        await GenerateTestsrcMp4Async(src);
        var prober = new FFmpegVideoProber();
        var result = await prober.ProbeAsync(src, TestCt);
        Assert.True(result.IsSuccess, string.Join("; ", result.Errors?.Select(e => e.Message) ?? []));
        var probe = result.Data!;
        Assert.True(probe.HasVideo);
        Assert.Equal(160, probe.Width);
        Assert.Equal(120, probe.Height);
        Assert.True(probe.Fps is > 0);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task StartConvert_ForRawPcm_FirstByteBeforeCompletion()
    {
        Assert.SkipUnless(HasFfmpeg, "ffmpeg is not on PATH.");
        var converter = new FFmpegAudioConverter();
        var opts = AudioConversionOptions.ForRawPcm() with { InputFormat = MediaContainer.Custom("lavfi") };
        var sessionResult = await converter.StartConvertAsync("sine=frequency=440:duration=8", opts, TestCt);
        Assert.True(sessionResult.IsSuccess, string.Join("; ", sessionResult.Errors?.Select(e => e.Message) ?? []));
        await using var session = sessionResult.Data!;
        var buf = new byte[1];
        var read = await session.StandardOutput.ReadAsync(buf.AsMemory(0, 1), TestCt);
        Assert.True(read > 0);
        Assert.False(session.Completion.IsCompleted);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task StartConvert_Stream_FirstByteBeforeCompletion()
    {
        Assert.SkipUnless(HasFfmpeg, "ffmpeg is not on PATH.");
        var dir = CreateWorkDir();
        var src = Path.Combine(dir, "src.wav");
        await GenerateSineWavAsync(src);
        await using var input = File.OpenRead(src);
        var converter = new FFmpegAudioConverter();
        var opts = AudioConversionOptions.ForRawPcm();
        var sessionResult = await converter.StartConvertAsync(input, opts, TestCt);
        Assert.True(sessionResult.IsSuccess, string.Join("; ", sessionResult.Errors?.Select(e => e.Message) ?? []));
        await using var session = sessionResult.Data!;
        var buf = new byte[1];
        var read = await session.StandardOutput.ReadAsync(buf.AsMemory(0, 1), TestCt);
        Assert.True(read > 0);
        Assert.False(session.Completion.IsCompleted);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task StartConvert_FragmentedMp4_WritesBytes()
    {
        Assert.SkipUnless(HasFfmpeg, "ffmpeg is not on PATH.");
        var dir = CreateWorkDir();
        var src = Path.Combine(dir, "clip.mp4");
        await GenerateTestsrcMp4Async(src);
        var converter = new FFmpegVideoConverter();
        var opts = VideoConversionOptions.ForStreamingVideo();
        var before = CountSpoolFiles();
        var sessionResult = await converter.StartConvertAsync(src, opts, TestCt);
        Assert.True(sessionResult.IsSuccess, string.Join("; ", sessionResult.Errors?.Select(e => e.Message) ?? []));
        await using var session = sessionResult.Data!;
        using var buffer = new MemoryStream();
        await session.StandardOutput.CopyToAsync(buffer, TestCt);
        var completed = await session.Completion;
        Assert.True(completed.IsSuccess, string.Join("; ", completed.Errors?.Select(e => e.Message) ?? []));
        Assert.True(buffer.Length > 0);
        Assert.Equal(before, CountSpoolFiles());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task StartConvert_ForStreamingVideo_WebM_WritesBytes()
    {
        Assert.SkipUnless(HasFfmpeg, "ffmpeg is not on PATH.");
        Assert.SkipUnless(HasEncoder("libvpx-vp9") || HasEncoder("vp9") || HasEncoder("libvpx"), "No VP9 encoder for WebM fixture.");
        var dir = CreateWorkDir();
        var src = Path.Combine(dir, "clip.webm");
        await GenerateTestsrcWebmAsync(src);
        var converter = new FFmpegVideoConverter();
        var opts = VideoConversionOptions.ForStreamingVideo(MediaContainer.WebM);
        var sessionResult = await converter.StartConvertAsync(src, opts, TestCt);
        Assert.True(sessionResult.IsSuccess, string.Join("; ", sessionResult.Errors?.Select(e => e.Message) ?? []));
        await using var session = sessionResult.Data!;
        using var buffer = new MemoryStream();
        await session.StandardOutput.CopyToAsync(buffer, TestCt);
        var completed = await session.Completion;
        Assert.True(completed.IsSuccess, string.Join("; ", completed.Errors?.Select(e => e.Message) ?? []));
        Assert.True(buffer.Length > 0);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ConvertFileToFile_SameInstance_ConcurrentSucceeds()
    {
        Assert.SkipUnless(HasFfmpeg, "ffmpeg is not on PATH.");
        var dir = CreateWorkDir();
        var src = Path.Combine(dir, "src.wav");
        await GenerateSineWavAsync(src);
        var converter = new FFmpegAudioConverter();
        var a = converter.ConvertFileToFileAsync(src, Path.Combine(dir, "a.wav"), AudioConversionOptions.ForAudio(), TestCt);
        var b = converter.ConvertFileToFileAsync(src, Path.Combine(dir, "b.wav"), AudioConversionOptions.ForAudio(), TestCt);
        var results = await Task.WhenAll(a, b);
        Assert.True(results[0].IsSuccess, string.Join("; ", results[0].Errors?.Select(e => e.Message) ?? []));
        Assert.True(results[1].IsSuccess, string.Join("; ", results[1].Errors?.Select(e => e.Message) ?? []));
        Assert.True(new FileInfo(Path.Combine(dir, "a.wav")).Length > 0);
        Assert.True(new FileInfo(Path.Combine(dir, "b.wav")).Length > 0);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Transcode_ToMkv_ProbesVideoCodec()
    {
        Assert.SkipUnless(HasFfmpeg && HasFfprobe, "ffmpeg/ffprobe are not on PATH.");
        var dir = CreateWorkDir();
        var src = Path.Combine(dir, "clip.mp4");
        var dst = Path.Combine(dir, "out.mkv");
        await GenerateTestsrcMp4Async(src);
        var encoder = FirstVideoEncoder();
        var audio = FirstAudioEncoder();
        var converter = new FFmpegVideoConverter();
        var request = VideoConversion.Transcode(src, dst, MediaContainer.Mkv, audio, encoder).Build();
        var result = await converter.ConvertAsync(request, TestCt);
        Assert.True(result.IsSuccess, string.Join("; ", result.Errors?.Select(e => e.Message) ?? []));
        var probe = await new FFmpegVideoProber().ProbeAsync(dst, TestCt);
        Assert.True(probe.IsSuccess, string.Join("; ", probe.Errors?.Select(e => e.Message) ?? []));
        Assert.Equal(encoder.Id, probe.Data!.VideoCodec);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ExtractAsync_WritesAudioFile()
    {
        Assert.SkipUnless(HasFfmpeg, "ffmpeg is not on PATH.");
        var dir = CreateWorkDir();
        var src = Path.Combine(dir, "clip.mp4");
        await GenerateTestsrcMp4Async(src);
        var aac = HasEncoder("aac");
        var dst = Path.Combine(dir, aac ? "out.m4a" : "out.wav");
        var options = aac ? null : AudioConversionOptions.ForExtract(MediaContainer.Wav, FirstAudioEncoder());
        var result = await new FFmpegAudioConverter().ExtractAsync(src, dst, options, TestCt);
        Assert.True(result.IsSuccess, string.Join("; ", result.Errors?.Select(e => e.Message) ?? []));
        Assert.True(new FileInfo(dst).Length > 0);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ConcatAsync_DoublesWavDuration()
    {
        Assert.SkipUnless(HasFfmpeg && HasFfprobe, "ffmpeg/ffprobe are not on PATH.");
        var dir = CreateWorkDir();
        var a = Path.Combine(dir, "a.wav");
        var b = Path.Combine(dir, "b.wav");
        var dst = Path.Combine(dir, "out.wav");
        await GenerateSineWavAsync(a);
        await GenerateSineWavAsync(b);
        var result = await new FFmpegAudioConverter().ConcatAsync([a, b], dst, ct: TestCt);
        Assert.True(result.IsSuccess, string.Join("; ", result.Errors?.Select(e => e.Message) ?? []));
        var probe = await new FFmpegAudioProber().ProbeAsync(dst, TestCt);
        Assert.True(probe.IsSuccess, string.Join("; ", probe.Errors?.Select(e => e.Message) ?? []));
        Assert.True(probe.Data!.DurationSeconds is > 0.35);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GifAndThumbnail_WriteSizedOutputs()
    {
        Assert.SkipUnless(HasFfmpeg && HasFfprobe, "ffmpeg/ffprobe are not on PATH.");
        var dir = CreateWorkDir();
        var src = Path.Combine(dir, "clip.mp4");
        var gif = Path.Combine(dir, "out.gif");
        var thumb = Path.Combine(dir, "thumb.jpg");
        await GenerateTestsrcMp4Async(src);
        var video = new FFmpegVideoConverter();
        var gifResult = await video.ConvertFileToFileAsync(src, gif, VideoConversionOptions.ForGif(160), TestCt);
        Assert.True(gifResult.IsSuccess, string.Join("; ", gifResult.Errors?.Select(e => e.Message) ?? []));
        Assert.True(new FileInfo(gif).Length > 0);
        var thumbResult = await video.ExtractFrameAsync(src, thumb, TimeSpan.Zero, VideoConversionOptions.ForThumbnail(80, 60), TestCt);
        Assert.True(thumbResult.IsSuccess, string.Join("; ", thumbResult.Errors?.Select(e => e.Message) ?? []));
        var probe = await new FFmpegVideoProber().ProbeAsync(thumb, TestCt);
        Assert.True(probe.IsSuccess, string.Join("; ", probe.Errors?.Select(e => e.Message) ?? []));
        Assert.Equal(80, probe.Data!.Width);
        Assert.Equal(60, probe.Data.Height);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ForHls_WritesPlaylistAndSegment()
    {
        Assert.SkipUnless(HasFfmpeg, "ffmpeg is not on PATH.");
        var dir = CreateWorkDir();
        var src = Path.Combine(dir, "clip.mp4");
        var playlist = Path.Combine(dir, "out.m3u8");
        await GenerateTestsrcMp4Async(src);
        var result = await new FFmpegVideoConverter().ConvertFileToFileAsync(src, playlist, VideoConversionOptions.ForHls(), TestCt);
        Assert.True(result.IsSuccess, string.Join("; ", result.Errors?.Select(e => e.Message) ?? []));
        var text = await File.ReadAllTextAsync(playlist, TestCt);
        Assert.Contains("#EXTM3U", text, StringComparison.Ordinal);
        Assert.NotEmpty(Directory.GetFiles(dir, "seg_*.ts"));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Loudness_Streaming_WritesWav()
    {
        Assert.SkipUnless(HasFfmpeg, "ffmpeg is not on PATH.");
        var dir = CreateWorkDir();
        var wav = Path.Combine(dir, "src.wav");
        var generated = await Cli.Wrap("ffmpeg")
            .WithArguments(["-y", "-f", "lavfi", "-i", "sine=frequency=440:duration=3", "-f", "wav", wav])
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync(TestCt);

        Assert.Equal(0, generated.ExitCode);
        var loud = await new FFmpegAudioConverter().ConvertFileToFileAsync(
            wav, Path.Combine(dir, "loud.wav"), AudioConversionOptions.ForAudio() with { Loudness = LoudnessTarget.Streaming }, TestCt);

        Assert.True(loud.IsSuccess, string.Join("; ", loud.Errors?.Select(e => e.Message) ?? []));
        Assert.True(new FileInfo(Path.Combine(dir, "loud.wav")).Length > 0);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task TwoPass_Libx264_WritesMp4()
    {
        Assert.SkipUnless(HasFfmpeg && HasEncoder("libx264"), "libx264 is not available.");
        var dir = CreateWorkDir();
        var src = Path.Combine(dir, "clip.mp4");
        await GenerateTestsrcMp4Async(src);
        var two = await new FFmpegVideoConverter().ConvertFileToFileAsync(
            src, Path.Combine(dir, "two.mp4"), new() {
                Encoder = VideoEncoder.H264,
                VideoBitRate = MediaBitRate.Custom("200k"),
                Format = MediaContainer.Mp4,
                Preset = EncoderPreset.Ultrafast,
                TwoPass = true
            }, TestCt);

        Assert.True(two.IsSuccess, string.Join("; ", two.Errors?.Select(e => e.Message) ?? []));
        Assert.True(new FileInfo(Path.Combine(dir, "two.mp4")).Length > 0);
    }

    private static bool ProbeCli(string name)
    {
        try {
            var result = Cli.Wrap(name).WithArguments("-version").WithValidation(CommandResultValidation.None).ExecuteAsync().GetAwaiter().GetResult();
            return result.ExitCode == 0;
        }
        catch {
            return false;
        }
    }

    private static async Task GenerateSineWavAsync(string path)
    {
        var args = new[] { "-y", "-f", "lavfi", "-i", "sine=frequency=440:duration=0.2", "-f", "wav", path };
        var result = await Cli.Wrap("ffmpeg").WithArguments(args).WithValidation(CommandResultValidation.None).ExecuteAsync();
        Assert.Equal(0, result.ExitCode);
    }

    private static async Task GenerateTestsrcMp4Async(string path)
    {
        var args = new[] {
            "-y",
            "-f",
            "lavfi",
            "-i",
            "testsrc=duration=1:size=160x120:rate=10",
            "-f",
            "lavfi",
            "-i",
            "sine=duration=1",
            "-shortest",
            "-c:v",
            FFmpegCodecMap.ToFfmpegVideo(FirstVideoEncoder().Id),
            "-c:a",
            FFmpegCodecMap.ToFfmpegAudio(FirstAudioEncoder().Id),
            "-pix_fmt",
            "yuv420p",
            path
        };

        var result = await Cli.Wrap("ffmpeg").WithArguments(args).WithValidation(CommandResultValidation.None).ExecuteAsync();
        Assert.Equal(0, result.ExitCode);
    }

    private static async Task GenerateTestsrcWebmAsync(string path)
    {
        var video = HasEncoder("libvpx-vp9") ? "libvpx-vp9" : HasEncoder("libvpx") ? "libvpx" : "libvpx-vp9";
        var audio = HasEncoder("libopus") ? "libopus" : FFmpegCodecMap.ToFfmpegAudio(FirstAudioEncoder().Id);
        var args = new[] {
            "-y",
            "-f",
            "lavfi",
            "-i",
            "testsrc=duration=1:size=160x120:rate=10",
            "-f",
            "lavfi",
            "-i",
            "sine=duration=1",
            "-shortest",
            "-c:v",
            video,
            "-c:a",
            audio,
            "-pix_fmt",
            "yuv420p",
            "-f",
            "webm",
            path
        };

        var result = await Cli.Wrap("ffmpeg").WithArguments(args).WithValidation(CommandResultValidation.None).ExecuteAsync();
        Assert.Equal(0, result.ExitCode);
    }

    private static bool HasEncoder(string name)
    {
        var list = EncoderList.Value;
        return list.Contains($" {name} ", StringComparison.Ordinal) || list.Contains($" {name}\n", StringComparison.Ordinal);
    }

    private static VideoEncoder FirstVideoEncoder()
    {
        foreach (var encoder in new[] { VideoEncoder.H264, VideoEncoder.Mpeg4, VideoEncoder.Custom("mpeg2video"), VideoEncoder.Custom("mjpeg") }) {
            if (HasEncoder(FFmpegCodecMap.ToFfmpegVideo(encoder.Id)))
                return encoder;
        }

        return VideoEncoder.Mpeg4;
    }

    private static AudioEncoder FirstAudioEncoder()
    {
        foreach (var encoder in new[] { AudioEncoder.Aac, AudioEncoder.Custom("mp2"), AudioEncoder.Mp3 }) {
            if (HasEncoder(FFmpegCodecMap.ToFfmpegAudio(encoder.Id)))
                return encoder;
        }

        return AudioEncoder.Aac;
    }

    private static string LoadEncoderList()
    {
        try {
            var stdout = new StringBuilder();
            Cli.Wrap("ffmpeg")
                .WithArguments(["-hide_banner", "-encoders"])
                .WithValidation(CommandResultValidation.None)
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdout))
                .WithStandardErrorPipe(PipeTarget.Null)
                .ExecuteAsync()
                .GetAwaiter()
                .GetResult();

            return stdout.ToString();
        }
        catch {
            return "";
        }
    }

    private static string CreateWorkDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "lyo-ffmpeg-tests-" + LyoGuid.CreateV7().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static int CountSpoolFiles() => Directory.GetFiles(Path.GetTempPath(), "lyo-ffmpeg-*").Length;
}