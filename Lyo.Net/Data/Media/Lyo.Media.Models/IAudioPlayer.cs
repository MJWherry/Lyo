using Lyo.Result;

namespace Lyo.Media.Models;

/// <summary>Plays audio locally. Network streaming is <see cref="IAudioConverter.StartConvertAsync(string, AudioConversionOptions, CancellationToken)" />, not this type.</summary>
public interface IAudioPlayer
{
    /// <summary>Plays a local path or <c>http(s)://</c> URL.</summary>
    Task<Result<bool>> PlayAsync(string filePath, AudioPlayOptions? options = null, CancellationToken ct = default);

    /// <summary>Plays from a stream. Use <see cref="MediaIoMode.Pipe" /> so large files are not spooled to disk.</summary>
    Task<Result<bool>> PlayStreamAsync(Stream stream, AudioPlayOptions? options = null, CancellationToken ct = default);

    /// <summary>Plays from a byte array.</summary>
    Task<Result<bool>> PlayBytesAsync(byte[] bytes, AudioPlayOptions? options = null, CancellationToken ct = default);
}
