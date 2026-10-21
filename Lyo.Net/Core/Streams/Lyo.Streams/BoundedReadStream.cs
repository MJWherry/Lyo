using Lyo.Exceptions;

namespace Lyo.Streams;

/// <summary>Reads at most a fixed number of bytes from an inner stream, then returns end-of-stream. Bytes past that cap stay unread. Disposes the inner stream.</summary>
public sealed class BoundedReadStream : Stream
{
    private readonly Stream _inner;
    private bool _disposed;
    private long _remaining;

    /// <inheritdoc />
    public override bool CanRead => !_disposed && _inner.CanRead;

    /// <inheritdoc />
    public override bool CanSeek => false;

    /// <inheritdoc />
    public override bool CanWrite => false;

    /// <inheritdoc />
    public override long Length => throw new NotSupportedException($"{nameof(BoundedReadStream)} does not support seeking.");

    /// <inheritdoc />
    public override long Position {
        get => throw new NotSupportedException($"{nameof(BoundedReadStream)} does not support seeking.");
        set => throw new NotSupportedException($"{nameof(BoundedReadStream)} does not support seeking.");
    }

    /// <summary>Wraps <paramref name="inner" /> so reads stop after <paramref name="count" /> bytes. This wrapper disposes <paramref name="inner" />.</summary>
    /// <param name="inner">Stream to read.</param>
    /// <param name="count">Maximum bytes to return. Zero yields an empty read.</param>
    public BoundedReadStream(Stream inner, long count)
    {
        ArgumentHelpers.ThrowIfNull(inner);
        ArgumentHelpers.ThrowIfNegative(count);
        _inner = inner;
        _remaining = count;
    }

    /// <inheritdoc />
    public override void Flush() => ThrowIfDisposed();

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
    {
        ThrowIfDisposed();
        StreamValidation.ValidateReadBuffer(buffer, offset, count);
        return ReadLimited(count, n => _inner.Read(buffer, offset, n));
    }

#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
    /// <inheritdoc />
    public override int Read(Span<byte> buffer)
    {
        ThrowIfDisposed();
        if (_remaining == 0)
            return 0;

        var n = _inner.Read(buffer[..(int)Math.Min(buffer.Length, _remaining)]);
        if (n > 0)
            _remaining -= n;

        return n;
    }
#endif

    /// <inheritdoc />
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct)
    {
        ThrowIfDisposed();
        StreamValidation.ValidateReadBuffer(buffer, offset, count);
        return ReadLimitedAsync(count, n => _inner.ReadAsync(buffer, offset, n, ct));
    }

#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
    /// <inheritdoc />
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await ReadLimitedAsync(buffer.Length, n => _inner.ReadAsync(buffer[..n], ct).AsTask()).ConfigureAwait(false);
    }
#endif

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException($"{nameof(BoundedReadStream)} does not support seeking.");

    /// <inheritdoc />
    public override void SetLength(long value) => throw new NotSupportedException($"{nameof(BoundedReadStream)} does not support seeking.");

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException($"{nameof(BoundedReadStream)} is read-only.");

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing && !_disposed) {
            _disposed = true;
            _inner.Dispose();
        }

        base.Dispose(disposing);
    }

#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        if (!_disposed) {
            _disposed = true;
            await _inner.DisposeAsync().ConfigureAwait(false);
        }

        await base.DisposeAsync().ConfigureAwait(false);
    }
#endif

    private int ReadLimited(int count, Func<int, int> read)
    {
        if (_remaining == 0)
            return 0;

        var n = read((int)Math.Min(count, _remaining));
        if (n > 0)
            _remaining -= n;

        return n;
    }

    private async Task<int> ReadLimitedAsync(int count, Func<int, Task<int>> read)
    {
        if (_remaining == 0)
            return 0;

        var n = await read((int)Math.Min(count, _remaining)).ConfigureAwait(false);
        if (n > 0)
            _remaining -= n;

        return n;
    }

    private void ThrowIfDisposed() => OperationHelpers.ThrowIfDisposed(_disposed, nameof(BoundedReadStream));
}