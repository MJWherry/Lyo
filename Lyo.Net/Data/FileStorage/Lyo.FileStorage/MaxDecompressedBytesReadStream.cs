namespace Lyo.FileStorage;

/// <summary>
/// Enforces a maximum number of bytes read from the inner stream (decompression bomb / policy limit).
/// </summary>
internal sealed class MaxDecompressedBytesReadStream : Stream
{
    private readonly Stream _inner;
    private readonly long _maxBytes;
    private readonly Guid _fileId;
    private long _totalRead;
    private bool _disposed;

    public MaxDecompressedBytesReadStream(Stream inner, long maxBytes, Guid fileId)
    {
        _inner = inner;
        _maxBytes = maxBytes;
        _fileId = fileId;
    }

    public override bool CanRead => !_disposed && _inner.CanRead;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => throw new NotSupportedException();

    public override long Position {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
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
        if (_totalRead > _maxBytes)
            throw new InvalidDataException(
                $"Decompressed data for file {_fileId} exceeded maximum allowed size ({_maxBytes} bytes). Possible decompression bomb or misconfiguration.");
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
