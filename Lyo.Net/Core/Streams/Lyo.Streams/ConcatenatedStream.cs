using Lyo.Exceptions;

namespace Lyo.Streams;

/// <summary>A stream that concatenates multiple input streams sequentially, reading from each one in order until all are exhausted.</summary>
public class ConcatenatedStream : Stream
{
    private readonly bool _ownsStreams;
    private readonly IEnumerator<Stream> _streamEnumerator;
    private Stream? _currentStream;
    private bool _disposed;

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => throw new NotSupportedException("ConcatenatedStream does not support Length");

    public override long Position {
        get => throw new NotSupportedException("ConcatenatedStream does not support Position");
        set => throw new NotSupportedException("ConcatenatedStream does not support seeking");
    }

    /// <summary>Creates a new ConcatenatedStream from a collection of streams.</summary>
    /// <param name="streams">The streams to concatenate</param>
    /// <param name="ownsStreams">If true, the streams will be disposed when this stream is disposed</param>
    public ConcatenatedStream(IEnumerable<Stream> streams, bool ownsStreams = false)
    {
        ArgumentHelpers.ThrowIfNull(streams, nameof(streams));
        var streamList = streams.ToList();
        OperationHelpers.ThrowIfNullOrEmpty(streamList, nameof(streams));
        ArgumentHelpers.ThrowIf(streamList.Any(s => s == null), "Streams cannot be null", nameof(streams));
        _streamEnumerator = streamList.GetEnumerator();
        _ownsStreams = ownsStreams;
        MoveToNextStream();
    }

    /// <summary>Creates a new ConcatenatedStream from multiple streams.</summary>
    /// <param name="ownsStreams">If true, the streams will be disposed when this stream is disposed</param>
    /// <param name="streams">The streams to concatenate</param>
    public ConcatenatedStream(bool ownsStreams, params Stream[] streams)
        : this(streams, ownsStreams) { }

    private void MoveToNextStream()
    {
        if (_currentStream != null && _ownsStreams) {
            try {
                _currentStream.Dispose();
            }
            catch {
                // Ignore disposal errors
            }
        }

        _currentStream = null;
        if (_streamEnumerator.MoveNext()) {
            _currentStream = _streamEnumerator.Current;
            OperationHelpers.ThrowIfNull(_currentStream, "Enumerator returned a null stream");
        }
    }

    public override void Flush()
    {
        ThrowIfDisposed();
        _currentStream?.Flush();
    }

    public override async Task FlushAsync(CancellationToken ct)
    {
        ThrowIfDisposed();
        if (_currentStream != null)
            await _currentStream.FlushAsync(ct).ConfigureAwait(false);
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        ThrowIfDisposed();
        StreamValidation.ValidateReadBuffer(buffer, offset, count);
        if (_currentStream == null)
            return 0;

        var totalBytesRead = 0;
        while (count > 0 && _currentStream != null) {
            var bytesRead = _currentStream.Read(buffer, offset, count);
            if (bytesRead == 0) {
                MoveToNextStream();
                if (_currentStream == null)
                    break;
            }
            else {
                totalBytesRead += bytesRead;
                offset += bytesRead;
                count -= bytesRead;
            }
        }

        return totalBytesRead;
    }

#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
    public override int Read(Span<byte> buffer)
    {
        ThrowIfDisposed();
        if (_currentStream == null)
            return 0;

        var totalBytesRead = 0;
        var remaining = buffer;
        while (remaining.Length > 0 && _currentStream != null) {
            var bytesRead = _currentStream.Read(remaining);
            if (bytesRead == 0) {
                MoveToNextStream();
                if (_currentStream == null)
                    break;
            }
            else {
                totalBytesRead += bytesRead;
                remaining = remaining[bytesRead..];
            }
        }

        return totalBytesRead;
    }
#endif

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct)
    {
        ThrowIfDisposed();
        StreamValidation.ValidateReadBuffer(buffer, offset, count);
        if (_currentStream == null)
            return 0;

        var totalBytesRead = 0;
        while (count > 0 && _currentStream != null) {
            var bytesRead = await _currentStream.ReadAsync(buffer, offset, count, ct).ConfigureAwait(false);
            if (bytesRead == 0) {
                MoveToNextStream();
                if (_currentStream == null)
                    break;
            }
            else {
                totalBytesRead += bytesRead;
                offset += bytesRead;
                count -= bytesRead;
            }
        }

        return totalBytesRead;
    }

#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        if (_currentStream == null)
            return 0;

        var totalBytesRead = 0;
        var remaining = buffer;
        while (remaining.Length > 0 && _currentStream != null) {
            var bytesRead = await _currentStream.ReadAsync(remaining, ct).ConfigureAwait(false);
            if (bytesRead == 0) {
                MoveToNextStream();
                if (_currentStream == null)
                    break;
            }
            else {
                totalBytesRead += bytesRead;
                remaining = remaining[bytesRead..];
            }
        }

        return totalBytesRead;
    }
#endif

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException("ConcatenatedStream does not support seeking");

    public override void SetLength(long value) => throw new NotSupportedException("ConcatenatedStream does not support SetLength");

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException("ConcatenatedStream is read-only");

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ConcatenatedStream));
    }

    protected override void Dispose(bool disposing)
    {
        if (!_disposed && disposing) {
            if (_currentStream != null && _ownsStreams) {
                try {
                    _currentStream.Dispose();
                }
                catch {
                    // Ignore disposal errors
                }
            }

            _streamEnumerator.Dispose();
            _disposed = true;
        }

        base.Dispose(disposing);
    }
}