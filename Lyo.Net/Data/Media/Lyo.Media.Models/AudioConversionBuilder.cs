using Lyo.Common.Core.Records;
using Lyo.Exceptions;
using Lyo.FileTags;

namespace Lyo.Media.Models;

/// <summary>Fluent builder for <see cref="AudioConversionOptions" /> and <see cref="AudioConversionRequest" />. Start with <see cref="AudioConversion" />.</summary>
public sealed class AudioConversionBuilder
{
    private readonly List<string> _inputs = [];
    private readonly Dictionary<FileTagKey, string> _metadata = [];
    private MediaBitRate? _bitRate;
    private ChannelLayout? _channels;
    private bool _clearMetadata;
    private bool _concat;
    private bool _copy;
    private TimeSpan? _duration;
    private AudioEncoder? _encoder;
    private string? _filter;
    private MediaContainer? _format;
    private ChannelLayout? _inputChannels;
    private MediaContainer? _inputFormat;
    private AudioSampleRate? _inputSampleRate;
    private MediaIoMode _ioMode = MediaIoMode.TempFile;
    private LoudnessTarget? _loudness;
    private string? _output;
    private bool _overwrite = true;
    private AudioVbrQuality? _quality;
    private AudioSampleRate? _sampleRate;
    private TimeSpan? _start;

    private AudioConversionBuilder() { }

