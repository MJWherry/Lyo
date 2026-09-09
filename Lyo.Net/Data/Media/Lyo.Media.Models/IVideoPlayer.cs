using Lyo.Result;

namespace Lyo.Media.Models;

/// <summary>Plays video locally. Network streaming is <see cref="IVideoConverter.StartConvertAsync(string, VideoConversionOptions, CancellationToken)" />, not this type. Windowed play is opt-in via <see cref="VideoPlayOptions.NoDisplay" />.</summary>
public interface IVideoPlayer
{
    /// <summary>Plays a local path or <c>http(s)://</c> URL.</summary>
    Task<Result<bool>> PlayAsync(string filePath, VideoPlayOptions? options = null, CancellationToken ct = default);

    /// <summary>Plays from a stream. Use <see cref="MediaIoMode.Pipe" /> so large video is not spooled to disk.</summary>
    Task<Result<bool>> PlayStreamAsync(Stream stream, VideoPlayOptions? options = null, CancellationToken ct = default);

    /// <summary>Plays from a byte array.</summary>
    Task<Result<bool>> PlayBytesAsync(byte[] bytes, VideoPlayOptions? options = null, CancellationToken ct = default);
}
