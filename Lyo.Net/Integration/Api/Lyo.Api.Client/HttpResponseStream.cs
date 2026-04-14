namespace Lyo.Api.Client;

/// <summary>Wraps a response body stream and disposes the <see cref="HttpResponseMessage"/> and request when the stream is disposed.</summary>
public sealed class HttpResponseStream(Stream inner, HttpResponseMessage response, HttpRequestMessage request) : Stream
{
    public override bool CanRead => inner.CanRead;
    public override bool CanSeek => inner.CanSeek;
    public override bool CanWrite => false;
    public override long Length => inner.Length;

    public override long Position
    {
        get => inner.Position;
        set => inner.Position = value;
    }

    public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);

#if NET5_0_OR_GREATER
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        => inner.ReadAsync(buffer, cancellationToken);
#endif

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => inner.ReadAsync(buffer, offset, count, cancellationToken);

    public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);
    public override void SetLength(long value) => inner.SetLength(value);
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override void Flush() => inner.Flush();

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
    public override async ValueTask DisposeAsync()
    {
        await inner.DisposeAsync().ConfigureAwait(false);
        response.Dispose();
        request.Dispose();
        await base.DisposeAsync().ConfigureAwait(false);
    }
#endif
}
