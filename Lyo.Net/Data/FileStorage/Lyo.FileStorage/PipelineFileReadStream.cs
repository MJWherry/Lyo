namespace Lyo.FileStorage;

/// <summary>
/// Wraps a <see cref="PipeReader"/> stream backed by an in-flight async pipeline; cancels and awaits the pipeline on dispose.
/// </summary>
internal sealed class PipelineFileReadStream : Stream
{
    private readonly Stream _inner;
    private readonly Task _pipelineTask;
    private readonly CancellationTokenSource _cts;
    private bool _disposed;

    public PipelineFileReadStream(Stream inner, Task pipelineTask, CancellationTokenSource cts)
    {
        _inner = inner;
        _pipelineTask = pipelineTask;
        _cts = cts;
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
        ThrowIfDisposed();
        return _inner.Read(buffer, offset, count);
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct)
    {
        ThrowIfDisposed();
        return _inner.ReadAsync(buffer, offset, count, ct);
    }

#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return _inner.ReadAsync(buffer, ct);
    }
#endif

    public override void Flush() { }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_disposed) {
            _disposed = true;
            _cts.Cancel();
            _inner.Dispose();
            try {
                _pipelineTask.GetAwaiter().GetResult();
            }
            catch (OperationCanceledException) {
                // Expected when the reader is closed early.
            }
            catch (AggregateException ex) when (ex.InnerException is OperationCanceledException) {
            }

            _cts.Dispose();
        }

        base.Dispose(disposing);
    }

#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
    public override async ValueTask DisposeAsync()
    {
        if (!_disposed) {
            _disposed = true;
#if NET5_0_OR_GREATER
            await _cts.CancelAsync().ConfigureAwait(false);
#else
            _cts.Cancel();
#endif
            await _inner.DisposeAsync().ConfigureAwait(false);
            try {
                await _pipelineTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException) {
            }

            _cts.Dispose();
        }

        await base.DisposeAsync().ConfigureAwait(false);
    }
#endif

    private void ThrowIfDisposed()
    {
#if NET5_0_OR_GREATER
        ObjectDisposedException.ThrowIf(_disposed, this);
#else
        if (_disposed)
            throw new ObjectDisposedException(nameof(PipelineFileReadStream));
#endif
    }
}
