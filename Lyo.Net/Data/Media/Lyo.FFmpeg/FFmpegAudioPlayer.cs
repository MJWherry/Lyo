using Lyo.Exceptions;
using Lyo.FFmpeg.Models;
using Lyo.Media.Models;
using Lyo.Metrics;
using Lyo.Result;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.FFmpeg;

/// <summary><see cref="IAudioPlayer" /> that plays audio through ffplay. Concurrent play calls on one instance are safe.</summary>
public sealed class FFmpegAudioPlayer : IAudioPlayer
{
    private readonly FFmpegPlayEngine _engine;

    /// <summary>Creates a player.</summary>
    public FFmpegAudioPlayer(FFmpegOptions? options = null, ILogger<FFmpegAudioPlayer>? logger = null, IMetrics? metrics = null)
        => _engine = new(options ?? new FFmpegOptions(), logger ?? NullLogger<FFmpegAudioPlayer>.Instance, metrics);

    /// <inheritdoc />
    public Task<Result<bool>> PlayAsync(string filePath, AudioPlayOptions? options = null, CancellationToken ct = default)
    {
        var opts = Prepare(options);
        return _engine.PlayAsync(filePath, opts.NoDisplay, opts.AutoExit, opts.IoMode, opts.StartTime, opts.Duration, ct);
    }

    /// <inheritdoc />
    public Task<Result<bool>> PlayStreamAsync(Stream stream, AudioPlayOptions? options = null, CancellationToken ct = default)
    {
        var opts = Prepare(options);
        return _engine.PlayStreamAsync(stream, opts.NoDisplay, opts.AutoExit, opts.IoMode, opts.StartTime, opts.Duration, ct);
    }

    /// <inheritdoc />
    public Task<Result<bool>> PlayBytesAsync(byte[] bytes, AudioPlayOptions? options = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(bytes);
        return PlayStreamAsync(new MemoryStream(bytes, writable: false), options, ct);
    }

    internal static List<string> BuildPlayArguments(string input, AudioPlayOptions opts, bool suppressOutput)
        => FFmpegPlayEngine.BuildPlayArguments(input, opts.NoDisplay, opts.AutoExit, opts.StartTime, opts.Duration, suppressOutput);

    private static AudioPlayOptions Prepare(AudioPlayOptions? options)
    {
        var opts = options ?? new AudioPlayOptions();
        opts.Validate();
        return opts;
    }
}
