using System.Globalization;
using Lyo.Common.Core.Records;
using Lyo.Exceptions;
using Lyo.FFmpeg.Models;
using Lyo.Media.Models;
using Lyo.Result;

namespace Lyo.FFmpeg;

internal sealed partial class FFmpegConvertEngine
{
    public async Task<Result<bool>> ConcatAsync(IReadOnlyList<string> inputs, string outputPath, ConvertKnobs opts, CancellationToken ct)
    {
        ArgumentHelpers.ThrowIfNull(inputs);
        ArgumentHelpers.ThrowIf(inputs.Count == 0, "Concat requires at least one input.", nameof(inputs));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(outputPath);
        foreach (var input in inputs)
            FFmpegPathKind.ThrowIfLocalFileMissing(input);

        var list = await FFmpegConcatList.WriteAsync(inputs, ct).ConfigureAwait(false);
        try {
            return await ConvertFileToFileAsync(list, outputPath, PrepareConcat(opts), ct).ConfigureAwait(false);
        }
        finally {
            FFmpegTempHelper.TryDelete(list);
        }
    }

    internal static ConvertKnobs PrepareConcat(ConvertKnobs opts)
    {
        var reencode = opts.VideoEncoder != null || opts.AudioEncoder != null || opts.VideoBitRate != null || opts.Crf != null || opts.AudioBitRate != null;
        return opts with {
            ConcatSafe = true,
            InputFormat = MediaContainer.Custom("concat"),
            CopyCodecs = opts.CopyCodecs || !reencode
        };
    }

    internal static FFmpegCommand BuildExtractFrame(FFmpegOptions options, string input, string output, TimeSpan? at, ConvertKnobs opts)
    {
        var builder = FFmpegCommandBuilder.New()
            .WithDefaults(options)
            .WithInput(input)
            .WithOutput(output)
            .WithStartTime(at ?? TimeSpan.Zero)
            .WithArgument("-frames:v")
            .WithArgument("1");
        if (opts.Overwrite)
            builder.Overwrite();
        else
            builder.NoOverwrite();
        if (opts.Format != null)
            builder.WithFormat(opts.Format);
        if (opts.PixelFormat != null)
            builder.WithPixelFormat(opts.PixelFormat);
        if (opts.Scale == ScaleMode.Stretch && opts.Width is { } width && opts.Height is { } height)
            builder.WithSize(width, height);

        var filter = FFmpegFilterChain.Video(opts.Scale, opts.Width, opts.Height, opts.VideoFilter, opts.Subtitles, opts.SubtitlePath);
        if (filter != null)
            builder.WithVideoFilter(filter);

        return builder.Build();
    }

    internal static (FFmpegCommand Palette, FFmpegCommand Encode) BuildGif(FFmpegOptions options, string input, string palettePath, string output, ConvertKnobs opts)
    {
        var graph = GifGraph(opts);
        var palette = FFmpegCommandBuilder.New().WithDefaults(options).WithInput(input).WithOutput(palettePath).WithVideoFilter(graph + ",palettegen");
        var encode = FFmpegCommandBuilder.New()
            .WithDefaults(options)
            .WithInput(input)
            .WithExtraInput(palettePath)
            .WithOutput(output)
            .WithFormat(MediaContainer.Gif)
            .WithArgument("-filter_complex")
            .WithArgument(graph + "[x];[x][1:v]paletteuse");
        if (opts.Overwrite) {
            palette.Overwrite();
            encode.Overwrite();
        }
        else {
            palette.NoOverwrite();
            encode.NoOverwrite();
        }

        return (palette.Build(), encode.Build());
    }

    internal static FFmpegCommand BuildLoudnessMeasure(FFmpegOptions options, string input, ConvertKnobs opts)
    {
        ArgumentHelpers.ThrowIfNull(opts.Loudness);
        var target = opts.Loudness;
        var culture = CultureInfo.InvariantCulture;
        var filter = "loudnorm=" +
                     $"I={target.IntegratedLufs.ToString("G", culture)}:" +
                     $"TP={target.TruePeak.ToString("G", culture)}:" +
                     $"LRA={target.Range.ToString("G", culture)}:" +
                     "print_format=json";
        return FFmpegCommandBuilder.New()
            .WithDefaults(options)
            .WithInput(input)
            .WithOutput("-")
            .DropVideo()
            .WithAudioFilter(filter)
            .WithFormat("null")
            .WithArgument("-loglevel")
            .WithArgument("info")
            .Build();
    }

