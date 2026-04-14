using System.Diagnostics;
using Lyo.Exceptions;
using Lyo.Ffmpeg.Models;

namespace Lyo.Ffmpeg;

/// <summary>Builder for constructing FFmpeg command lines with a fluent API.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class FFmpegCommandBuilder
{
    private readonly List<string> _customArguments = [];
    private int? _channels;
    private string? _codec;
    private FfmpegOptions? _defaults;
    private string? _executablePath;
    private string? _format;
    private string? _inputPath;
    private bool? _noVideo;
    private string? _outputPath;
    private bool? _overwrite;
    private int? _sampleRate;

    /// <inheritdoc />
    public FFmpegCommandBuilder WithInput(string inputPath)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(inputPath, nameof(inputPath));
        _inputPath = inputPath;
        return this;
    }

    /// <inheritdoc />
    public FFmpegCommandBuilder WithOutput(string outputPath)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(outputPath, nameof(outputPath));
        _outputPath = outputPath;
        return this;
    }

    /// <inheritdoc />
    public FFmpegCommandBuilder WithCodec(string codec)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(codec, nameof(codec));
        _codec = codec;
        return this;
    }

    /// <inheritdoc />
    public FFmpegCommandBuilder WithSampleRate(int sampleRate)
    {
        ArgumentHelpers.ThrowIfNotInRange(sampleRate, 1, int.MaxValue, nameof(sampleRate));
        _sampleRate = sampleRate;
        return this;
    }

    /// <inheritdoc />
    public FFmpegCommandBuilder WithChannels(int channels)
    {
        ArgumentHelpers.ThrowIfNotInRange(channels, 1, int.MaxValue, nameof(channels));
        _channels = channels;
        return this;
    }

    /// <inheritdoc />
    public FFmpegCommandBuilder WithFormat(string format)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(format, nameof(format));
        _format = format;
        return this;
    }

    /// <inheritdoc />
    public FFmpegCommandBuilder Overwrite()
    {
        _overwrite = true;
        return this;
    }

    /// <inheritdoc />
    public FFmpegCommandBuilder NoOverwrite()
    {
        _overwrite = false;
        return this;
    }

    /// <inheritdoc />
    public FFmpegCommandBuilder NoVideo()
    {
        _noVideo = true;
        return this;
    }

    /// <inheritdoc />
    public FFmpegCommandBuilder WithVideo()
    {
        _noVideo = false;
        return this;
    }

    /// <inheritdoc />
    public FFmpegCommandBuilder WithArgument(string argument)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(argument, nameof(argument));
        _customArguments.Add(argument);
        return this;
    }

    /// <inheritdoc />
    public FFmpegCommandBuilder WithArguments(IEnumerable<string> arguments)
    {
        ArgumentHelpers.ThrowIfNull(arguments, nameof(arguments));
        foreach (var arg in arguments) {
            if (!string.IsNullOrWhiteSpace(arg))
                _customArguments.Add(arg);
        }

        return this;
    }

    /// <inheritdoc />
    public FFmpegCommandBuilder WithDefaults(FfmpegOptions options)
    {
        ArgumentHelpers.ThrowIfNull(options, nameof(options));
        _defaults = options;
        if (!string.IsNullOrWhiteSpace(options.FfmpegPath))
            _executablePath = options.FfmpegPath;

        return this;
    }

    /// <summary>Sets the ffmpeg executable path.</summary>
    public FFmpegCommandBuilder WithExecutablePath(string path)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(path, nameof(path));
        _executablePath = path;
        return this;
    }

    /// <inheritdoc />
    public FfmpegCommand Build()
    {
        OperationHelpers.ThrowIfNullOrWhiteSpace(_inputPath, "Input path is required. Call WithInput().");
        OperationHelpers.ThrowIfNullOrWhiteSpace(_outputPath, "Output path is required. Call WithOutput().");
        var opts = _defaults;
        var codec = _codec ?? opts?.DefaultCodec ?? "pcm_s16le";
        var sampleRate = _sampleRate ?? opts?.DefaultSampleRate ?? 44100;
        var channels = _channels ?? opts?.DefaultChannels ?? 2;
        var format = _format ?? opts?.DefaultFormat ?? "wav";
        var overwrite = _overwrite ?? opts?.DefaultOverwrite ?? true;
        var noVideo = _noVideo ?? opts?.DefaultNoVideo ?? true;
        var executablePath = _executablePath ?? opts?.FfmpegPath ?? "ffmpeg";
        var argList = new List<string>();

        // Global arguments from options (e.g., -hide_banner -loglevel warning)
        if (opts?.GlobalArguments != null)
            argList.AddRange(opts.GlobalArguments);

        if (overwrite)
            argList.Add("-y");

        argList.Add("-i");
        argList.Add(_inputPath!);
        if (noVideo)
            argList.Add("-vn");

        argList.Add("-acodec");
        argList.Add(codec);
        argList.Add("-ar");
        argList.Add(sampleRate.ToString());
        argList.Add("-ac");
        argList.Add(channels.ToString());
        argList.AddRange(_customArguments);
        argList.Add(_outputPath!);
        var arguments = string.Join(" ", argList.Select(EscapeArgument));
        return new(executablePath, arguments, argList);
    }

    private static string EscapeArgument(string arg)
    {
        if (string.IsNullOrEmpty(arg))
            return "\"\"";

        if (arg.Contains(' ') || arg.Contains('"') || arg.Contains('\''))
            return $"\"{arg.Replace("\"", "\\\"")}\"";

        return arg;
    }

    /// <summary>Creates a new FFmpegCommandBuilder instance.</summary>
    public static FFmpegCommandBuilder New() => new();

    public override string ToString()
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(_inputPath))
            parts.Add($"Input: {_inputPath}");

        if (!string.IsNullOrWhiteSpace(_outputPath))
            parts.Add($"Output: {_outputPath}");

        if (!string.IsNullOrWhiteSpace(_codec))
            parts.Add($"Codec: {_codec}");

        if (_sampleRate.HasValue)
            parts.Add($"SampleRate: {_sampleRate}");

        if (_channels.HasValue)
            parts.Add($"Channels: {_channels}");

        if (!string.IsNullOrWhiteSpace(_format))
            parts.Add($"Format: {_format}");

        if (_overwrite == true)
            parts.Add("Overwrite");

        if (_noVideo == true)
            parts.Add("NoVideo");

        return $"FFmpegCommandBuilder: {string.Join(", ", parts)}";
    }
}