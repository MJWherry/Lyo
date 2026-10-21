using Lyo.Common.Core.Records;
using Lyo.Exceptions;
using Lyo.FileTags;

namespace Lyo.Media.Models;

/// <summary>Fluent builder for <see cref="VideoConversionOptions" /> and <see cref="VideoConversionRequest" />. Start with <see cref="VideoConversion" />.</summary>
public sealed class VideoConversionBuilder
{
    private readonly List<string> _inputs = [];
    private readonly List<MediaStreamMap> _maps = [];
    private readonly Dictionary<FileTagKey, string> _metadata = [];
    private MediaBitRate? _audioBitRate;
    private AudioEncoder? _audioEncoder;
    private string? _audioFilter;
    private AudioVbrQuality? _audioQuality;
    private ChannelLayout? _channels;
    private bool _clearMetadata;
    private bool _concat;
    private bool _copy;
    private ConstantRateFactor? _crf;
    private bool _dropAudio;
    private TimeSpan? _duration;
    private VideoEncoder? _encoder;
    private MediaContainer? _format;
    private bool _fragmented;
    private VideoFrameRate? _frameRate;
    private HardwareAccel? _hardware;
    private HardwareDevice? _hardwareDevice;
    private int? _height;
    private MediaContainer? _inputFormat;
    private MediaIoMode _ioMode = MediaIoMode.TempFile;
    private LoudnessTarget? _loudness;
    private string? _output;
    private bool _overwrite = true;
    private StreamPackaging? _packaging;
    private PixelFormat? _pixelFormat;
    private EncoderPreset? _preset;
    private AudioSampleRate? _sampleRate;
    private ScaleMode _scale = ScaleMode.Stretch;
    private TimeSpan? _start;
    private SubtitleCodec? _subtitleCodec;
    private string? _subtitlePath;
    private SubtitleMode _subtitles;
    private bool _twoPass;
    private MediaBitRate? _videoBitRate;
    private string? _videoFilter;
    private int? _width;

    private VideoConversionBuilder() { }