    internal static (FFmpegCommand Pass1, FFmpegCommand Pass2) BuildTwoPass(FFmpegOptions options, string input, string output, string prefix, ConvertKnobs opts)
    {
        ThrowIfTwoPass(opts, options);
        var encoder = opts.VideoEncoder == null ? "libx264" : FFmpegCodecMap.ToFfmpegVideo(opts.VideoEncoder.Id);
        var pass1 = FFmpegCommandBuilder.New().WithDefaults(options).WithInput(input).WithOutput("-").DropAudio().WithVideoCodec(encoder);
        if (opts.VideoBitRate != null)
            pass1.WithVideoBitRate(opts.VideoBitRate.Id);
        if (opts.Preset != null)
            pass1.WithPreset(opts.Preset);
        if (opts.PixelFormat != null)
            pass1.WithPixelFormat(opts.PixelFormat);
        if (opts.FrameRate != null)
            pass1.WithFrameRate(opts.FrameRate.Value);
        ApplyScale(pass1, opts);
        if (!opts.Overwrite)
            pass1.NoOverwrite();

        pass1.WithFormat("null").WithPass(1).WithPassLogFile(prefix);
        var pass2 = FFmpegCommandBuilder.New()
            .WithDefaults(options)
            .WithInput(input)
            .WithOutput(output)
            .ApplyKnobs(opts with { TwoPass = false })
            .WithPass(2)
            .WithPassLogFile(prefix);
        if (opts.Progress != null)
            pass2.WithArgument("-progress").WithArgument("pipe:1");

        return (pass1.Build(), pass2.Build());
    }

    internal static IReadOnlyList<string> TwoPassLogs(string prefix) => [prefix + "-0.log", prefix + "-0.log.mbtree"];

    internal static void ThrowIfBlocked(ConvertKnobs opts, FFmpegOptions host, bool outputIsPipe)
    {
        ArgumentHelpers.ThrowIf(IsGif(opts) && outputIsPipe, "GIF cannot use a pipe.");
        ArgumentHelpers.ThrowIf(opts.TwoPass && outputIsPipe, "TwoPass cannot use a pipe.");
        ArgumentHelpers.ThrowIf(opts.Packaging != null && outputIsPipe, "HLS and DASH require a file output.");
        if (opts.TwoPass)
            ThrowIfTwoPass(opts, host);
    }

    internal static void ThrowIfSessionBlocked(ConvertKnobs opts)
    {
        ArgumentHelpers.ThrowIf(IsGif(opts), "GIF cannot use StartConvert.");
        ArgumentHelpers.ThrowIf(opts.TwoPass, "TwoPass cannot use StartConvert.");
        ArgumentHelpers.ThrowIf(opts.Packaging != null, "HLS and DASH require a file output.");
    }

    internal static void ThrowIfTwoPass(ConvertKnobs opts, FFmpegOptions host)
    {
        HardwareAccel? device = opts.Hardware == HardwareAccel.None ? null : opts.Hardware;
        if (device == null && opts.Hardware != HardwareAccel.None) {
            var parsed = HardwareAccel.TryParse(host.Hardware);
            if (parsed != null && parsed != HardwareAccel.None)
                device = parsed;
        }

        ArgumentHelpers.ThrowIf(device != null, "TwoPass cannot use hardware.", nameof(opts));
        var encoder = opts.VideoEncoder == null ? "libx264" : FFmpegCodecMap.ToFfmpegVideo(opts.VideoEncoder.Id);
        ArgumentHelpers.ThrowIf(encoder is not ("libx264" or "libx265"), "TwoPass requires libx264 or libx265.", nameof(opts));
    }

    private async Task<Result<bool>> RunGifAsync(string input, string output, ConvertKnobs opts, CancellationToken ct)
    {
        var palette = FFmpegTempHelper.CreateTempFilePath(".png");
        try {
            var (first, second) = BuildGif(_options, input, palette, output, opts);
            var paletteRun = await RunCommandAsync(first, progress: null, knownDuration: null, progressOnStdout: false, ct).ConfigureAwait(false);
            if (!paletteRun.IsSuccess)
                return paletteRun;

            return await RunCommandAsync(second, progress: opts.Progress, knownDuration: opts.KnownDuration, progressOnStdout: opts.Progress != null, ct)
                .ConfigureAwait(false);
        }
        finally {
            FFmpegTempHelper.TryDelete(palette);
        }
    }

