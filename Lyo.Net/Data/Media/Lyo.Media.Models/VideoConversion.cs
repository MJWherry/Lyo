using Lyo.Common.Core.Records;

namespace Lyo.Media.Models;

/// <summary>Entry points for <see cref="VideoConversionBuilder" />.</summary>
public static class VideoConversion
{
    /// <summary>Starts a builder at <paramref name="inputPath" />.</summary>
    public static VideoConversionBuilder From(string inputPath) => VideoConversionBuilder.From(inputPath);

    /// <summary>Transcodes <paramref name="inputPath" /> into <paramref name="outputPath" />.</summary>
    public static VideoConversionBuilder Transcode(
        string inputPath,
        string outputPath,
        MediaContainer format,
        AudioEncoder? audioEncoder = null,
        VideoEncoder? videoEncoder = null)
        => VideoConversionBuilder.Seed(VideoConversionOptions.ForTranscode(format, audioEncoder, videoEncoder), inputPath, outputPath, null, false);

    /// <summary>Compresses <paramref name="inputPath" /> into <paramref name="outputPath" />.</summary>
    public static VideoConversionBuilder Compress(string inputPath, string outputPath, HardwareAccel? hardware = null)
        => VideoConversionBuilder.Seed(VideoConversionOptions.ForCompressVideo(hardware: hardware), inputPath, outputPath, null, false);

    /// <summary>Builds a GIF from <paramref name="inputPath" />.</summary>
    public static VideoConversionBuilder Gif(string inputPath, string outputPath, int? width = 480, VideoFrameRate? frameRate = null)
        => VideoConversionBuilder.Seed(VideoConversionOptions.ForGif(width, frameRate), inputPath, outputPath, null, false);

    /// <summary>Remuxes <paramref name="inputPath" /> to an HLS playlist at <paramref name="outputPath" />.</summary>
    public static VideoConversionBuilder Hls(string inputPath, string outputPath, StreamPackaging? packaging = null)
        => VideoConversionBuilder.Seed(VideoConversionOptions.ForHls(packaging), inputPath, outputPath, null, false);

    /// <summary>Remuxes <paramref name="inputPath" /> to a DASH manifest at <paramref name="outputPath" />.</summary>
    public static VideoConversionBuilder Dash(string inputPath, string outputPath, StreamPackaging? packaging = null)
        => VideoConversionBuilder.Seed(VideoConversionOptions.ForDash(packaging), inputPath, outputPath, null, false);

    /// <summary>Thumbnail knobs for <see cref="IVideoConverter.ExtractFrameAsync" />. Paths are not stored.</summary>
    public static VideoConversionBuilder Thumbnail(int width, int height, ScaleMode scale = ScaleMode.Fit)
        => VideoConversionBuilder.Seed(VideoConversionOptions.ForThumbnail(width, height, scale), null, null, null, false);

    /// <summary>Concatenates <paramref name="inputs" /> into <paramref name="outputPath" />. Call <see cref="VideoConversionBuilder.BuildConcat" />.</summary>
    public static VideoConversionBuilder Concat(IReadOnlyList<string> inputs, string outputPath)
        => VideoConversionBuilder.Seed(new VideoConversionOptions { CopyCodecs = true }, null, outputPath, inputs, true);
}
