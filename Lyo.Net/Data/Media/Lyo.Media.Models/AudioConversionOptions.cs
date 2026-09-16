using System.Diagnostics;
using Lyo.Common.Core.Records;
using Lyo.Exceptions;
using Lyo.FileTags;

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

    /// <summary>Output sample rate.</summary>
    public AudioSampleRate? SampleRate { get; init; }

    /// <summary>Output channel layout.</summary>
    public ChannelLayout? Channels { get; init; }

    /// <summary>Audio bitrate. Mutually exclusive with <see cref="AudioQuality" />.</summary>
    public MediaBitRate? AudioBitRate { get; init; }

    /// <summary>Audio VBR quality. Mutually exclusive with <see cref="AudioBitRate" />.</summary>
    public AudioVbrQuality? AudioQuality { get; init; }

    /// <summary>Audio filtergraph. Mutually exclusive with <see cref="Loudness" />.</summary>
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
    public AudioSampleRate? InputSampleRate { get; init; }

    /// <summary>Input channel count for raw PCM.</summary>
    public ChannelLayout? InputChannels { get; init; }

    /// <summary>Tags written with <c>-metadata</c>. Used with <see cref="ClearMetadata" />.</summary>
    public IReadOnlyDictionary<FileTagKey, string>? Metadata { get; init; }

    /// <summary>If true, existing container tags are dropped before <see cref="Metadata" /> is written.</summary>
    public bool ClearMetadata { get; init; }

    /// <summary>Two-pass loudness target. Mutually exclusive with <see cref="AudioFilter" />.</summary>
    public LoudnessTarget? Loudness { get; init; }

    /// <summary>Decode to PCM/WAV-style audio. Sets sample rate, channels, pcm encoder, and wav container.</summary>
    public static AudioConversionOptions ForAudio(int sampleRate = 44100, int channels = 2, AudioEncoder? encoder = null, MediaContainer? format = null)
        => ForAudio(AudioSampleRate.FromHertz(sampleRate), ChannelLayout.FromCount(channels), encoder, format);

    /// <summary>Decode to PCM/WAV-style audio. Sets sample rate, channels, pcm encoder, and wav container.</summary>
    public static AudioConversionOptions ForAudio(AudioSampleRate sampleRate, ChannelLayout channels, AudioEncoder? encoder = null, MediaContainer? format = null)
    {
        ArgumentHelpers.ThrowIfNull(sampleRate);
        ArgumentHelpers.ThrowIfNull(channels);
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

    /// <summary>Re-encode audio smaller. Default is <see cref="MediaBitRate.K128" /> MP3.</summary>
    public static AudioConversionOptions ForCompressAudio(MediaBitRate? audioBitRate = null, AudioEncoder? encoder = null, MediaContainer? format = null)
        => new() { AudioBitRate = audioBitRate ?? MediaBitRate.K128, Encoder = encoder ?? AudioEncoder.Mp3, Format = format ?? MediaContainer.Mp3 };

    /// <summary>Pull the audio stream out of a file. Default is AAC in M4A.</summary>
    public static AudioConversionOptions ForExtract(MediaContainer? format = null, AudioEncoder? encoder = null)
        => ForTranscode(format ?? MediaContainer.M4a, encoder ?? AudioEncoder.Aac);

    /// <summary>Raw PCM on a pipe (s16le, 48 kHz stereo by default). Shaped for a voice pump without referencing Discord.</summary>
    public static AudioConversionOptions ForRawPcm(int sampleRate = 48000, int channels = 2)
        => ForRawPcm(AudioSampleRate.FromHertz(sampleRate), ChannelLayout.FromCount(channels));

    /// <summary>Raw PCM on a pipe. Shaped for a voice pump without referencing Discord.</summary>
    public static AudioConversionOptions ForRawPcm(AudioSampleRate sampleRate, ChannelLayout channels)
    {
        ArgumentHelpers.ThrowIfNull(sampleRate);
        ArgumentHelpers.ThrowIfNull(channels);
        return new() {
            Encoder = AudioEncoder.PcmS16le,
            SampleRate = sampleRate,
            Channels = channels,
            Format = MediaContainer.S16le,
            IoMode = MediaIoMode.Pipe
        };
    }

    /// <summary>Checks mutually exclusive knobs. File-to-file plus <see cref="MediaIoMode.Pipe" /> is rejected by the converter, not here.</summary>
    public void Validate()
    {
        ArgumentHelpers.ThrowIfNotDefined(IoMode);
        ArgumentHelpers.ThrowIf(AudioBitRate != null && AudioQuality != null, "Set AudioBitRate or AudioQuality, not both.", nameof(AudioBitRate));
        ArgumentHelpers.ThrowIf(Loudness != null && !string.IsNullOrWhiteSpace(AudioFilter), "Set Loudness or AudioFilter, not both.", nameof(Loudness));
    }

    /// <inheritdoc />
    public override string ToString()
        => $"AudioConversionOptions(Format={Format}, Encoder={Encoder}, IoMode={IoMode}, CopyCodecs={CopyCodecs})";
}
