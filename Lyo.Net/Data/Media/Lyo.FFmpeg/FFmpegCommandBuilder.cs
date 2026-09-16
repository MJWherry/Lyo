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
    private static readonly string[] PreInputOrder = ["-hwaccel", "-hwaccel_output_format", "-vaapi_device", "-ss", "-f", "-safe", "-ar", "-ac"];
    private static readonly string[] PostInputOrder = [
        "-t", "-map_metadata", "-c", "-c:a", "-ar", "-ac", "-b:a", "-q:a", "-c:v", "-rc:v", "-cq", "-global_quality", "-qp", "-crf", "-preset", "-b:v", "-r",
        "-pix_fmt", "-s", "-vf", "-af", "-c:s", "-pass", "-passlogfile", "-movflags", "-hls_time", "-hls_playlist_type", "-hls_segment_filename", "-seg_duration",
        "-init_seg_name", "-media_seg_name"
    ];
    private static readonly string[] PreOutputOrder = ["-f"];

    private readonly List<string> _customArguments = [];
    private readonly List<string> _extraInputs = [];
    private readonly List<string> _maps = [];
    private readonly List<string> _metadata = [];
    private readonly Dictionary<string, string> _postInput = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _preInput = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _preOutput = new(StringComparer.Ordinal);
    private FFmpegOptions? _defaults;
    private bool? _dropAudio;
    private bool? _dropVideo;
    private string? _executablePath;
    private HardwareAccel? _requestedHardware;
    private HardwareDevice? _requestedDevice;
    private bool _hasUserVideoFilter;
    private string? _codecId;
    private string? _segmentName;
    private bool _hls;
    private bool _dash;
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
        if (options.VideoEncoder == VideoEncoder.Av1 && options.Preset is { IsX264: true })
            throw new ArgumentException("AV1 does not accept an x264 preset. Use EncoderPreset.Svt8.", nameof(options));

        Apply(options.Format, WithFormat);
        Apply(options.AudioEncoder, WithAudioCodec);
        if (options.SampleRate is { } sampleRate)
            WithSampleRate(sampleRate.Hertz);
        if (options.Channels is { } channels)
            WithChannels(channels.Count);
        if (options.AudioBitRate is { } audioBitRate)
            WithAudioBitRate(audioBitRate.Id);
        if (options.AudioQuality is { } audioQuality)
            WithAudioQuality(audioQuality.Id);
        Apply(options.AudioFilter, WithAudioFilter);
        if (options.VideoEncoder is { } videoEncoder)
            WithVideoCodec(videoEncoder);
        if (options.FrameRate is { } frameRate)
            WithFrameRate(frameRate.Value);
        Apply(options.PixelFormat, WithPixelFormat);
        if (options.Crf is { } crf)
            WithCrf(crf.Value);
        if (options.VideoBitRate is { } videoBitRate)
            WithVideoBitRate(videoBitRate.Id);
        Apply(options.Preset, WithPreset);
        Apply(options.DropVideo, DropVideo);
        Apply(options.DropAudio, DropAudio);
        Apply(options.CopyCodecs, CopyCodecs);
        Apply(options.FragmentedOutput, FragmentedOutput);
        Apply(options.StartTime, WithStartTime);
        Apply(options.Duration, WithDuration);
        Apply(options.InputFormat, WithInputFormat);
        if (options.InputSampleRate is { } inputRate)
            WithInputSampleRate(inputRate.Hertz);
        if (options.InputChannels is { } inputChannels)
            WithInputChannels(inputChannels.Count);
        if (options.Scale == ScaleMode.Stretch && options.Width is { } width && options.Height is { } height)
            WithSize(width, height);

        _hasUserVideoFilter = !string.IsNullOrWhiteSpace(options.VideoFilter);
        var filter = FFmpegFilterChain.Video(options.Scale, options.Width, options.Height, options.VideoFilter, options.Subtitles, options.SubtitlePath);
        if (filter != null)
            WithVideoFilter(filter);

        ApplyMaps(options);
        ApplyMetadata(options);
        ApplyPackaging(options);
        ApplySubtitles(options);
        if (options.ConcatSafe)
            Pair(_preInput, "-safe", "0");

        _requestedHardware = options.Hardware;
        _requestedDevice = options.HardwareDevice;
        _codecId = options.VideoEncoder?.Id;
        if (options.Hardware != null && options.Hardware != HardwareAccel.None)
            ApplyDevice(options.Hardware, options.HardwareDevice?.Path);

        return options.Overwrite ? Overwrite() : NoOverwrite();
    }

    /// <summary>
    /// Applies <see cref="FFmpegOptions.Hardware" /> when the knobs did not set a device.
    /// <see cref="HardwareAccel.None" /> on the knobs forces the CPU. A device already set on the knobs is left as-is.
    /// </summary>
    internal FFmpegCommandBuilder ApplyHost(FFmpegOptions host)
    {
        ArgumentHelpers.ThrowIfNull(host);
        if (_requestedHardware != null)
            return this;

        var parsed = HardwareAccel.TryParse(host.Hardware);
        if (parsed == null || parsed == HardwareAccel.None)
            return this;

        ApplyDevice(parsed, host.HardwareDevice);
        return this;
    }

    /// <summary>Adds another <c>-i</c> after the primary input. Used for subtitle files and GIF palettes.</summary>
    public FFmpegCommandBuilder WithExtraInput(string path)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(path);
        _extraInputs.Add(path);
        return this;
    }

    /// <summary>Sets <c>-pass</c> for libx264 or libx265 two-pass.</summary>
    public FFmpegCommandBuilder WithPass(int pass)
    {
        ArgumentHelpers.ThrowIfLessThan(pass, 1);
        return Pair(_postInput, "-pass", Number(pass));
    }

    /// <summary>Sets <c>-passlogfile</c> without the <c>-0.log</c> suffix ffmpeg appends.</summary>
    public FFmpegCommandBuilder WithPassLogFile(string prefix)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(prefix);
        return Pair(_postInput, "-passlogfile", prefix);
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
        foreach (var extra in _extraInputs) {
            argList.Add("-i");
            argList.Add(extra);
        }

        if (_dropVideo == true)
            argList.Add("-vn");
        if (_dropAudio == true)
            argList.Add("-an");

        foreach (var map in _maps) {
            argList.Add("-map");
            argList.Add(map);
        }

        ApplySegmentNames();
        Emit(argList, _postInput, PostInputOrder);
        foreach (var tag in _metadata) {
            argList.Add("-metadata");
            argList.Add(tag);
        }

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

    private void ApplyDevice(HardwareAccel device, string? devicePath)
    {
        if (_dropVideo == true || IsGif() || IsCopyOnly())
            return;

        var codecId = _codecId ?? "h264";
        Pair(_postInput, "-c:v", FFmpegCodecMap.ToFfmpegVideo(codecId, device));
        Pair(_preInput, "-hwaccel", FFmpegCodecMap.HwaccelFlag(device));
        if (device == HardwareAccel.Nvenc && _postInput.TryGetValue("-preset", out var presetId) && EncoderPreset.TryFromId(presetId) is { IsX264: true })
            Pair(_postInput, "-preset", EncoderPreset.P4.Id);

        RemapQuality(device, devicePath);
    }

    private bool IsGif() => _preOutput.TryGetValue("-f", out var format) && string.Equals(format, "gif", StringComparison.OrdinalIgnoreCase);

    private bool IsCopyOnly() => _postInput.TryGetValue("-c", out var copy) && copy == "copy" && !_postInput.ContainsKey("-c:v");

    private void RemapQuality(HardwareAccel device, string? devicePath)
    {
        if (!_postInput.TryGetValue("-crf", out var crf)) {
            if (device == HardwareAccel.Vaapi)
                ApplyVaapi(devicePath, upload: !_hasUserVideoFilter);
            return;
        }

        if (device == HardwareAccel.VideoToolbox && !_postInput.ContainsKey("-b:v"))
            throw new ArgumentException("VideoToolbox requires VideoBitRate.", nameof(device));

        _postInput.Remove("-crf");
        if (device == HardwareAccel.Nvenc) {
            Pair(_postInput, "-rc:v", "vbr");
            Pair(_postInput, "-cq", crf);
        }
        else if (device == HardwareAccel.Qsv) {
            Pair(_postInput, "-global_quality", crf);
        }
        else if (device == HardwareAccel.Vaapi) {
            Pair(_postInput, "-qp", crf);
            ApplyVaapi(devicePath, upload: !_hasUserVideoFilter);
        }
    }

    private void ApplyVaapi(string? devicePath, bool upload)
    {
        Pair(_preInput, "-hwaccel_output_format", "vaapi");
        Pair(_preInput, "-vaapi_device", string.IsNullOrWhiteSpace(devicePath) ? HardwareDevice.RenderD128.Path : devicePath);
        if (!upload)
            return;

        if (_postInput.TryGetValue("-vf", out var filter))
            Pair(_postInput, "-vf", "format=nv12,hwupload," + filter);
        else
            Pair(_postInput, "-vf", "format=nv12,hwupload");
    }

    private void ApplyMaps(ConvertKnobs options)
    {
        if (options.Maps != null) {
            foreach (var map in options.Maps)
                _maps.Add(map.Spec);
        }

        if (options.Subtitles != SubtitleMode.Copy)
            return;

        var hasSecond = _maps.Any(static map => map.StartsWith("1:", StringComparison.Ordinal) || map == "1");
        if (hasSecond)
            return;

        if (_maps.Count == 0)
            _maps.Add("0");
        _maps.Add("1");
    }

    private void ApplyMetadata(ConvertKnobs options)
    {
        if (options.ClearMetadata)
            Pair(_postInput, "-map_metadata", "-1");
        if (options.Metadata == null)
            return;

        foreach (var pair in options.Metadata)
            _metadata.Add($"{pair.Key.Id}={pair.Value}");
    }

    private void ApplyPackaging(ConvertKnobs options)
    {
        if (options.Packaging is not { } packaging)
            return;

        var seconds = (packaging.SegmentDuration ?? TimeSpan.FromSeconds(packaging.IsDash ? 4 : 6)).TotalSeconds.ToString("G", CultureInfo.InvariantCulture);
        _segmentName = packaging.SegmentFileName;
        if (packaging.IsDash) {
            _dash = true;
            Pair(_postInput, "-seg_duration", seconds);
            return;
        }

        _hls = true;
        Pair(_postInput, "-hls_time", seconds);
        Pair(_postInput, "-hls_playlist_type", (packaging.PlaylistType ?? HlsPlaylistType.Vod).Id);
    }

    private void ApplySubtitles(ConvertKnobs options)
    {
        if (options.Subtitles != SubtitleMode.Copy || string.IsNullOrWhiteSpace(options.SubtitlePath))
            return;

        _extraInputs.Add(options.SubtitlePath);
        Pair(_postInput, "-c:s", FFmpegFilterChain.CodecFor(options.Format, options.SubtitleCodec).Id);
    }

    private void ApplySegmentNames()
    {
        if (_hls)
            Pair(_postInput, "-hls_segment_filename", _segmentName ?? Beside(_outputPath!, "seg_%03d.ts"));
        if (!_dash)
            return;

        Pair(_postInput, "-init_seg_name", _segmentName ?? Beside(_outputPath!, "init.m4s"));
        Pair(_postInput, "-media_seg_name", Beside(_outputPath!, "chunk_%03d.m4s"));
    }

    private static string Beside(string output, string name)
    {
        var dir = Path.GetDirectoryName(output);
        return string.IsNullOrEmpty(dir) ? name : Path.Combine(dir, name);
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
