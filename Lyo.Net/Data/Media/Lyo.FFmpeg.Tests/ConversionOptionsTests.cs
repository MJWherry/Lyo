using Lyo.Common.Core.Records;
using Lyo.FFmpeg.Models;
using Lyo.Media.Models;

namespace Lyo.FFmpeg.Tests;

public class ConversionOptionsTests
{
    public static TheoryData<AudioConversionOptions> InvalidAudioOptions()
    {
        var data = new TheoryData<AudioConversionOptions>();
        data.Add(new AudioConversionOptions { AudioBitRate = MediaBitRate.K128, AudioQuality = AudioVbrQuality.V2 });
        data.Add(new AudioConversionOptions { Loudness = LoudnessTarget.Streaming, AudioFilter = "volume=0.5" });
        return data;
    }

    public static TheoryData<VideoConversionOptions> InvalidVideoOptions()
    {
        var data = new TheoryData<VideoConversionOptions>();
        data.Add(new VideoConversionOptions { DropAudio = true, AudioEncoder = AudioEncoder.Aac });
        data.Add(new VideoConversionOptions { Crf = ConstantRateFactor.Standard, VideoBitRate = MediaBitRate.M2 });
        data.Add(new VideoConversionOptions { Width = 640 });
        data.Add(new VideoConversionOptions { Width = 640, Scale = ScaleMode.Fill });
        data.Add(new VideoConversionOptions { FragmentedOutput = true, Format = MediaContainer.WebM });
        data.Add(new VideoConversionOptions { TwoPass = true });
        data.Add(new VideoConversionOptions { TwoPass = true, VideoBitRate = MediaBitRate.M2, Hardware = HardwareAccel.Nvenc });
        data.Add(new VideoConversionOptions { TwoPass = true, VideoBitRate = MediaBitRate.M2, Encoder = VideoEncoder.Vp9 });
        data.Add(new VideoConversionOptions { Subtitles = SubtitleMode.BurnIn });
        data.Add(new VideoConversionOptions { Loudness = LoudnessTarget.Streaming, AudioFilter = "volume=0.5" });
        data.Add(new VideoConversionOptions { Encoder = VideoEncoder.Av1, Preset = EncoderPreset.Medium });
        return data;
    }

    public static TheoryData<FFmpegOptions> InvalidFFmpegOptions()
    {
        var data = new TheoryData<FFmpegOptions>();
        data.Add(new FFmpegOptions { DefaultSampleRate = 0 });
        data.Add(new FFmpegOptions { ProcessTimeout = TimeSpan.Zero });
        data.Add(new FFmpegOptions { Hardware = "nope" });
        return data;
    }

    [Theory]
    [MemberData(nameof(InvalidAudioOptions))]
    public void AudioValidate_InvalidOptions_Throws(AudioConversionOptions opts) => Assert.ThrowsAny<ArgumentException>(opts.Validate);

    [Theory]
    [MemberData(nameof(InvalidVideoOptions))]
    public void VideoValidate_InvalidOptions_Throws(VideoConversionOptions opts) => Assert.ThrowsAny<ArgumentException>(opts.Validate);

    [Theory]
    [MemberData(nameof(InvalidFFmpegOptions))]
    public void FFmpegOptions_InvalidOptions_Throws(FFmpegOptions opts) => Assert.ThrowsAny<ArgumentException>(opts.Validate);

    [Fact]
    public void ForAudio_SetsPcmAndWav()
    {
        var opts = AudioConversionOptions.ForAudio();
        opts.Validate();
        Assert.Equal(AudioEncoder.PcmS16le, opts.Encoder);
        Assert.Equal(AudioSampleRate.Hz44100, opts.SampleRate);
        Assert.Equal(44100, opts.SampleRate!.Hertz);
        Assert.Equal(MediaContainer.Wav, opts.Format);
    }

    [Fact]
    public void ForCompressVideo_DefaultCrf23()
    {
        var opts = VideoConversionOptions.ForCompressVideo();
        opts.Validate();
        Assert.Equal(ConstantRateFactor.Standard, opts.Crf);
        Assert.Equal(23, opts.Crf!.Value);
        Assert.Equal(EncoderPreset.Medium, opts.Preset);
        Assert.Equal(VideoEncoder.H264, opts.Encoder);
    }

    [Fact]
    public void ConvertFileToFile_WithPipeIoMode_Throws()
    {
        var converter = new FFmpegAudioConverter();
        var opts = new AudioConversionOptions { IoMode = MediaIoMode.Pipe };
        Assert.Throws<InvalidOperationException>(() => converter.ConvertFileToFileAsync("in.wav", "out.wav", opts, TestCt).GetAwaiter().GetResult());
    }
}