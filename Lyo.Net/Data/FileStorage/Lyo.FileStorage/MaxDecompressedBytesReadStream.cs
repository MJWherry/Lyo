namespace Lyo.FileStorage;

/// <summary>Enforces a maximum number of bytes <em>written</em> to the inner stream (used to bound decompression output during streaming).</summary>
internal sealed class MaxBytesWriteStream : Stream
{
    private readonly Guid _fileId;
    private readonly Stream _inner;
    private readonly long _maxBytes;
    private bool _disposed;
    private long _totalWritten;

    public override bool CanRead => false;

    public override bool CanSeek => false;

    public override bool CanWrite => !_disposed && _inner.CanWrite;

    public override long Length => throw new NotSupportedException();

    public override long Position {
        get => _totalWritten;
        set => throw new NotSupportedException();
    }

    public MaxBytesWriteStream(Stream inner, long maxBytes, Guid fileId)
    {
        _inner = inner;
        _maxBytes = maxBytes;
        _fileId = fileId;
    }

    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Flush() => _inner.Flush();

    public override Task FlushAsync(CancellationToken ct) => _inner.FlushAsync(ct);

    public override void Write(byte[] buffer, int offset, int count)
    {
        EnsureCapacity(count);
        _inner.Write(buffer, offset, count);
        _totalWritten += count;
    }

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken ct)
    {
        EnsureCapacity(count);
        await _inner.WriteAsync(buffer, offset, count, ct).ConfigureAwait(false);
        _totalWritten += count;
    }

#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken ct = default)
    {
        EnsureCapacity(buffer.Length);
        await _inner.WriteAsync(buffer, ct).ConfigureAwait(false);
        _totalWritten += buffer.Length;
    }
#endif

    private void EnsureCapacity(int incoming)
    {
        if (_totalWritten + incoming > _maxBytes) {
            throw new InvalidDataException(
                $"Decompressed data for file {_fileId} exceeded maximum allowed size ({_maxBytes} bytes). Possible decompression bomb or misconfiguration.");
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (!_disposed) {
            _disposed = true;
            // Do not dispose inner — the caller owns the output stream.
        }

        base.Dispose(disposing);
    }
}

/// <summary>Enforces a maximum number of bytes read from the inner stream (decompression bomb / policy limit).</summary>
internal sealed class MaxDecompressedBytesReadStream : Stream
{
    private readonly Guid _fileId;
    private readonly Stream _inner;
    private readonly long _maxBytes;
    private bool _disposed;
    private long _totalRead;

    public override bool CanRead => !_disposed && _inner.CanRead;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => throw new NotSupportedException();

    public override long Position {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public MaxDecompressedBytesReadStream(Stream inner, long maxBytes, Guid fileId)
    {
        _inner = inner;
        _maxBytes = maxBytes;
        _fileId = fileId;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var n = _inner.Read(buffer, offset, count);
        AfterRead(n);
        return n;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct)
    {
        var n = await _inner.ReadAsync(buffer, offset, count, ct).ConfigureAwait(false);
        AfterRead(n);
        return n;
    }

#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default)
    {
        var n = await _inner.ReadAsync(buffer, ct).ConfigureAwait(false);
        AfterRead(n);
        return n;
    }
#endif

    private void AfterRead(int n)
    {
        if (n <= 0)
            return;

        _totalRead += n;
        if (_totalRead > _maxBytes) {
            throw new InvalidDataException(
                $"Decompressed data for file {_fileId} exceeded maximum allowed size ({_maxBytes} bytes). Possible decompression bomb or misconfiguration.");
        }
    }

    public override void Flush() { }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_disposed) {
            _disposed = true;
            _inner.Dispose();
        }

        base.Dispose(disposing);
    }

#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
    public override async ValueTask DisposeAsync()
    {
        if (!_disposed) {
            _disposed = true;
            await _inner.DisposeAsync().ConfigureAwait(false);
        }

        await base.DisposeAsync().ConfigureAwait(false);
    }
#endif
}