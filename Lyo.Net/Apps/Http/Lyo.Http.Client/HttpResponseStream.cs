namespace Lyo.Http.Client;

/// <summary>Response-body stream that disposes the <see cref="HttpResponseMessage" /> and request when the stream itself is disposed.</summary>
public sealed class HttpResponseStream(Stream inner, HttpResponseMessage response, HttpRequestMessage request) : Stream
{
    /// <inheritdoc />
    public override bool CanRead => inner.CanRead;

    /// <inheritdoc />
    public override bool CanSeek => inner.CanSeek;

    /// <inheritdoc />
    public override bool CanWrite => false;

    /// <inheritdoc />
    public override long Length => inner.Length;

    /// <inheritdoc />
    public override long Position {
        get => inner.Position;
        set => inner.Position = value;
    }

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);

#if NET5_0_OR_GREATER
    /// <inheritdoc />
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default) => inner.ReadAsync(buffer, ct);
#endif

    /// <inheritdoc />
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct) => inner.ReadAsync(buffer, offset, count, ct);

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);

    /// <inheritdoc />
    public override void SetLength(long value) => inner.SetLength(value);

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    /// <inheritdoc />
    public override void Flush() => inner.Flush();

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) {
            inner.Dispose();
            response.Dispose();
            request.Dispose();
        }

        base.Dispose(disposing);
    }

#if NET5_0_OR_GREATER
    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        await inner.DisposeAsync().ConfigureAwait(false);
        response.Dispose();
        request.Dispose();
        await base.DisposeAsync().ConfigureAwait(false);
    }
#endif
}
