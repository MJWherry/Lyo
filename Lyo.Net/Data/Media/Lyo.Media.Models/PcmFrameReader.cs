using System.Buffers;
using System.Runtime.CompilerServices;
using Lyo.Exceptions;

namespace Lyo.Media.Models;

/// <summary>
/// Slices a raw PCM s16le stream into fixed-duration frames. Default frame duration is 20 ms (voice-pump shaped without referencing Discord).
/// Each yielded array is a copy the caller owns. Uses <see cref="ArrayPool{T}" /> for the read buffer.
/// </summary>
/// <remarks>
/// Single-consumer. Do not call <see cref="ReadFramesAsync" /> from two threads against the same instance. Dispose or complete the underlying stream to stop.
/// </remarks>
public sealed class PcmFrameReader
{
    private readonly int _channels;
    private readonly TimeSpan _frameDuration;
    private readonly int _sampleRate;
    private readonly Stream _stream;

    /// <summary>Wraps <paramref name="stream" />. Frame size is <c>sampleRate * channels * 2 * frameDuration</c> bytes.</summary>
    public PcmFrameReader(Stream stream, int sampleRate = 48000, int channels = 2, TimeSpan? frameDuration = null)
    {
        ArgumentHelpers.ThrowIfNull(stream);
        OperationHelpers.ThrowIfNotReadable(stream, $"Stream '{nameof(stream)}' must be readable.");
        ArgumentHelpers.ThrowIfLessThan(sampleRate, 1);
        ArgumentHelpers.ThrowIfLessThan(channels, 1);
        _stream = stream;
        _sampleRate = sampleRate;
        _channels = channels;
        _frameDuration = frameDuration ?? TimeSpan.FromMilliseconds(20);
        ArgumentHelpers.ThrowIf(_frameDuration <= TimeSpan.Zero, "Frame duration must be greater than zero.", nameof(frameDuration));
    }

    /// <summary>Byte length of one full frame.</summary>
    public int FrameSizeBytes
    {
        get
        {
            var seconds = _frameDuration.TotalSeconds;
            return checked((int)Math.Round(_sampleRate * _channels * 2 * seconds, MidpointRounding.AwayFromZero));
        }
    }

    /// <summary>Reads frames until the stream ends. A partial last frame is yielded if any bytes remain.</summary>
    public async IAsyncEnumerable<byte[]> ReadFramesAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        var frameSize = FrameSizeBytes;
        ArgumentHelpers.ThrowIfLessThan(frameSize, 1);
        var rented = ArrayPool<byte>.Shared.Rent(frameSize);
        try {
            var filled = 0;
            while (true) {
                OperationHelpers.ThrowIfCancelled(ct);
                var read = await _stream.ReadAsync(rented.AsMemory(filled, frameSize - filled), ct).ConfigureAwait(false);
                if (read == 0) {
                    if (filled > 0) {
                        var last = new byte[filled];
                        Buffer.BlockCopy(rented, 0, last, 0, filled);
                        yield return last;
                    }

                    yield break;
                }

                filled += read;
                if (filled != frameSize)
                    continue;

                var frame = new byte[frameSize];
                Buffer.BlockCopy(rented, 0, frame, 0, frameSize);
                filled = 0;
                yield return frame;
            }
        }
        finally {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }
}
