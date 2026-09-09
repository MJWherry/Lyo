using System.Diagnostics;
using Lyo.Common.Core.Records;
using Lyo.Exceptions;

namespace Lyo.Media.Models;

/// <summary>
/// Convert knobs for <see cref="IAudioConverter" />.
/// Audio converts always drop video. A bare instance does not force PCM.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record AudioConversionOptions
{
    /// <summary>Output container.</summary>
    public MediaContainer? Format { get; init; }

    /// <summary>If true, an existing output is overwritten. If false, the convert fails when the output exists. Starts as true.</summary>
    public bool Overwrite { get; init; } = true;

    /// <summary>Audio codec.</summary>
    public AudioEncoder? Encoder { get; init; }

    /// <summary>Output sample rate in hertz.</summary>
    public int? SampleRate { get; init; }

    /// <summary>Output channel count.</summary>
    public int? Channels { get; init; }

    /// <summary>Audio bitrate, for example <c>128k</c>. Mutually exclusive with <see cref="AudioQuality" />.</summary>
    public string? AudioBitRate { get; init; }

    /// <summary>Audio VBR quality. Mutually exclusive with <see cref="AudioBitRate" />.</summary>
    public string? AudioQuality { get; init; }

    /// <summary>Audio filtergraph.</summary>
    public string? AudioFilter { get; init; }

    /// <summary>If true, remux without re-encoding unless <see cref="Encoder" /> overrides the audio side.</summary>
    public bool CopyCodecs { get; init; }

    /// <summary>Input seek. Null means start at the beginning.</summary>
    public TimeSpan? StartTime { get; init; }

    /// <summary>Output duration. Null means encode to the end.</summary>
    public TimeSpan? Duration { get; init; }

    /// <summary>Known duration used only to compute <see cref="MediaProgress.Percentage" />. Does not probe the source.</summary>
    public TimeSpan? KnownDuration { get; init; }

    /// <summary>Progress reporter. Callbacks run on the process I/O thread. Null means the backend does not report progress.</summary>
    public IProgress<MediaProgress>? Progress { get; init; }

    /// <summary>How stream overloads feed the converter. Starts as <see cref="MediaIoMode.TempFile" />.</summary>
    public MediaIoMode IoMode { get; init; } = MediaIoMode.TempFile;

    /// <summary>Input container, required for raw PCM stdin.</summary>
    public MediaContainer? InputFormat { get; init; }

    /// <summary>Input sample rate for raw PCM.</summary>
    public int? InputSampleRate { get; init; }

    /// <summary>Input channel count for raw PCM.</summary>
    public int? InputChannels { get; init; }

    /// <summary>Decode to PCM/WAV-style audio. Sets sample rate, channels, pcm encoder, and wav container.</summary>
    public static AudioConversionOptions ForAudio(int sampleRate = 44100, int channels = 2, AudioEncoder? encoder = null, MediaContainer? format = null)
    {
        ArgumentHelpers.ThrowIfLessThan(sampleRate, 1);
        ArgumentHelpers.ThrowIfLessThan(channels, 1);
        return new() {
            Encoder = encoder ?? AudioEncoder.PcmS16le,
            SampleRate = sampleRate,
            Channels = channels,
            Format = format ?? MediaContainer.Wav
        };
    }

    /// <summary>Change container and optional encoder with no quality knobs.</summary>
    public static AudioConversionOptions ForTranscode(MediaContainer format, AudioEncoder? encoder = null)
    {
        ArgumentHelpers.ThrowIfNull(format);
        return new() { Format = format, Encoder = encoder };
    }

    /// <summary>Re-encode audio smaller. Default is 128k MP3.</summary>
    public static AudioConversionOptions ForCompressAudio(string audioBitRate = "128k", AudioEncoder? encoder = null, MediaContainer? format = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(audioBitRate);
        return new() { AudioBitRate = audioBitRate, Encoder = encoder ?? AudioEncoder.Mp3, Format = format ?? MediaContainer.Mp3 };
    }

    /// <summary>Raw PCM on a pipe (s16le, 48 kHz stereo by default). Shaped for a voice pump without referencing Discord.</summary>
    public static AudioConversionOptions ForRawPcm(int sampleRate = 48000, int channels = 2)
    {
        ArgumentHelpers.ThrowIfLessThan(sampleRate, 1);
        ArgumentHelpers.ThrowIfLessThan(channels, 1);
        return new() {
            Encoder = AudioEncoder.PcmS16le,
            SampleRate = sampleRate,
            Channels = channels,
            Format = MediaContainer.S16le,
            IoMode = MediaIoMode.Pipe
        };
    }

    /// <summary>Checks mutually exclusive knobs and positive rates. File-to-file plus <see cref="MediaIoMode.Pipe" /> is rejected by the converter, not here.</summary>
    public void Validate()
    {
        ArgumentHelpers.ThrowIfNotDefined(IoMode);
        if (SampleRate is { } sampleRate)
            ArgumentHelpers.ThrowIfLessThan(sampleRate, 1);
        if (Channels is { } channels)
            ArgumentHelpers.ThrowIfLessThan(channels, 1);
        if (InputSampleRate is { } inputSampleRate)
            ArgumentHelpers.ThrowIfLessThan(inputSampleRate, 1);
        if (InputChannels is { } inputChannels)
            ArgumentHelpers.ThrowIfLessThan(inputChannels, 1);
        ArgumentHelpers.ThrowIf(
            !string.IsNullOrWhiteSpace(AudioBitRate) && !string.IsNullOrWhiteSpace(AudioQuality),
            "Set AudioBitRate or AudioQuality, not both.",
            nameof(AudioBitRate));
    }

    /// <inheritdoc />
    public override string ToString()
        => $"AudioConversionOptions(Format={Format}, Encoder={Encoder}, IoMode={IoMode}, CopyCodecs={CopyCodecs})";
}
