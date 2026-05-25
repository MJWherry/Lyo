using Amazon.S3.Model;

namespace Lyo.FileStorage.S3;

/// <summary>
/// Owning wrapper around a <see cref="GetObjectResponse" />'s <c>ResponseStream</c> that disposes the response (and its underlying HTTP handle) when the returned stream is disposed.
/// Without this, callers reading <c>response.ResponseStream</c> directly leak the HTTP connection back to the S3 SDK pool.
/// </summary>
internal sealed class S3GetObjectResponseStream : Stream
{
    private readonly GetObjectResponse _response;
    private readonly Stream _stream;
    private bool _disposed;

    public S3GetObjectResponseStream(GetObjectResponse response)
    {
        _response = response ?? throw new ArgumentNullException(nameof(response));
        _stream = response.ResponseStream ?? throw new InvalidOperationException("GetObjectResponse has no ResponseStream.");
    }

    public override bool CanRead => _stream.CanRead;

    public override bool CanSeek => _stream.CanSeek;

    public override bool CanWrite => false;

    public override long Length => _stream.Length;

    public override long Position {
        get => _stream.Position;
        set => _stream.Position = value;
    }

    public override void Flush() => _stream.Flush();

    public override Task FlushAsync(CancellationToken ct) => _stream.FlushAsync(ct);

    public override int Read(byte[] buffer, int offset, int count) => _stream.Read(buffer, offset, count);

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct) => _stream.ReadAsync(buffer, offset, count, ct);

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default) => _stream.ReadAsync(buffer, ct);

    public override long Seek(long offset, SeekOrigin origin) => _stream.Seek(offset, origin);

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (_disposed) {
            base.Dispose(disposing);
            return;
        }

        _disposed = true;
        if (disposing) {
            try {
                _stream.Dispose();
            }
            finally {
                _response.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        if (_disposed) {
            await base.DisposeAsync().ConfigureAwait(false);
            return;
        }

        _disposed = true;
        try {
            await _stream.DisposeAsync().ConfigureAwait(false);
        }
        finally {
            _response.Dispose();
        }

        await base.DisposeAsync().ConfigureAwait(false);
    }
}
