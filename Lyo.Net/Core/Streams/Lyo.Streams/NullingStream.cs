using Lyo.Exceptions;

namespace Lyo.Streams;

/// <summary>
/// A write-only sink that accepts bytes and discards them (like <c>/dev/null</c>). Use as the consumer for stream encrypt/compress throughput paths when the output need not
/// be retained.
/// </summary>
public sealed class NullingStream : Stream
{
    private bool _disposed;

    /// <summary>Total number of bytes accepted by write operations.</summary>
    public long BytesWritten { get; private set; }

    /// <inheritdoc />
    public override bool CanRead => false;

    /// <inheritdoc />
    public override bool CanSeek => false;

    /// <inheritdoc />
    public override bool CanWrite => !_disposed;

    /// <inheritdoc />
    public override long Length => throw new NotSupportedException($"{nameof(NullingStream)} does not support seeking.");

    /// <inheritdoc />
    public override long Position {
        get => throw new NotSupportedException($"{nameof(NullingStream)} does not support seeking.");
        set => throw new NotSupportedException($"{nameof(NullingStream)} does not support seeking.");
    }

    /// <inheritdoc />
    public override void Flush() => ThrowIfDisposed();

    /// <inheritdoc />
    public override Task FlushAsync(CancellationToken ct)
    {
        ThrowIfDisposed();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException($"{nameof(NullingStream)} is write-only.");

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException($"{nameof(NullingStream)} does not support seeking.");

    /// <inheritdoc />
    public override void SetLength(long value) => throw new NotSupportedException($"{nameof(NullingStream)} does not support seeking.");

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count)
    {
        ThrowIfDisposed();
        StreamValidation.ValidateWriteBuffer(buffer, offset, count);
        BytesWritten += count;
    }

#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
    /// <inheritdoc />
    public override void Write(ReadOnlySpan<byte> buffer)
    {
        ThrowIfDisposed();
        BytesWritten += buffer.Length;
    }
#endif

    /// <inheritdoc />
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken ct)
    {
        Write(buffer, offset, count);
        return Task.CompletedTask;
    }

#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
    /// <inheritdoc />
    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        BytesWritten += buffer.Length;
        return ValueTask.CompletedTask;
    }
#endif

    /// <summary>Resets <see cref="BytesWritten" /> to zero.</summary>
    public void ResetCounter() => BytesWritten = 0;

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        _disposed = true;
        base.Dispose(disposing);
    }

    private void ThrowIfDisposed() => OperationHelpers.ThrowIfDisposed(_disposed, nameof(NullingStream));
}