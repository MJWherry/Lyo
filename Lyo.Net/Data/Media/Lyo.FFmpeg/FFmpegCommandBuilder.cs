using System.Diagnostics;
using System.Globalization;
using Lyo.Common.Core.Records;
using Lyo.Exceptions;
using Lyo.FFmpeg.Models;
using Lyo.Media.Models;

namespace Lyo.FFmpeg;

/// <summary>
/// Fluent builder that assembles an ffmpeg command line. Flags are emitted only when set.
/// Do not reuse a builder across threads. Every operation should call <see cref="New" />.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class FFmpegCommandBuilder
{
    private static readonly string[] PreInputOrder = ["-ss", "-f", "-ar", "-ac"];
    private static readonly string[] PostInputOrder = [
        "-t", "-c", "-c:a", "-ar", "-ac", "-b:a", "-q:a", "-c:v", "-crf", "-preset", "-b:v", "-r", "-pix_fmt", "-s", "-vf", "-af", "-movflags"
    ];
    private static readonly string[] PreOutputOrder = ["-f"];

    private readonly List<string> _customArguments = [];
    private readonly Dictionary<string, string> _postInput = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _preInput = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _preOutput = new(StringComparer.Ordinal);
    private FFmpegOptions? _defaults;
    private bool? _dropAudio;
    private bool? _dropVideo;
    private string? _executablePath;
    private string? _inputPath;
    private bool? _overwrite;
    private string? _outputPath;

    /// <summary>Allocates a fresh builder.</summary>
    public static FFmpegCommandBuilder New() => new();

    /// <summary>Sets the input path, URL, lavfi name, or <c>pipe:0</c>.</summary>
    public FFmpegCommandBuilder WithInput(string inputPath)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(inputPath);
        _inputPath = inputPath;
        return this;
    }

    /// <summary>Sets the output path or <c>pipe:1</c>.</summary>
    public FFmpegCommandBuilder WithOutput(string outputPath)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(outputPath);
        _outputPath = outputPath;
        return this;
    }

    /// <summary>Sets <c>-c:a</c>.</summary>
    public FFmpegCommandBuilder WithAudioCodec(string codec)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(codec);
        return Pair(_postInput, "-c:a", codec);
    }

    /// <summary>Sets <c>-c:v</c>.</summary>
    public FFmpegCommandBuilder WithVideoCodec(string codec)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(codec);
        return Pair(_postInput, "-c:v", codec);
    }

    /// <summary>Sets output <c>-ar</c>.</summary>
    public FFmpegCommandBuilder WithSampleRate(int sampleRate)
    {
        ArgumentHelpers.ThrowIfLessThan(sampleRate, 1);
        return Pair(_postInput, "-ar", Number(sampleRate));
    }

    /// <summary>Sets output <c>-ac</c>.</summary>
    public FFmpegCommandBuilder WithChannels(int channels)
    {
        ArgumentHelpers.ThrowIfLessThan(channels, 1);
        return Pair(_postInput, "-ac", Number(channels));
    }

    /// <summary>Sets output <c>-f</c>.</summary>
    public FFmpegCommandBuilder WithFormat(string format)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(format);
        return Pair(_preOutput, "-f", format);
    }

    /// <summary>Sets output <c>-f</c> from a container's <see cref="MediaContainer.Format" />.</summary>
    public FFmpegCommandBuilder WithFormat(MediaContainer format)
    {
        ArgumentHelpers.ThrowIfNull(format);
        return WithFormat(format.Format);
    }

    /// <summary>Sets <c>-c:a</c> from an encoder id, mapped to an ffmpeg encoder name.</summary>
    public FFmpegCommandBuilder WithAudioCodec(AudioEncoder encoder)
    {
        ArgumentHelpers.ThrowIfNull(encoder);
        return WithAudioCodec(FFmpegCodecMap.ToFfmpegAudio(encoder.Id));
    }

    /// <summary>Sets <c>-c:v</c> from an encoder id, mapped to an ffmpeg encoder name.</summary>
    public FFmpegCommandBuilder WithVideoCodec(VideoEncoder encoder)
    {
        ArgumentHelpers.ThrowIfNull(encoder);
        return WithVideoCodec(FFmpegCodecMap.ToFfmpegVideo(encoder.Id));
    }

    /// <summary>Sets demuxer <c>-f</c> before <c>-i</c> from a container's <see cref="MediaContainer.Format" />.</summary>
    public FFmpegCommandBuilder WithInputFormat(MediaContainer format)
    {
        ArgumentHelpers.ThrowIfNull(format);
        return WithInputFormat(format.Format);
    }

    /// <summary>Sets <c>-pix_fmt</c> from a pixel format's <see cref="PixelFormat.Id" />.</summary>
    public FFmpegCommandBuilder WithPixelFormat(PixelFormat pixelFormat)
    {
        ArgumentHelpers.ThrowIfNull(pixelFormat);
        return WithPixelFormat(pixelFormat.Id);
    }

    /// <summary>Sets <c>-preset</c> from a preset's <see cref="EncoderPreset.Id" />.</summary>
    public FFmpegCommandBuilder WithPreset(EncoderPreset preset)
    {
        ArgumentHelpers.ThrowIfNull(preset);
        return WithPreset(preset.Id);
    }

    /// <summary>Sets demuxer <c>-f</c> before <c>-i</c>.</summary>
    public FFmpegCommandBuilder WithInputFormat(string format)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(format);
        return Pair(_preInput, "-f", format);
    }

    /// <summary>Sets input <c>-ar</c> before <c>-i</c>.</summary>
    public FFmpegCommandBuilder WithInputSampleRate(int sampleRate)
    {
        ArgumentHelpers.ThrowIfLessThan(sampleRate, 1);
        return Pair(_preInput, "-ar", Number(sampleRate));
    }

    /// <summary>Sets input <c>-ac</c> before <c>-i</c>.</summary>
    public FFmpegCommandBuilder WithInputChannels(int channels)
    {
        ArgumentHelpers.ThrowIfLessThan(channels, 1);
        return Pair(_preInput, "-ac", Number(channels));
    }

    /// <summary>Sets <c>-b:a</c>.</summary>
    public FFmpegCommandBuilder WithAudioBitRate(string bitRate)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(bitRate);
        return Pair(_postInput, "-b:a", bitRate);
    }

    /// <summary>Sets <c>-q:a</c>.</summary>
    public FFmpegCommandBuilder WithAudioQuality(string quality)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(quality);
        return Pair(_postInput, "-q:a", quality);
    }

    /// <summary>Sets <c>-af</c>.</summary>
    public FFmpegCommandBuilder WithAudioFilter(string filter)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(filter);
        return Pair(_postInput, "-af", filter);
    }

    /// <summary>Sets <c>-b:v</c>.</summary>
    public FFmpegCommandBuilder WithVideoBitRate(string bitRate)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(bitRate);
        return Pair(_postInput, "-b:v", bitRate);
    }

    /// <summary>Sets <c>-crf</c>.</summary>
    public FFmpegCommandBuilder WithCrf(int crf) => Pair(_postInput, "-crf", Number(crf));

    /// <summary>Sets <c>-preset</c>.</summary>
    public FFmpegCommandBuilder WithPreset(string preset)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(preset);
        return Pair(_postInput, "-preset", preset);
    }

    /// <summary>Sets <c>-r</c>.</summary>
    public FFmpegCommandBuilder WithFrameRate(double frameRate)
    {
        ArgumentHelpers.ThrowIfLessThanOrEqual(frameRate, 0, message: "Frame rate must be positive.");
        return Pair(_postInput, "-r", Number(frameRate));
    }

    /// <summary>Sets <c>-pix_fmt</c>.</summary>
    public FFmpegCommandBuilder WithPixelFormat(string pixelFormat)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(pixelFormat);
        return Pair(_postInput, "-pix_fmt", pixelFormat);
    }

    /// <summary>Sets <c>-s WxH</c>.</summary>
    public FFmpegCommandBuilder WithSize(int width, int height)
    {
        ArgumentHelpers.ThrowIfLessThan(width, 1);
        ArgumentHelpers.ThrowIfLessThan(height, 1);
        return Pair(_postInput, "-s", $"{width}x{height}");
    }

    /// <summary>Sets <c>-vf</c>.</summary>
    public FFmpegCommandBuilder WithVideoFilter(string filter)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(filter);
        return Pair(_postInput, "-vf", filter);
    }

    /// <summary>Sets <c>-ss</c> before <c>-i</c>.</summary>
    public FFmpegCommandBuilder WithStartTime(TimeSpan startTime)
    {
        ArgumentHelpers.ThrowIf(startTime < TimeSpan.Zero, "Start time cannot be negative.", nameof(startTime));
        return Pair(_preInput, "-ss", FormatTimestamp(startTime));
    }

    /// <summary>Sets <c>-t</c> after <c>-i</c>.</summary>
    public FFmpegCommandBuilder WithDuration(TimeSpan duration)
    {
        ArgumentHelpers.ThrowIf(duration <= TimeSpan.Zero, "Duration must be greater than zero.", nameof(duration));
        return Pair(_postInput, "-t", FormatTimestamp(duration));
    }

    /// <summary>Emits <c>-c copy</c> unless a side codec overrides it.</summary>
    public FFmpegCommandBuilder CopyCodecs() => Pair(_postInput, "-c", "copy");

    /// <summary>Emits fragmented-mp4 movflags.</summary>
    public FFmpegCommandBuilder FragmentedOutput() => Pair(_postInput, "-movflags", "+frag_keyframe+empty_moov+default_base_moof");

    /// <summary>Emits <c>-y</c>.</summary>
    public FFmpegCommandBuilder Overwrite()
    {
        _overwrite = true;
        return this;
    }

    /// <summary>Emits <c>-n</c>.</summary>
    public FFmpegCommandBuilder NoOverwrite()
    {
        _overwrite = false;
        return this;
    }

    /// <summary>Emits <c>-vn</c>.</summary>
    public FFmpegCommandBuilder DropVideo()
    {
        _dropVideo = true;
        return this;
    }

    /// <summary>Emits <c>-an</c>.</summary>
    public FFmpegCommandBuilder DropAudio()
    {
        _dropAudio = true;
        return this;
    }

    /// <summary>Appends a raw argument.</summary>
    public FFmpegCommandBuilder WithArgument(string argument)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(argument);
        _customArguments.Add(argument);
        return this;
    }

    /// <summary>Appends raw arguments, skipping blanks.</summary>
    public FFmpegCommandBuilder WithArguments(IEnumerable<string> arguments)
    {
        ArgumentHelpers.ThrowIfNull(arguments);
        foreach (var arg in arguments) {
            if (!string.IsNullOrWhiteSpace(arg))
                _customArguments.Add(arg);
        }

        return this;
    }

    /// <summary>Applies executable path and global arguments from host options. Does not apply DefaultCodec.</summary>
    public FFmpegCommandBuilder WithDefaults(FFmpegOptions options)
    {
        ArgumentHelpers.ThrowIfNull(options);
        _defaults = options;
        if (!string.IsNullOrWhiteSpace(options.FFmpegPath))
            _executablePath = options.FFmpegPath;

        return this;
    }

    /// <summary>Sets which ffmpeg binary to run.</summary>
    public FFmpegCommandBuilder WithExecutablePath(string path)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(path);
        _executablePath = path;
        return this;
    }

    /// <summary>Copies knobs from <paramref name="options" />. Does not set input or output paths. Audio converts always emit <c>-vn</c>.</summary>
    public FFmpegCommandBuilder ApplyOptions(AudioConversionOptions options)
    {
        ArgumentHelpers.ThrowIfNull(options);
        return ApplyKnobs(ConvertKnobs.FromAudio(options));
    }

    /// <summary>Copies knobs from <paramref name="options" />. Does not set input or output paths.</summary>
    public FFmpegCommandBuilder ApplyOptions(VideoConversionOptions options)
    {
        ArgumentHelpers.ThrowIfNull(options);
        return ApplyKnobs(ConvertKnobs.FromVideo(options));
    }

    /// <summary>Copies knobs from the internal convert spec. Maps catalog Format / Id onto ffmpeg argv.</summary>
    internal FFmpegCommandBuilder ApplyKnobs(ConvertKnobs options)
    {
        ArgumentHelpers.ThrowIfNull(options);
        Apply(options.Format, WithFormat);
        Apply(options.AudioEncoder, WithAudioCodec);
        Apply(options.SampleRate, WithSampleRate);
        Apply(options.Channels, WithChannels);
        Apply(options.AudioBitRate, WithAudioBitRate);
        Apply(options.AudioQuality, WithAudioQuality);
        Apply(options.AudioFilter, WithAudioFilter);
        Apply(options.VideoEncoder, WithVideoCodec);
        Apply(options.FrameRate, WithFrameRate);
        Apply(options.PixelFormat, WithPixelFormat);
        Apply(options.Crf, WithCrf);
        Apply(options.VideoBitRate, WithVideoBitRate);
        Apply(options.Preset, WithPreset);
        Apply(options.VideoFilter, WithVideoFilter);
        Apply(options.DropVideo, DropVideo);
        Apply(options.DropAudio, DropAudio);
        Apply(options.CopyCodecs, CopyCodecs);
        Apply(options.FragmentedOutput, FragmentedOutput);
        Apply(options.StartTime, WithStartTime);
        Apply(options.Duration, WithDuration);
        Apply(options.InputFormat, WithInputFormat);
        Apply(options.InputSampleRate, WithInputSampleRate);
        Apply(options.InputChannels, WithInputChannels);
        if (options.Width is { } w && options.Height is { } h)
            WithSize(w, h);

        return options.Overwrite ? Overwrite() : NoOverwrite();
    }

    /// <summary>Builds the command. Input and output are required. Output may be <c>pipe:1</c>.</summary>
    public FFmpegCommand Build()
    {
        OperationHelpers.ThrowIfNullOrWhiteSpace(_inputPath, "Input path is required. Call WithInput().");
        OperationHelpers.ThrowIfNullOrWhiteSpace(_outputPath, "Output path is required. Call WithOutput().");
        var opts = _defaults;
        var overwrite = _overwrite ?? opts?.DefaultOverwrite ?? true;
        var executablePath = _executablePath ?? opts?.FFmpegPath ?? "ffmpeg";
        var argList = new List<string>();
        if (opts?.GlobalArguments != null)
            argList.AddRange(opts.GlobalArguments);

        argList.Add(overwrite ? "-y" : "-n");
        Emit(argList, _preInput, PreInputOrder);
        argList.Add("-i");
        argList.Add(_inputPath);
        if (_dropVideo == true)
            argList.Add("-vn");
        if (_dropAudio == true)
            argList.Add("-an");

        Emit(argList, _postInput, PostInputOrder);
        argList.AddRange(_customArguments);
        Emit(argList, _preOutput, PreOutputOrder);
        argList.Add(_outputPath);
        return new(executablePath, argList);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        var parts = new List<string>();
        if (_inputPath != null)
            parts.Add($"Input: {_inputPath}");
        if (_outputPath != null)
            parts.Add($"Output: {_outputPath}");
        if (_preOutput.TryGetValue("-f", out var format))
            parts.Add($"Format: {format}");
        if (_postInput.TryGetValue("-c:a", out var audioCodec))
            parts.Add($"AudioCodec: {audioCodec}");
        if (_postInput.TryGetValue("-c:v", out var videoCodec))
            parts.Add($"VideoCodec: {videoCodec}");

        return $"FFmpegCommandBuilder: {string.Join(", ", parts)}";
    }

    internal static string FormatTimestamp(TimeSpan value) => value.TotalSeconds.ToString("G", CultureInfo.InvariantCulture);

    private FFmpegCommandBuilder Apply<T>(T? value, Func<T, FFmpegCommandBuilder> setter)
        where T : class
        => value is null ? this : setter(value);

    private FFmpegCommandBuilder Apply<T>(T? value, Func<T, FFmpegCommandBuilder> setter)
        where T : struct
        => value is { } v ? setter(v) : this;

    private FFmpegCommandBuilder Apply(bool on, Func<FFmpegCommandBuilder> setter) => on ? setter() : this;

    private FFmpegCommandBuilder Pair(Dictionary<string, string> bag, string flag, string value)
    {
        bag[flag] = value;
        return this;
    }

    private static void Emit(List<string> args, Dictionary<string, string> bag, string[] order)
    {
        foreach (var flag in order) {
            if (bag.TryGetValue(flag, out var value))
                AddPair(args, flag, value);
        }
    }

    private static void AddPair(List<string> args, string flag, string value)
    {
        args.Add(flag);
        args.Add(value);
    }

    private static string Number<T>(T value)
        where T : IFormattable
        => value.ToString(null, CultureInfo.InvariantCulture);
}
