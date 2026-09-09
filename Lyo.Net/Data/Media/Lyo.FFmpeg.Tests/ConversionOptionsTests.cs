using Lyo.Common.Core.Records;
using Lyo.FFmpeg;
using Lyo.FFmpeg.Models;
using Lyo.Media.Models;

namespace Lyo.FFmpeg.Tests;

public class ConversionOptionsTests
{
    public static TheoryData<AudioConversionOptions> InvalidAudioOptions()
    {
        var data = new TheoryData<AudioConversionOptions>();
        data.Add(new AudioConversionOptions { SampleRate = 0 });
        data.Add(new AudioConversionOptions { Channels = 0 });
        data.Add(new AudioConversionOptions { AudioBitRate = "128k", AudioQuality = "2" });
        return data;
    }

    public static TheoryData<VideoConversionOptions> InvalidVideoOptions()
    {
        var data = new TheoryData<VideoConversionOptions>();
        data.Add(new VideoConversionOptions { DropAudio = true, AudioEncoder = AudioEncoder.Aac });
        data.Add(new VideoConversionOptions { Crf = 23, VideoBitRate = "2M" });
        data.Add(new VideoConversionOptions { Width = 640 });
        data.Add(new VideoConversionOptions { FrameRate = 0 });
        data.Add(new VideoConversionOptions { FragmentedOutput = true, Format = MediaContainer.WebM });
        return data;
    }

    public static TheoryData<FFmpegOptions> InvalidFFmpegOptions()
    {
        var data = new TheoryData<FFmpegOptions>();
        data.Add(new FFmpegOptions { DefaultSampleRate = 0 });
        data.Add(new FFmpegOptions { ProcessTimeout = TimeSpan.Zero });
        return data;
    }

    [Theory]
    [MemberData(nameof(InvalidAudioOptions))]
    public void AudioValidate_InvalidOptions_Throws(AudioConversionOptions opts)
        => Assert.ThrowsAny<ArgumentException>(opts.Validate);

    [Theory]
    [MemberData(nameof(InvalidVideoOptions))]
    public void VideoValidate_InvalidOptions_Throws(VideoConversionOptions opts)
        => Assert.ThrowsAny<ArgumentException>(opts.Validate);

    [Theory]
    [MemberData(nameof(InvalidFFmpegOptions))]
    public void FFmpegOptions_InvalidOptions_Throws(FFmpegOptions opts)
        => Assert.ThrowsAny<ArgumentException>(opts.Validate);

    [Fact]
    public void ForAudio_SetsPcmAndWav()
    {
        var opts = AudioConversionOptions.ForAudio();
        opts.Validate();
        Assert.Equal(AudioEncoder.PcmS16le, opts.Encoder);
        Assert.Equal(44100, opts.SampleRate);
        Assert.Equal(MediaContainer.Wav, opts.Format);
    }

    [Fact]
    public void ForCompressVideo_DefaultCrf23()
    {
        var opts = VideoConversionOptions.ForCompressVideo();
        opts.Validate();
        Assert.Equal(23, opts.Crf);
        Assert.Equal(EncoderPreset.Medium, opts.Preset);
        Assert.Equal(VideoEncoder.H264, opts.Encoder);
    }

    [Fact]
    public void ConvertFileToFile_WithPipeIoMode_Throws()
    {
        var converter = new FFmpegAudioConverter();
        var opts = new AudioConversionOptions { IoMode = MediaIoMode.Pipe };
        Assert.Throws<InvalidOperationException>(() => converter.ConvertFileToFileAsync("in.wav", "out.wav", opts, TestContext.Current.CancellationToken).GetAwaiter().GetResult());
    }
}
