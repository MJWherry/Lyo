namespace Lyo.FileStorage.AzureBlob;

/// <summary>
/// Stream that owns a download response and disposes it when the caller disposes the stream. Returning <c>BlobDownloadStreamingResult.Content</c> without this wrapper
/// leaves the HTTP connection checked out of the Azure SDK pool.
/// </summary>
internal sealed class AzureBlobDownloadStream : Stream
{
    private readonly IDisposable _owner;
    private readonly Stream _stream;
    private bool _disposed;

    public override bool CanRead => _stream.CanRead;

    public override bool CanSeek => _stream.CanSeek;

    public override bool CanWrite => false;

    public override long Length => _stream.Length;

    public override long Position {
        get => _stream.Position;
        set => _stream.Position = value;
    }

    /// <summary>Wraps <paramref name="content" /> and disposes <paramref name="owner" /> (the SDK result) with the stream.</summary>
    public AzureBlobDownloadStream(Stream content, IDisposable owner)
    {
        _stream = content ?? throw new ArgumentNullException(nameof(content));
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
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
                _owner.Dispose();
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
            if (_owner is IAsyncDisposable asyncOwner)
                await asyncOwner.DisposeAsync().ConfigureAwait(false);
            else
                _owner.Dispose();
        }

        await base.DisposeAsync().ConfigureAwait(false);
    }
}
