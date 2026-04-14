using Lyo.Common;

namespace Lyo.Ffmpeg.Models;

/// <summary>Service interface for playing audio files.</summary>
public interface IAudioPlayer
{
    /// <summary>Plays an audio file by path.</summary>
    Task<Result<bool>> PlayAsync(string filePath, CancellationToken ct = default);

    /// <summary>Plays audio from a stream.</summary>
    Task<Result<bool>> PlayStreamAsync(Stream stream, CancellationToken ct = default);

    /// <summary>Plays audio from a byte array.</summary>
    Task<Result<bool>> PlayBytesAsync(byte[] bytes, CancellationToken ct = default);
}