    /// <summary>Starts a builder at <paramref name="inputPath" />.</summary>
    public static AudioConversionBuilder From(string inputPath)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(inputPath);
        var builder = new AudioConversionBuilder();
        builder._inputs.Add(inputPath);
        return builder;
    }

    /// <summary>Sets the output file.</summary>
    public AudioConversionBuilder To(string outputPath)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(outputPath);
        _output = outputPath;
        return this;
    }

    /// <summary>Sets the output container.</summary>
    public AudioConversionBuilder WithFormat(MediaContainer format)
    {
        ArgumentHelpers.ThrowIfNull(format);
        _format = format;
        return this;
    }

    /// <summary>Sets the audio encoder.</summary>
    public AudioConversionBuilder WithEncoder(AudioEncoder encoder)
    {
        ArgumentHelpers.ThrowIfNull(encoder);
        _encoder = encoder;
        return this;
    }

    /// <summary>Sets the audio bitrate.</summary>
    public AudioConversionBuilder WithBitRate(MediaBitRate bitRate)
    {
        ArgumentHelpers.ThrowIfNull(bitRate);
        _bitRate = bitRate;
        return this;
    }

    /// <summary>Sets the VBR quality.</summary>
    public AudioConversionBuilder WithQuality(AudioVbrQuality quality)
    {
        ArgumentHelpers.ThrowIfNull(quality);
        _quality = quality;
        return this;
    }

    /// <summary>Sets the sample rate.</summary>
    public AudioConversionBuilder WithSampleRate(AudioSampleRate sampleRate)
    {
        ArgumentHelpers.ThrowIfNull(sampleRate);
        _sampleRate = sampleRate;
        return this;
    }

    /// <summary>Sets the channel layout.</summary>
    public AudioConversionBuilder WithChannels(ChannelLayout channels)
    {
        ArgumentHelpers.ThrowIfNull(channels);
        _channels = channels;
        return this;
    }

    /// <summary>Sets the audio filtergraph.</summary>
    public AudioConversionBuilder WithFilter(string filter)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(filter);
        _filter = filter;
        return this;
    }

    /// <summary>Copies the audio stream.</summary>
    public AudioConversionBuilder CopyCodecs()
    {
        _copy = true;
        return this;
    }

    /// <summary>Sets seek and duration.</summary>
    public AudioConversionBuilder Trim(TimeSpan? start, TimeSpan? duration)
    {
        _start = start;
        _duration = duration;
        return this;
    }

    /// <summary>Adds one metadata tag.</summary>
    public AudioConversionBuilder WithMetadata(FileTagKey key, string value)
    {
        ArgumentHelpers.ThrowIfNull(key);
        ArgumentHelpers.ThrowIfNull(value);
        _metadata[key] = value;
        return this;
    }

    /// <summary>Drops existing container tags.</summary>
    public AudioConversionBuilder ClearMetadata()
    {
        _clearMetadata = true;
        return this;
    }

    /// <summary>Sets a loudness target. Null uses <see cref="LoudnessTarget.Streaming" />.</summary>
    public AudioConversionBuilder WithLoudness(LoudnessTarget? target = null)
    {
        _loudness = target ?? LoudnessTarget.Streaming;
        return this;
    }

    /// <summary>Uses pipe I/O.</summary>
    public AudioConversionBuilder Pipe()
    {
        _ioMode = MediaIoMode.Pipe;
        return this;
    }

    /// <summary>Sets the input container.</summary>
    public AudioConversionBuilder WithInputFormat(MediaContainer format)
    {
        ArgumentHelpers.ThrowIfNull(format);
        _inputFormat = format;
        return this;
    }

    /// <summary>Sets the input sample rate.</summary>
    public AudioConversionBuilder WithInputSampleRate(AudioSampleRate sampleRate)
    {
        ArgumentHelpers.ThrowIfNull(sampleRate);
        _inputSampleRate = sampleRate;
        return this;
    }

    /// <summary>Sets the input channel layout.</summary>
    public AudioConversionBuilder WithInputChannels(ChannelLayout channels)
    {
        ArgumentHelpers.ThrowIfNull(channels);
        _inputChannels = channels;
        return this;
    }

    /// <summary>Fails when the output file already exists.</summary>
    public AudioConversionBuilder NoOverwrite()
    {
        _overwrite = false;
        return this;
    }

    /// <summary>Returns the knobs with no paths. Use this for <c>StartConvertAsync</c> and stream overloads.</summary>
    public AudioConversionOptions BuildOptions()
    {
        var options = ToOptions();
        options.Validate();
        return options;
    }

    /// <summary>Returns a single-file request. Throws when either path is missing.</summary>
    public AudioConversionRequest Build()
    {
        OperationHelpers.ThrowIf(_concat, "Concat builders use BuildConcat().");
        OperationHelpers.ThrowIf(_inputs.Count != 1, "Build() requires one input. Call From().");
        OperationHelpers.ThrowIfNullOrWhiteSpace(_output, "Build() requires an output. Call To().");
        return new() { InputPath = _inputs[0], OutputPath = _output!, Options = BuildOptions() };
    }

    /// <summary>Returns a concat request.</summary>
    public AudioConcatRequest BuildConcat()
    {
        OperationHelpers.ThrowIf(!_concat, "BuildConcat() requires AudioConversion.Concat().");
        OperationHelpers.ThrowIfNullOrWhiteSpace(_output, "BuildConcat() requires an output.");
        return new() { Inputs = _inputs.ToArray(), OutputPath = _output!, Options = BuildOptions() };
    }

    internal static AudioConversionBuilder Seed(AudioConversionOptions options, string? input, string? output, IReadOnlyList<string>? inputs, bool concat)
    {
        var builder = new AudioConversionBuilder {
            _encoder = options.Encoder,
            _format = options.Format,
            _sampleRate = options.SampleRate,
            _channels = options.Channels,
            _bitRate = options.AudioBitRate,
            _quality = options.AudioQuality,
            _filter = options.AudioFilter,
            _copy = options.CopyCodecs,
            _start = options.StartTime,
            _duration = options.Duration,
            _ioMode = options.IoMode,
            _inputFormat = options.InputFormat,
            _inputSampleRate = options.InputSampleRate,
            _inputChannels = options.InputChannels,
            _clearMetadata = options.ClearMetadata,
            _loudness = options.Loudness,
            _overwrite = options.Overwrite,
            _output = output,
            _concat = concat
        };

        if (inputs != null)
            builder._inputs.AddRange(inputs);
        else if (!string.IsNullOrWhiteSpace(input))
            builder._inputs.Add(input);

        return builder;
    }

    private AudioConversionOptions ToOptions()
        => new() {
            Format = _format,
            Overwrite = _overwrite,
            Encoder = _encoder,
            SampleRate = _sampleRate,
            Channels = _channels,
            AudioBitRate = _bitRate,
            AudioQuality = _quality,
            AudioFilter = _filter,
            CopyCodecs = _copy,
            StartTime = _start,
            Duration = _duration,
            IoMode = _ioMode,
            InputFormat = _inputFormat,
            InputSampleRate = _inputSampleRate,
            InputChannels = _inputChannels,
            Metadata = _metadata.Count == 0 ? null : new Dictionary<FileTagKey, string>(_metadata),
            ClearMetadata = _clearMetadata,
            Loudness = _loudness
        };
}