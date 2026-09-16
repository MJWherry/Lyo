using Lyo.Common.Core.Records;

namespace Lyo.Media.Models;

/// <summary>Entry points for <see cref="AudioConversionBuilder" />.</summary>
public static class AudioConversion
{
    /// <summary>Starts a builder at <paramref name="inputPath" />.</summary>
    public static AudioConversionBuilder From(string inputPath) => AudioConversionBuilder.From(inputPath);

    /// <summary>Transcodes <paramref name="inputPath" /> into <paramref name="outputPath" />.</summary>
    public static AudioConversionBuilder Transcode(string inputPath, string outputPath, MediaContainer format, AudioEncoder? encoder = null)
        => AudioConversionBuilder.Seed(AudioConversionOptions.ForTranscode(format, encoder), inputPath, outputPath, null, false);

    /// <summary>Compresses <paramref name="inputPath" /> into <paramref name="outputPath" />.</summary>
    public static AudioConversionBuilder Compress(string inputPath, string outputPath, MediaBitRate? bitRate = null)
        => AudioConversionBuilder.Seed(AudioConversionOptions.ForCompressAudio(bitRate), inputPath, outputPath, null, false);

    /// <summary>Extracts audio from <paramref name="inputPath" /> into <paramref name="outputPath" />.</summary>
    public static AudioConversionBuilder Extract(string inputPath, string outputPath, MediaContainer? format = null, AudioEncoder? encoder = null)
        => AudioConversionBuilder.Seed(AudioConversionOptions.ForExtract(format, encoder), inputPath, outputPath, null, false);

    /// <summary>Concatenates <paramref name="inputs" /> into <paramref name="outputPath" />. Call <see cref="AudioConversionBuilder.BuildConcat" />.</summary>
    public static AudioConversionBuilder Concat(IReadOnlyList<string> inputs, string outputPath)
        => AudioConversionBuilder.Seed(new AudioConversionOptions { CopyCodecs = true }, null, outputPath, inputs, true);
}
