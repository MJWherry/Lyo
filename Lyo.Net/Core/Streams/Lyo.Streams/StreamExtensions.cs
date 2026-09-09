using Lyo.Exceptions;

namespace Lyo.Streams;

/// <summary>Helpers for <see cref="Stream" />.</summary>
public static class StreamExtensions
{
    /// <summary>Copies bytes from this stream to <paramref name="destination" />, optionally reporting write progress.</summary>
    /// <param name="source">Stream to copy from.</param>
    /// <param name="destination">Stream to copy to.</param>
    /// <param name="bufferSize">Copy buffer size. Null lets <see cref="StreamChunkSizeHelper" /> pick a size.</param>
    /// <param name="progress">Optional reporter of cumulative bytes written.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Task that completes when the copy finishes.</returns>
    /// <exception cref="ArgumentNullException">Thrown when source or destination is null.</exception>
    public static async Task CopyToAsync(this Stream source, Stream destination, int? bufferSize = null, IProgress<long>? progress = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(source);
        ArgumentHelpers.ThrowIfNull(destination);
        var effectiveBufferSize = bufferSize ?? StreamChunkSizeHelper.DetermineChunkSize(source);
        if (progress == null) {
            await source.CopyToAsync(destination, effectiveBufferSize, ct).ConfigureAwait(false);
            return;
        }

        using var progressStream = new ProgressStream(destination, writeProgress: progress);
        await source.CopyToAsync(progressStream, effectiveBufferSize, ct).ConfigureAwait(false);
        await progressStream.FlushAsync(ct).ConfigureAwait(false);
    }
}