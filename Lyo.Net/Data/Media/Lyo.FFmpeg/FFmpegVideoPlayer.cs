using Lyo.Exceptions;
using Lyo.FFmpeg.Models;
using Lyo.Media.Models;
using Lyo.Metrics;
using Lyo.Result;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.FFmpeg;

/// <summary><see cref="IVideoPlayer" /> that plays video through ffplay. Concurrent play calls on one instance are safe. Windowed play is opt-in.</summary>
public sealed class FFmpegVideoPlayer : IVideoPlayer
{
    private readonly FFmpegPlayEngine _engine;

    /// <summary>Creates a player.</summary>
    public FFmpegVideoPlayer(FFmpegOptions? options = null, ILogger<FFmpegVideoPlayer>? logger = null, IMetrics? metrics = null)
        => _engine = new(options ?? new FFmpegOptions(), logger ?? NullLogger<FFmpegVideoPlayer>.Instance, metrics);

    /// <inheritdoc />
    public Task<Result<bool>> PlayAsync(string filePath, VideoPlayOptions? options = null, CancellationToken ct = default)
    {
        var opts = Prepare(options);
        return _engine.PlayAsync(filePath, opts.NoDisplay, opts.AutoExit, opts.IoMode, opts.StartTime, opts.Duration, ct);
    }

    /// <inheritdoc />
    public Task<Result<bool>> PlayStreamAsync(Stream stream, VideoPlayOptions? options = null, CancellationToken ct = default)
    {
        var opts = Prepare(options);
        return _engine.PlayStreamAsync(stream, opts.NoDisplay, opts.AutoExit, opts.IoMode, opts.StartTime, opts.Duration, ct);
    }

    /// <inheritdoc />
    public Task<Result<bool>> PlayBytesAsync(byte[] bytes, VideoPlayOptions? options = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(bytes);
        return PlayStreamAsync(new MemoryStream(bytes, writable: false), options, ct);
    }

    internal static List<string> BuildPlayArguments(string input, VideoPlayOptions opts, bool suppressOutput)
        => FFmpegPlayEngine.BuildPlayArguments(input, opts.NoDisplay, opts.AutoExit, opts.StartTime, opts.Duration, suppressOutput);

    private static VideoPlayOptions Prepare(VideoPlayOptions? options)
    {
        var opts = options ?? new VideoPlayOptions();
        opts.Validate();
        return opts;
    }
}
