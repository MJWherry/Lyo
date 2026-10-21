using Lyo.Common.Core.Records;
using Lyo.Media.Models;

namespace Lyo.FFmpeg.Tests;

public class VideoConversionTests
{
    [Fact]
    public void Build_Transcode_SetsFormatEncoderAndPaths()
    {
        var request = VideoConversion.Transcode("in.mov", "out.webm", MediaContainer.WebM, AudioEncoder.Opus, VideoEncoder.Vp9).WithAudioBitRate(MediaBitRate.K128).Build();
        Assert.Equal("in.mov", request.InputPath);
        Assert.Equal("out.webm", request.OutputPath);
        Assert.Equal(MediaContainer.WebM, request.Options.Format);
        Assert.Equal(AudioEncoder.Opus, request.Options.AudioEncoder);
        Assert.Equal(VideoEncoder.Vp9, request.Options.Encoder);
        Assert.Equal(MediaBitRate.K128, request.Options.AudioBitRate);
        var options = VideoConversion.Transcode("in.mov", "out.webm", MediaContainer.WebM, AudioEncoder.Opus, VideoEncoder.Vp9).WithAudioBitRate(MediaBitRate.K128).BuildOptions();
        Assert.Equal(request.Options.Format, options.Format);
        Assert.Equal(request.Options.AudioEncoder, options.AudioEncoder);
        Assert.Equal(request.Options.Encoder, options.Encoder);
        Assert.Equal(request.Options.AudioBitRate, options.AudioBitRate);
    }
}