    private async Task<Result<bool>> RunTwoPassAsync(string input, string output, ConvertKnobs opts, CancellationToken ct)
    {
        var prefix = Path.Combine(Path.GetTempPath(), "lyo-ffmpeg-pass-" + Guid.NewGuid().ToString("N"));
        try {
            var (pass1, pass2) = BuildTwoPass(_options, input, output, prefix, opts);
            var first = await RunCommandAsync(pass1, progress: null, knownDuration: null, progressOnStdout: false, ct).ConfigureAwait(false);
            if (!first.IsSuccess)
                return first;

            return await RunCommandAsync(pass2, opts.Progress, opts.KnownDuration, progressOnStdout: opts.Progress != null, ct).ConfigureAwait(false);
        }
        finally {
            foreach (var log in TwoPassLogs(prefix))
                FFmpegTempHelper.TryDelete(log);
        }
    }

    private async Task<Result<bool>> RunLoudnessAsync(
        string input,
        string output,
        Stream? stdin,
        Stream? stdout,
        ConvertKnobs opts,
        bool applyTimeout,
        CancellationToken ct)
    {
        ArgumentHelpers.ThrowIf(string.Equals(input, FFmpegPathKind.PipeInput, StringComparison.Ordinal), "Loudness requires a file input.");
        var measured = await MeasureLoudnessAsync(input, opts, ct).ConfigureAwait(false);
        if (!measured.IsSuccess)
            return Result<bool>.Failure(measured.Errors ?? []);

        var applied = opts with { Loudness = null, AudioFilter = measured.Data };
        return await RunToCompletionAsync(input, output, stdin, stdout, applied, applyTimeout, ct).ConfigureAwait(false);
    }

    private async Task<Result<string>> MeasureLoudnessAsync(string input, ConvertKnobs opts, CancellationToken ct)
    {
        var command = BuildLoudnessMeasure(_options, input, opts);
        var run = await _runner.RunAsync(
                new() {
                    ExecutablePath = command.ExecutablePath,
                    Arguments = command.ArgumentList,
                    Timeout = _options.ProcessTimeout,
                    CancellationToken = ct
                })
            .ConfigureAwait(false);
        var result = ToResult(run, Constants.Errors.FFmpegError);
        if (!result.IsSuccess)
            return Result<string>.Failure(result.Errors ?? []);
        if (opts.Loudness == null || !FFmpegLoudness.TryParse(run.StdErr, out var measured))
            return Result<string>.Failure("loudnorm did not return measurements.", Constants.Errors.FFmpegError);

        return Result<string>.Success(FFmpegLoudness.FormatFilter(opts.Loudness, measured));
    }

    private async Task<Result<bool>> RunCommandAsync(
        FFmpegCommand command,
        IProgress<MediaProgress>? progress,
        TimeSpan? knownDuration,
        bool progressOnStdout,
        CancellationToken ct)
    {
        var run = await _runner.RunAsync(
                new() {
                    ExecutablePath = command.ExecutablePath,
                    Arguments = command.ArgumentList,
                    Progress = progress,
                    KnownDuration = knownDuration,
                    ProgressOnStdout = progressOnStdout,
                    Timeout = _options.ProcessTimeout,
                    CancellationToken = ct
                })
            .ConfigureAwait(false);
        return ToResult(run, Constants.Errors.FFmpegError);
    }

    private static void ApplyScale(FFmpegCommandBuilder builder, ConvertKnobs opts)
    {
        if (opts.Scale == ScaleMode.Stretch && opts.Width is { } width && opts.Height is { } height)
            builder.WithSize(width, height);

        var filter = FFmpegFilterChain.Video(opts.Scale, opts.Width, opts.Height, opts.VideoFilter, opts.Subtitles, opts.SubtitlePath);
        if (filter != null)
            builder.WithVideoFilter(filter);
    }

    private static string GifGraph(ConvertKnobs opts)
    {
        var culture = CultureInfo.InvariantCulture;
        var fps = (opts.FrameRate?.Value ?? 12).ToString("G", culture);
        string scale;
        if (opts.Width is int width && opts.Height is int height)
            scale = $"scale={width}:{height}:force_original_aspect_ratio=decrease:flags=lanczos";
        else if (opts.Width is int onlyWidth)
            scale = $"scale={onlyWidth}:-1:flags=lanczos";
        else if (opts.Height is int onlyHeight)
            scale = $"scale=-1:{onlyHeight}:flags=lanczos";
        else
            scale = "scale=iw:ih:flags=lanczos";

        return $"fps={fps},{scale}";
    }

    private static bool IsGif(ConvertKnobs opts)
        => opts.Format == MediaContainer.Gif || string.Equals(opts.Format?.Format, "gif", StringComparison.OrdinalIgnoreCase);
}
