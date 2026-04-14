using Lyo.Exceptions;

namespace Lyo.Streams;

/// <summary>A stream wrapper that reports progress as data is read or written.</summary>
public class ProgressStream : Stream
{
    private readonly Stream _baseStream;
    private readonly IProgress<long>? _readProgress;
    private readonly IProgress<long>? _writeProgress;
    private bool _disposed;

    public override bool CanRead => !_disposed && _baseStream.CanRead;

    public override bool CanSeek => !_disposed && _baseStream.CanSeek;

    public override bool CanWrite => !_disposed && _baseStream.CanWrite;

    public override long Length {
        get {
            ThrowIfDisposed();
            return _baseStream.Length;
        }
    }

    public override long Position {
        get {
            ThrowIfDisposed();
            return _baseStream.Position;
        }
        set {
            ThrowIfDisposed();
            _baseStream.Position = value;
        }
    }

    /// <summary>Gets the total number of bytes read from this stream.</summary>
    public long TotalBytesRead { get; private set; }

    /// <summary>Gets the total number of bytes written to this stream.</summary>
    public long TotalBytesWritten { get; private set; }

    /// <summary>Gets the total number of bytes transferred (read + written).</summary>
    public long TotalBytesTransferred => TotalBytesRead + TotalBytesWritten;

    public ProgressStream(Stream baseStream, IProgress<long>? readProgress = null, IProgress<long>? writeProgress = null)
    {
        ArgumentHelpers.ThrowIfNull(baseStream, nameof(baseStream));
        _baseStream = baseStream;
        _readProgress = readProgress;
        _writeProgress = writeProgress;
    }

    public override void Flush()
    {
        ThrowIfDisposed();
        _baseStream.Flush();
    }

    public override async Task FlushAsync(CancellationToken ct)
    {
        ThrowIfDisposed();
        await _baseStream.FlushAsync(ct).ConfigureAwait(false);
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        ThrowIfDisposed();
        StreamValidation.ValidateReadBuffer(buffer, offset, count);
        var bytesRead = _baseStream.Read(buffer, offset, count);
        if (bytesRead > 0) {
            TotalBytesRead += bytesRead;
            _readProgress?.Report(TotalBytesRead);
        }

        return bytesRead;
    }

#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
    public override int Read(Span<byte> buffer)
    {
        ThrowIfDisposed();
        var bytesRead = _baseStream.Read(buffer);
        if (bytesRead > 0) {
            TotalBytesRead += bytesRead;
            _readProgress?.Report(TotalBytesRead);
        }

        return bytesRead;
    }
#endif

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct)
    {
        ThrowIfDisposed();
        StreamValidation.ValidateReadBuffer(buffer, offset, count);
        var bytesRead = await _baseStream.ReadAsync(buffer, offset, count, ct).ConfigureAwait(false);
        if (bytesRead > 0) {
            TotalBytesRead += bytesRead;
            _readProgress?.Report(TotalBytesRead);
        }

        return bytesRead;
    }

#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        var bytesRead = await _baseStream.ReadAsync(buffer, ct).ConfigureAwait(false);
        if (bytesRead > 0) {
            TotalBytesRead += bytesRead;
            _readProgress?.Report(TotalBytesRead);
        }

        return bytesRead;
    }
#endif

    public override long Seek(long offset, SeekOrigin origin)
    {
        ThrowIfDisposed();
        return _baseStream.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
        ThrowIfDisposed();
        _baseStream.SetLength(value);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        ThrowIfDisposed();
        StreamValidation.ValidateWriteBuffer(buffer, offset, count);
        _baseStream.Write(buffer, offset, count);
        TotalBytesWritten += count;
        _writeProgress?.Report(TotalBytesWritten);
    }

#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
    public override void Write(ReadOnlySpan<byte> buffer)
    {
        ThrowIfDisposed();
        _baseStream.Write(buffer);
        TotalBytesWritten += buffer.Length;
        _writeProgress?.Report(TotalBytesWritten);
    }
#endif

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken ct)
    {
        ThrowIfDisposed();
        StreamValidation.ValidateWriteBuffer(buffer, offset, count);
        await _baseStream.WriteAsync(buffer, offset, count, ct).ConfigureAwait(false);
        TotalBytesWritten += count;
        _writeProgress?.Report(TotalBytesWritten);
    }

#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        await _baseStream.WriteAsync(buffer, ct).ConfigureAwait(false);
        TotalBytesWritten += buffer.Length;
        _writeProgress?.Report(TotalBytesWritten);
    }
#endif

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(GetType().Name);
    }

    protected override void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
            _disposed = true;

        base.Dispose(disposing);
    }
}