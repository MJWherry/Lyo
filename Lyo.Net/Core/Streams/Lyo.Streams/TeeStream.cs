using Lyo.Exceptions;

namespace Lyo.Streams;

/// <summary>A stream that writes to multiple output streams simultaneously (like Unix tee command). This allows computing hashes while streaming data to the processing pipeline.</summary>
public class TeeStream : Stream
{
    private readonly bool _ownsStreams;
    private readonly Stream _primary;
    private readonly Stream[] _secondary;
    private bool _disposed;

    public override bool CanRead => false;

    public override bool CanSeek => false;

    public override bool CanWrite => !_disposed && _primary.CanWrite && _secondary.All(s => s.CanWrite);

    public override long Length => throw new NotSupportedException("TeeStream does not support Length");

    public override long Position {
        get {
            ThrowIfDisposed();
            return _primary.Position;
        }
        set => throw new NotSupportedException("TeeStream does not support seeking");
    }

    /// <summary>Creates a new TeeStream. The caller retains ownership of all streams; they will not be disposed when this stream is disposed.</summary>
    /// <param name="primary">The primary stream to write to.</param>
    /// <param name="secondary">One or more secondary streams to write to.</param>
    public TeeStream(Stream primary, params Stream[] secondary)
        : this(primary, false, secondary) { }

    /// <summary>Creates a new TeeStream.</summary>
    /// <param name="primary">The primary stream to write to.</param>
    /// <param name="ownsStreams">If true, the primary and secondary streams will be disposed when this stream is disposed.</param>
    /// <param name="secondary">One or more secondary streams to write to.</param>
    public TeeStream(Stream primary, bool ownsStreams, params Stream[] secondary)
    {
        ArgumentHelpers.ThrowIfNull(primary, nameof(primary));
        ArgumentHelpers.ThrowIfNull(secondary, nameof(secondary));
        ArgumentHelpers.ThrowIfNullOrEmpty(secondary, nameof(secondary));
        ArgumentHelpers.ThrowIf(secondary!.Any(s => s == null), "Secondary streams cannot be null", nameof(secondary));
        _primary = primary;
        _secondary = secondary;
        _ownsStreams = ownsStreams;
    }

    public override void Flush()
    {
        ThrowIfDisposed();
        _primary.Flush();
        foreach (var stream in _secondary)
            stream.Flush();
    }

    public override async Task FlushAsync(CancellationToken ct)
    {
        ThrowIfDisposed();
        await _primary.FlushAsync(ct).ConfigureAwait(false);
        foreach (var stream in _secondary)
            await stream.FlushAsync(ct).ConfigureAwait(false);
    }

    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException("TeeStream is write-only");

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException("TeeStream does not support seeking");

    public override void SetLength(long value) => throw new NotSupportedException("TeeStream does not support SetLength");

    public override void Write(byte[] buffer, int offset, int count)
    {
        ThrowIfDisposed();
        StreamValidation.ValidateWriteBuffer(buffer, offset, count);
        _primary.Write(buffer, offset, count);
        foreach (var stream in _secondary)
            stream.Write(buffer, offset, count);
    }

#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
    public override void Write(ReadOnlySpan<byte> buffer)
    {
        ThrowIfDisposed();
        _primary.Write(buffer);
        foreach (var stream in _secondary)
            stream.Write(buffer);
    }
#endif

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken ct)
    {
        ThrowIfDisposed();
        StreamValidation.ValidateWriteBuffer(buffer, offset, count);
        await _primary.WriteAsync(buffer, offset, count, ct).ConfigureAwait(false);
        foreach (var stream in _secondary)
            await stream.WriteAsync(buffer, offset, count, ct).ConfigureAwait(false);
    }

#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        await _primary.WriteAsync(buffer, ct).ConfigureAwait(false);
        foreach (var stream in _secondary)
            await stream.WriteAsync(buffer, ct).ConfigureAwait(false);
    }
#endif

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(TeeStream));
    }

    protected override void Dispose(bool disposing)
    {
        if (!_disposed && disposing) {
            if (_ownsStreams) {
                try {
                    _primary.Dispose();
                }
                catch {
                    // Ignore disposal errors
                }

                foreach (var stream in _secondary) {
                    try {
                        stream.Dispose();
                    }
                    catch {
                        // Ignore disposal errors
                    }
                }
            }

            _disposed = true;
        }

        base.Dispose(disposing);
    }
}