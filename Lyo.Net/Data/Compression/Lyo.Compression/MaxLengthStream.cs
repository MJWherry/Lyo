namespace Lyo.Compression;

/// <summary>
/// Write-only pass-through stream that throws <see cref="InvalidDataException" /> as soon as the total number of bytes written exceeds a limit. Wrapped around decompression
/// output streams so a decompression bomb is stopped mid-flight — including for non-seekable outputs where an after-the-fact position check is impossible. Does not own or dispose the
/// inner stream.
/// </summary>
internal sealed class MaxLengthStream : Stream
{
    private readonly Stream _inner;
    private readonly long _maxLength;

    /// <summary>Total bytes written through this wrapper so far.</summary>
    public long BytesWritten { get; private set; }

    public override bool CanRead => false;

    public override bool CanSeek => false;

    public override bool CanWrite => _inner.CanWrite;

    public override long Length => BytesWritten;

    public override long Position {
        get => BytesWritten;
        set => throw new NotSupportedException();
    }

    /// <param name="inner">Destination stream that receives all writes.</param>
    /// <param name="maxLength">Maximum cumulative bytes that may be written before the stream throws.</param>
    public MaxLengthStream(Stream inner, long maxLength)
    {
        _inner = inner;
        _maxLength = maxLength;
    }

    public override void Flush() => _inner.Flush();

    public override Task FlushAsync(CancellationToken cancellationToken) => _inner.FlushAsync(cancellationToken);

    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count)
    {
        EnsureWithinLimit(count);
        _inner.Write(buffer, offset, count);
    }

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        EnsureWithinLimit(count);
        await _inner.WriteAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
    }

    public override void WriteByte(byte value)
    {
        EnsureWithinLimit(1);
        _inner.WriteByte(value);
    }

    private void EnsureWithinLimit(int count)
    {
        BytesWritten += count;
        if (BytesWritten > _maxLength)
            throw new InvalidDataException($"Decompressed size exceeds maximum allowed input size ({_maxLength} bytes). Possible decompression bomb.");
    }

#if !NETSTANDARD2_0
    public override void Write(ReadOnlySpan<byte> buffer)
    {
        EnsureWithinLimit(buffer.Length);
        _inner.Write(buffer);
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        EnsureWithinLimit(buffer.Length);
        await _inner.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
    }
#endif
}