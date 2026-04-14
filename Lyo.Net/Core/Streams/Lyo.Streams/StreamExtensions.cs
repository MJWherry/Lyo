using Lyo.Exceptions;

namespace Lyo.Streams;

/// <summary>Extension methods for Stream.</summary>
public static class StreamExtensions
{
    /// <summary>Copies bytes from the current stream to the destination stream, optionally reporting progress.</summary>
    /// <param name="source">The source stream to copy from.</param>
    /// <param name="destination">The destination stream to copy to.</param>
    /// <param name="bufferSize">The buffer size to use for copying. When null, uses <see cref="StreamChunkSizeHelper" /> to determine an optimal size.</param>
    /// <param name="progress">Optional progress reporter for bytes written. Reports cumulative bytes written.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that represents the asynchronous copy operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when source or destination is null.</exception>
    public static async Task CopyToAsync(this Stream source, Stream destination, int? bufferSize = null, IProgress<long>? progress = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(source, nameof(source));
        ArgumentHelpers.ThrowIfNull(destination, nameof(destination));
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