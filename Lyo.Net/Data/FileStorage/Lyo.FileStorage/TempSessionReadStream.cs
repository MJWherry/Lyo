using Lyo.IO.Temp.Models;

namespace Lyo.FileStorage;

/// <summary>FileStream wrapper that disposes the IOTemp session after the zip handle is released.</summary>
internal sealed class TempSessionReadStream : Stream
{
    private readonly FileStream _inner;
    private readonly IIOTempSession _session;
    private int _disposed;

    public TempSessionReadStream(FileStream inner, IIOTempSession session)
    {
        _inner = inner;
        _session = session;
    }

    public override bool CanRead => _inner.CanRead;
    public override bool CanSeek => _inner.CanSeek;
    public override bool CanWrite => false;
    public override long Length => _inner.Length;

    public override long Position
    {
        get => _inner.Position;
        set => _inner.Position = value;
    }

    public override void Flush() => _inner.Flush();
    public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
    public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

#if !NETSTANDARD2_0
    public override async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        await _inner.DisposeAsync().ConfigureAwait(false);
        await _session.DisposeAsync().ConfigureAwait(false);
        await base.DisposeAsync().ConfigureAwait(false);
    }
#endif

    protected override void Dispose(bool disposing)
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) {
            base.Dispose(disposing);
            return;
        }

        if (disposing) {
            _inner.Dispose();
            _session.Dispose();
        }

        base.Dispose(disposing);
    }
}
