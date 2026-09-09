using System.Threading.Channels;
using Lyo.Exceptions;
using Lyo.FileSystemWatcher.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.FileSystemWatcher.Postgres;

/// <summary>
/// Copies DTOs on <see cref="FileSystemWatcher.ScanCompleted" /> (timer thread) and writes them through <see cref="IFileSystemWatcherStore" /> from a background loop.
/// </summary>
public sealed class FileSystemWatcherPersister : IAsyncDisposable
{
    private readonly IFileSystemWatcherStore _store;
    private readonly ILogger<FileSystemWatcherPersister> _logger;
    private readonly Channel<PersistWork> _channel;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _pump;
    private FileSystemWatcher? _watcher;
    private Guid _watchId;

    /// <summary>Builds a persister. Call <see cref="Attach" /> then drain with the background pump.</summary>
    public FileSystemWatcherPersister(IFileSystemWatcherStore store, ILogger<FileSystemWatcherPersister>? logger = null, int capacity = 32)
    {
        ArgumentHelpers.ThrowIfNull(store);
        _store = store;
        _logger = logger ?? NullLogger<FileSystemWatcherPersister>.Instance;
        _channel = Channel.CreateBounded<PersistWork>(
            new BoundedChannelOptions(capacity) {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false
            });
        _pump = Task.Run(() => PumpAsync(_cts.Token));
    }

    /// <summary>Subscribes to <paramref name="watcher" />. Mapping runs on the scan thread; database writes run on the pump.</summary>
    public void Attach(FileSystemWatcher watcher, Guid watchId)
    {
        ArgumentHelpers.ThrowIfNull(watcher);
        if (_watcher != null)
            _watcher.ScanCompleted -= OnScanCompleted;

        _watcher = watcher;
        _watchId = watchId;
        watcher.ScanCompleted += OnScanCompleted;
    }

    /// <summary>Waits until queued work is written (or the channel is empty).</summary>
    public async Task FlushAsync(CancellationToken ct = default)
    {
        while (!_channel.Reader.Completion.IsCompleted && _channel.Reader.Count > 0)
            await Task.Delay(15, ct).ConfigureAwait(false);

        await Task.Delay(50, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_watcher != null)
            _watcher.ScanCompleted -= OnScanCompleted;

        _channel.Writer.TryComplete();
        _cts.Cancel();
        try {
            await _pump.ConfigureAwait(false);
        }
        catch (OperationCanceledException) { }

        _cts.Dispose();
    }

    private void OnScanCompleted(object? sender, FileSystemScanCompletedEventArgs e)
    {
        try {
            var occurred = DateTime.UtcNow;
            var tree = e.Current.ToDto();
            var changes = e.Changes.Select(c => c.ToDto(e.Current.RootPath, occurred)).ToArray();
            if (!_channel.Writer.TryWrite(new(tree, changes, occurred)))
                _logger.LogWarning("FileSystemWatcher persist queue is full; dropped a scan for watch {WatchId}", _watchId);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to map ScanCompleted for watch {WatchId}", _watchId);
        }
    }

    private async Task PumpAsync(CancellationToken ct)
    {
        try {
            await foreach (var work in _channel.Reader.ReadAllAsync(ct).ConfigureAwait(false)) {
                try {
                    var snapshotId = await _store.SaveSnapshotAsync(_watchId, work.Tree, work.TakenAtUtc, ct).ConfigureAwait(false);
                    await _store.SaveChangesAsync(_watchId, snapshotId, work.Changes, ct).ConfigureAwait(false);
                }
                catch (Exception ex) {
                    _logger.LogError(ex, "Failed to persist scan for watch {WatchId}", _watchId);
                }
            }
        }
        catch (OperationCanceledException) { }
    }

    private readonly record struct PersistWork(FileSystemSnapshotTreeDto Tree, IReadOnlyList<FileSystemChangeDto> Changes, DateTime TakenAtUtc);
}
