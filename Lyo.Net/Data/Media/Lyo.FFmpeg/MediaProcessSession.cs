using System.IO.Pipelines;
using Lyo.Media.Models;
using Lyo.Result;

namespace Lyo.FFmpeg;

internal sealed class MediaProcessSession : IMediaProcessSession
{
    private readonly CancellationTokenSource _cts;
    private readonly PipeWriter _writer;
    private int _disposed;

    public MediaProcessSession(Stream standardOutput, Task<Result<bool>> completion, PipeWriter writer, CancellationTokenSource cts)
    {
        StandardOutput = standardOutput;
        Completion = completion;
        _writer = writer;
        _cts = cts;
    }

    /// <inheritdoc />
    public Stream StandardOutput { get; }

    /// <inheritdoc />
    public Task<Result<bool>> Completion { get; }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        try {
            _cts.Cancel();
        }
        catch (ObjectDisposedException) {
        }

        try {
            await _writer.CompleteAsync().ConfigureAwait(false);
        }
        catch {
        }

        await StandardOutput.DisposeAsync().ConfigureAwait(false);
        _cts.Dispose();
    }
}