    /// <summary>Starts a builder at <paramref name="inputPath" />.</summary>
    public static VideoConversionBuilder From(string inputPath)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(inputPath);
        var builder = new VideoConversionBuilder();
        builder._inputs.Add(inputPath);
        return builder;
    }

    /// <summary>Sets the output file.</summary>
    public VideoConversionBuilder To(string outputPath)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(outputPath);
        _output = outputPath;
        return this;
    }

    /// <summary>Sets the output container.</summary>
    public VideoConversionBuilder WithFormat(MediaContainer format)
    {
        ArgumentHelpers.ThrowIfNull(format);
        _format = format;
        return this;
    }

    /// <summary>Sets the video encoder.</summary>
    public VideoConversionBuilder WithEncoder(VideoEncoder encoder)
    {
        ArgumentHelpers.ThrowIfNull(encoder);
        _encoder = encoder;
        return this;
    }

    /// <summary>Sets the audio encoder.</summary>
    public VideoConversionBuilder WithAudioEncoder(AudioEncoder encoder)
    {
        ArgumentHelpers.ThrowIfNull(encoder);
        _audioEncoder = encoder;
        return this;
    }

    /// <summary>Sets the audio bitrate.</summary>
    public VideoConversionBuilder WithAudioBitRate(MediaBitRate bitRate)
    {
        ArgumentHelpers.ThrowIfNull(bitRate);
        _audioBitRate = bitRate;
        return this;
    }

    /// <summary>Sets the video bitrate.</summary>
    public VideoConversionBuilder WithVideoBitRate(MediaBitRate bitRate)
    {
        ArgumentHelpers.ThrowIfNull(bitRate);
        _videoBitRate = bitRate;
        return this;
    }

    /// <summary>Sets the constant rate factor.</summary>
    public VideoConversionBuilder WithCrf(ConstantRateFactor crf)
    {
        ArgumentHelpers.ThrowIfNull(crf);
        _crf = crf;
        return this;
    }

    /// <summary>Sets the preset.</summary>
    public VideoConversionBuilder WithPreset(EncoderPreset preset)
    {
        ArgumentHelpers.ThrowIfNull(preset);
        _preset = preset;
        return this;
    }

    /// <summary>Sets the output size and scale mode.</summary>
    public VideoConversionBuilder WithSize(int width, int height, ScaleMode scale = ScaleMode.Stretch)
    {
        ArgumentHelpers.ThrowIfLessThan(width, 1);
        ArgumentHelpers.ThrowIfLessThan(height, 1);
        ArgumentHelpers.ThrowIfNotDefined(scale);
        _width = width;
        _height = height;
        _scale = scale;
        return this;
    }

    /// <summary>Sets the frame rate.</summary>
    public VideoConversionBuilder WithFrameRate(VideoFrameRate frameRate)
    {
        ArgumentHelpers.ThrowIfNull(frameRate);
        _frameRate = frameRate;
        return this;
    }

    /// <summary>Sets the sample rate.</summary>
    public VideoConversionBuilder WithSampleRate(AudioSampleRate sampleRate)
    {
        ArgumentHelpers.ThrowIfNull(sampleRate);
        _sampleRate = sampleRate;
        return this;
    }

    /// <summary>Sets the channel layout.</summary>
    public VideoConversionBuilder WithChannels(ChannelLayout channels)
    {
        ArgumentHelpers.ThrowIfNull(channels);
        _channels = channels;
        return this;
    }

    /// <summary>Adds a stream map.</summary>
    public VideoConversionBuilder WithMap(MediaStreamMap map)
    {
        ArgumentHelpers.ThrowIfNull(map);
        _maps.Add(map);
        return this;
    }

    /// <summary>Sets the hardware device for this call.</summary>
    public VideoConversionBuilder WithHardware(HardwareAccel hardware)
    {
        ArgumentHelpers.ThrowIfNull(hardware);
        _hardware = hardware;
        return this;
    }

    /// <summary>Burns or copies <paramref name="path" />.</summary>
    public VideoConversionBuilder WithSubtitles(string path, SubtitleMode mode, SubtitleCodec? codec = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(path);
        ArgumentHelpers.ThrowIfNotDefined(mode);
        _subtitlePath = path;
        _subtitles = mode;
        _subtitleCodec = codec;
        return this;
    }

    /// <summary>Enables bitrate two-pass.</summary>
    public VideoConversionBuilder TwoPass()
    {
        _twoPass = true;
        return this;
    }

    /// <summary>Adds one metadata tag.</summary>
    public VideoConversionBuilder WithMetadata(FileTagKey key, string value)
    {
        ArgumentHelpers.ThrowIfNull(key);
        ArgumentHelpers.ThrowIfNull(value);
        _metadata[key] = value;
        return this;
    }

    /// <summary>Returns the knobs with no paths.</summary>
    public VideoConversionOptions BuildOptions()
    {
        var options = ToOptions();
        options.Validate();
        return options;
    }

    /// <summary>Returns a single-file request. Throws when either path is missing.</summary>
    public VideoConversionRequest Build()
    {
        OperationHelpers.ThrowIf(_concat, "Concat builders use BuildConcat().");
        OperationHelpers.ThrowIf(_inputs.Count != 1, "Build() requires one input. Call From().");
        OperationHelpers.ThrowIfNullOrWhiteSpace(_output, "Build() requires an output. Call To().");
        return new() { InputPath = _inputs[0], OutputPath = _output!, Options = BuildOptions() };
    }

    /// <summary>Returns a concat request.</summary>
    public VideoConcatRequest BuildConcat()
    {
        OperationHelpers.ThrowIf(!_concat, "BuildConcat() requires VideoConversion.Concat().");
        OperationHelpers.ThrowIfNullOrWhiteSpace(_output, "BuildConcat() requires an output.");
        return new() { Inputs = _inputs.ToArray(), OutputPath = _output!, Options = BuildOptions() };
    }

    internal static VideoConversionBuilder Seed(VideoConversionOptions options, string? input, string? output, IReadOnlyList<string>? inputs, bool concat)
    {
        var builder = new VideoConversionBuilder {
            _encoder = options.Encoder,
            _audioEncoder = options.AudioEncoder,
            _format = options.Format,
            _frameRate = options.FrameRate,
            _pixelFormat = options.PixelFormat,
            _crf = options.Crf,
            _videoBitRate = options.VideoBitRate,
            _preset = options.Preset,
            _videoFilter = options.VideoFilter,
            _width = options.Width,
            _height = options.Height,
            _scale = options.Scale,
            _dropAudio = options.DropAudio,
            _copy = options.CopyCodecs,
            _start = options.StartTime,
            _duration = options.Duration,
            _ioMode = options.IoMode,
            _fragmented = options.FragmentedOutput,
            _inputFormat = options.InputFormat,
            _audioBitRate = options.AudioBitRate,
            _audioQuality = options.AudioQuality,
            _sampleRate = options.SampleRate,
            _channels = options.Channels,
            _audioFilter = options.AudioFilter,
            _hardware = options.Hardware,
            _hardwareDevice = options.HardwareDevice,
            _clearMetadata = options.ClearMetadata,
            _subtitlePath = options.SubtitlePath,
            _subtitles = options.Subtitles,
            _subtitleCodec = options.SubtitleCodec,
            _loudness = options.Loudness,
            _twoPass = options.TwoPass,
            _packaging = options.Packaging,
            _overwrite = options.Overwrite,
            _output = output,
            _concat = concat
        };

        if (options.Maps != null)
            builder._maps.AddRange(options.Maps);

        if (inputs != null)
            builder._inputs.AddRange(inputs);
        else if (!string.IsNullOrWhiteSpace(input))
            builder._inputs.Add(input);

        return builder;
    }

    private VideoConversionOptions ToOptions()
        => new() {
            Format = _format,
            Overwrite = _overwrite,
            Encoder = _encoder,
            AudioEncoder = _audioEncoder,
            FrameRate = _frameRate,
            PixelFormat = _pixelFormat,
            Crf = _crf,
            VideoBitRate = _videoBitRate,
            Preset = _preset,
            VideoFilter = _videoFilter,
            Width = _width,
            Height = _height,
            Scale = _scale,
            DropAudio = _dropAudio,
            CopyCodecs = _copy,
            StartTime = _start,
            Duration = _duration,
            IoMode = _ioMode,
            FragmentedOutput = _fragmented,
            InputFormat = _inputFormat,
            AudioBitRate = _audioBitRate,
            AudioQuality = _audioQuality,
            SampleRate = _sampleRate,
            Channels = _channels,
            AudioFilter = _audioFilter,
            Hardware = _hardware,
            HardwareDevice = _hardwareDevice,
            Maps = _maps.Count == 0 ? null : _maps.ToArray(),
            Metadata = _metadata.Count == 0 ? null : new Dictionary<FileTagKey, string>(_metadata),
            ClearMetadata = _clearMetadata,
            SubtitlePath = _subtitlePath,
            Subtitles = _subtitles,
            SubtitleCodec = _subtitleCodec,
            Loudness = _loudness,
            TwoPass = _twoPass,
            Packaging = _packaging
        };
}