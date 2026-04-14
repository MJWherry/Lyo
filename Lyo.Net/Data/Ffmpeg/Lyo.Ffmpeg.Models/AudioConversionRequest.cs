using System.Diagnostics;

namespace Lyo.Ffmpeg.Models;

/// <summary>Request for converting audio from one format to another.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class AudioConversionRequest
{
    /// <summary>Gets or sets the input file path.</summary>
    public string InputPath { get; set; } = null!;

    /// <summary>Gets or sets the output file path.</summary>
    public string OutputPath { get; set; } = null!;

    /// <summary>Gets or sets the audio codec (e.g., "pcm_s16le", "libmp3lame"). Default: pcm_s16le</summary>
    public string? Codec { get; set; }

    /// <summary>Gets or sets the sample rate in Hz. Default: 44100</summary>
    public int? SampleRate { get; set; }

    /// <summary>Gets or sets the number of audio channels. Default: 2</summary>
    public int? Channels { get; set; }

    /// <summary>Gets or sets the output format (e.g., "wav", "mp3"). Default: wav</summary>
    public string? Format { get; set; }

    /// <summary>Gets or sets whether to overwrite output if it exists. Default: true</summary>
    public bool Overwrite { get; set; } = true;

    /// <summary>Gets or sets whether to strip video (audio only). Default: true</summary>
    public bool NoVideo { get; set; } = true;

    public override string ToString() => $"AudioConversion: {InputPath} -> {OutputPath} (Codec={Codec ?? "pcm_s16le"}, Format={Format ?? "wav"})";
}