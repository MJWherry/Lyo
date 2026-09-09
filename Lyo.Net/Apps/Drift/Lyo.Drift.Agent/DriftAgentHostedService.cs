using System.Text.Json;
using System.Threading.Channels;
using Lyo.Api.Client;
using Lyo.Common.Json;
using Lyo.Diff.ObjectGraph;
using Lyo.Drift.Client;
using Lyo.Drift.Models;
using Lyo.Exceptions;
using Lyo.FileSystemWatcher;
using Lyo.FileSystemWatcher.Models;
using Lyo.SystemInformation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Watcher = Lyo.FileSystemWatcher.FileSystemWatcher;
using IoFileSystemWatcher = System.IO.FileSystemWatcher;

namespace Lyo.Drift.Agent;

/// <summary>
/// Registers with the collector, watches configured directories, and posts snapshots off the watcher debounce thread via a bounded channel.
/// </summary>
/// <remarks>
/// Native <see cref="IoFileSystemWatcher" /> events are consumed inside <see cref="Watcher" />. This hosted service must not POST from <c>ScanCompleted</c>
/// or from the system-info timer callback.
/// </remarks>
public sealed class DriftAgentHostedService : BackgroundService
{
    private static readonly JsonSerializerOptions Json = LyoJsonSerializerOptions.Create();
    private readonly DriftAgentOptions _options;
    private readonly IDriftClient _client;
    private readonly IObjectGraphDiffService _objectGraphDiff;
    private readonly ILogger<DriftAgentHostedService> _logger;
    private readonly Channel<IngestWork> _channel;
    private readonly List<Watcher> _watchers = [];
    private readonly Dictionary<string, Guid> _lastSnapshotIdByWatch = new(StringComparer.OrdinalIgnoreCase);
    private Guid? _instanceId;
    private Guid? _lastSystemSnapshotId;

    /// <summary>Managed thread id of the last <c>ScanCompleted</c> handler. Tests use this to prove HTTP did not run on the debounce thread.</summary>
    internal int? LastScanCompletedThreadId { get; private set; }

    /// <summary>True while <see cref="OnScanCompleted" /> is on the stack.</summary>
    internal bool ScanCompletedHandlerRunning { get; private set; }

    /// <summary>Managed thread id of the last system-info timer callback.</summary>
    internal int? LastSystemInfoTimerThreadId { get; private set; }

    /// <summary>True while the system-info timer callback is on the stack.</summary>
    internal bool SystemInfoTimerCallbackRunning { get; private set; }

    /// <summary>Builds an agent over <paramref name="client" />.</summary>
    public DriftAgentHostedService(
        DriftAgentOptions options,
        IDriftClient client,
        IObjectGraphDiffService objectGraphDiff,
        ILogger<DriftAgentHostedService>? logger = null)
    {
        ArgumentHelpers.ThrowIfNull(options);
        ArgumentHelpers.ThrowIfNull(client);
        ArgumentHelpers.ThrowIfNull(objectGraphDiff);
        _options = options;
        _options.Validate();
        _client = client;
        _objectGraphDiff = objectGraphDiff;
        _logger = logger ?? NullLogger<DriftAgentHostedService>.Instance;
        _channel = Channel.CreateBounded<IngestWork>(
            new BoundedChannelOptions(Math.Max(1, _options.IngestChannelCapacity)) {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false,
                AllowSynchronousContinuations = false
            });
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try {
            await RegisterAsync(stoppingToken).ConfigureAwait(false);
            StartWatchers(stoppingToken);
            if (_options.CollectSystemInfo)
                QueueSystemInfo();

            var pump = Task.Run(() => PumpAsync(stoppingToken), stoppingToken);
            var heartbeat = HeartbeatLoopAsync(stoppingToken);
            var systemInfo = SystemInfoLoopAsync(stoppingToken);
            await Task.WhenAll(pump, heartbeat, systemInfo).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        finally {
            foreach (var watcher in _watchers)
                watcher.Dispose();

            _watchers.Clear();
            if (_instanceId is { } id) {
                try {
                    await RetryAsync(() => _client.StopAsync(id, CancellationToken.None), CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception ex) {
                    _logger.LogWarning(ex, "Failed to mark drift instance stopped");
                }
            }
        }
    }

    internal void QueueSystemInfo()
    {
        LastSystemInfoTimerThreadId = Environment.CurrentManagedThreadId;
        SystemInfoTimerCallbackRunning = true;
        try {
            var projection = SystemInfoDriftProjection.From(SystemInfoCollector.Collect(), _options.ResolvedSystemInfoInclude());
            if (!_channel.Writer.TryWrite(IngestWork.ForSystem(projection, DateTime.UtcNow)))
                _logger.LogWarning("Drift ingest queue is full; dropped a system-info snapshot");
        }
        finally {
            SystemInfoTimerCallbackRunning = false;
        }
    }

    private async Task RegisterAsync(CancellationToken ct)
    {
        var key = _options.InstanceKey;
        var result = await RetryAsync(
            () => _client.UpsertInstanceAsync(
                new() {
                    InstanceKey = key,
                    MachineName = Environment.MachineName,
                    ProcessId = Environment.ProcessId,
                    State = DriftInstanceState.Running,
                    WatchesJson = JsonSerializer.Serialize(
                        _options.Watches.Select(w => new { w.Path, w.IncludeSubdirectories, w.IncludePatterns, w.ExcludePatterns }), Json)
                }, ct), ct).ConfigureAwait(false);
        if (result is not { IsSuccess: true, Data: { } data })
            throw new InvalidOperationException("Drift instance upsert failed.");

        _instanceId = data.Id;
    }

    private void StartWatchers(CancellationToken ct)
    {
        foreach (var watch in _options.Watches) {
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(watch.Path);
            var watcher = new Watcher(
                watch.Path, new FileSystemWatcherOptions {
                    IncludeSubdirectories = watch.IncludeSubdirectories,
                    IncludePatterns = watch.IncludePatterns,
                    ExcludePatterns = watch.ExcludePatterns,
                    EnableFileHashing = watch.EnableFileHashing,
                    DebounceTimerDelay = watch.DebounceTimerDelay
                });
            var root = watcher.Path;
            watcher.ScanCompleted += (_, e) => OnScanCompleted(root, e);
            _watchers.Add(watcher);
            var tree = watcher.CurrentSnapshot.ToDto();
            if (!_channel.Writer.TryWrite(IngestWork.ForTree(root, tree, [], DateTime.UtcNow)))
                _logger.LogWarning("Drift ingest queue is full; dropped the initial snapshot for {WatchRoot}", root);
        }

        _ = ct;
    }

    private void OnScanCompleted(string watchRoot, FileSystemScanCompletedEventArgs e)
    {
        LastScanCompletedThreadId = Environment.CurrentManagedThreadId;
        ScanCompletedHandlerRunning = true;
        try {
            var occurred = DateTime.UtcNow;
            var tree = e.Current.ToDto();
            var changes = e.Changes.Select(c => c.ToDto(e.Current.RootPath, occurred)).ToArray();
            if (!_channel.Writer.TryWrite(IngestWork.ForTree(watchRoot, tree, changes, occurred)))
                _logger.LogWarning("Drift ingest queue is full; dropped a scan for {WatchRoot}", watchRoot);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to map ScanCompleted for {WatchRoot}", watchRoot);
        }
        finally {
            ScanCompletedHandlerRunning = false;
        }
    }

    private async Task PumpAsync(CancellationToken ct)
    {
        try {
            await foreach (var work in _channel.Reader.ReadAllAsync(ct).ConfigureAwait(false)) {
                try {
                    if (work.Kind == IngestKind.FileTree)
                        await PostFileTreeAsync(work, ct).ConfigureAwait(false);
                    else
                        await PostSystemInfoAsync(work, ct).ConfigureAwait(false);
                }
                catch (Exception ex) {
                    _logger.LogWarning(ex, "Drift ingest failed for {Kind} {WatchRoot}", work.Kind, work.WatchRoot);
                }
            }
        }
        catch (OperationCanceledException) { }
    }

    private async Task PostFileTreeAsync(IngestWork work, CancellationToken ct)
    {
        if (_instanceId is not { } instanceId || work.Tree is null)
            return;

        var posted = await RetryAsync(
            () => _client.PostSnapshotAsync(
                new() {
                    InstanceId = instanceId,
                    Kind = DriftSnapshotKind.FileTree,
                    WatchRoot = work.WatchRoot,
                    TakenAtUtc = work.TakenAtUtc,
                    TreeJson = JsonSerializer.Serialize(work.Tree, Json)
                }, ct), ct).ConfigureAwait(false);
        if (posted is not { IsSuccess: true, Data: { } snapshot })
            return;

        _lastSnapshotIdByWatch.TryGetValue(work.WatchRoot ?? "", out var previousId);
        if (!snapshot.Deduped)
            _lastSnapshotIdByWatch[work.WatchRoot ?? ""] = snapshot.Id;

        if (_options.PostLiveChanges && work.Changes is { Count: > 0 }) {
            await RetryAsync(
                () => _client.PostChangesAsync(
                    new() {
                        InstanceId = instanceId,
                        WatchRoot = work.WatchRoot,
                        Changes = work.Changes.ToList()
                    }, ct), ct).ConfigureAwait(false);
        }

        if (_options.ComputeDiffsLocally && !snapshot.Deduped && previousId != Guid.Empty && previousId != snapshot.Id && work.Tree is not null) {
            var previous = await RetryAsync(() => _client.GetSnapshotAsync(previousId, ct), ct).ConfigureAwait(false);
            if (previous?.TreeJson is { Length: > 0 }) {
                var oldTree = JsonSerializer.Deserialize<FileSystemSnapshotTreeDto>(previous.TreeJson, Json);
                if (oldTree is not null) {
                    var changes = FileSystemSnapshotDiffer.DetectChanges(oldTree, work.Tree, work.TakenAtUtc, ct);
                    await RetryAsync(
                        () => _client.PostDiffAsync(
                            new() {
                                InstanceId = instanceId,
                                FromSnapshotId = previousId,
                                ToSnapshotId = snapshot.Id,
                                Source = DriftDiffSource.Agent,
                                FileChangesJson = JsonSerializer.Serialize(changes, Json)
                            }, ct), ct).ConfigureAwait(false);
                }
            }
        }
    }

    private async Task PostSystemInfoAsync(IngestWork work, CancellationToken ct)
    {
        if (_instanceId is not { } instanceId || work.Projection is null)
            return;

        var json = JsonSerializer.Serialize(work.Projection, Json);
        var posted = await RetryAsync(
            () => _client.PostSnapshotAsync(
                new() {
                    InstanceId = instanceId,
                    Kind = DriftSnapshotKind.SystemInfo,
                    TakenAtUtc = work.TakenAtUtc,
                    SystemInfoJson = json
                }, ct), ct).ConfigureAwait(false);
        if (posted is not { IsSuccess: true, Data: { } snapshot })
            return;

        var previousId = _lastSystemSnapshotId;
        if (!snapshot.Deduped)
            _lastSystemSnapshotId = snapshot.Id;

        if (_options.ComputeDiffsLocally && !snapshot.Deduped && previousId is { } fromId && fromId != snapshot.Id) {
            var previous = await RetryAsync(() => _client.GetSnapshotAsync(fromId, ct), ct).ConfigureAwait(false);
            if (previous?.SystemInfoJson is { Length: > 0 }) {
                var oldProjection = JsonSerializer.Deserialize<SystemInfoDriftProjection>(previous.SystemInfoJson, Json);
                if (oldProjection is not null) {
                    var diffs = _objectGraphDiff.GetDifferences(oldProjection, work.Projection);
                    var dtos = diffs.Select(d => new ObjectGraphDifferenceDto {
                        Path = d.Path,
                        OldValueJson = d.OldValue is null ? null : JsonSerializer.Serialize(d.OldValue, Json),
                        NewValueJson = d.NewValue is null ? null : JsonSerializer.Serialize(d.NewValue, Json)
                    }).ToArray();
                    await RetryAsync(
                        () => _client.PostDiffAsync(
                            new() {
                                InstanceId = instanceId,
                                FromSnapshotId = fromId,
                                ToSnapshotId = snapshot.Id,
                                Source = DriftDiffSource.Agent,
                                SystemDifferencesJson = JsonSerializer.Serialize(dtos, Json)
                            }, ct), ct).ConfigureAwait(false);
                }
            }
        }
    }

    private async Task HeartbeatLoopAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(_options.HeartbeatInterval);
        try {
            while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false)) {
                if (_instanceId is not { } id)
                    continue;

                try {
                    await _client.HeartbeatAsync(id, new() { LastHeartbeatUtc = DateTime.UtcNow }, ct).ConfigureAwait(false);
                }
                catch (ApiException ex) when (ex.StatusCode == 404) {
                    _logger.LogWarning("Drift instance {InstanceId} was not found — re-upserting {InstanceKey}", id, _options.InstanceKey);
                    await RegisterAsync(ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) {
                    break;
                }
                catch (Exception ex) {
                    _logger.LogWarning(ex, "Drift heartbeat failed");
                }
            }
        }
        catch (OperationCanceledException) { }
    }

    private async Task SystemInfoLoopAsync(CancellationToken ct)
    {
        if (!_options.CollectSystemInfo)
            return;

        using var timer = new PeriodicTimer(_options.SystemInfoInterval);
        try {
            while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
                QueueSystemInfo();
        }
        catch (OperationCanceledException) { }
    }

    private async Task<T> RetryAsync<T>(Func<Task<T>> send, CancellationToken ct)
    {
        Exception? last = null;
        for (var attempt = 0; attempt < 4; attempt++) {
            ct.ThrowIfCancellationRequested();
            try {
                return await send().ConfigureAwait(false);
            }
            catch (ApiException ex) when (ex.StatusCode is 408 or 429 or 502 or 503 or 504) {
                last = ex;
            }
            catch (HttpRequestException ex) {
                last = ex;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt)), ct).ConfigureAwait(false);
        }

        throw last ?? new InvalidOperationException("Drift ingest retry exhausted.");
    }

    private async Task RetryAsync(Func<Task> send, CancellationToken ct)
        => await RetryAsync(async () => {
            await send().ConfigureAwait(false);
            return true;
        }, ct).ConfigureAwait(false);

    private enum IngestKind
    {
        FileTree,
        SystemInfo
    }

    private readonly record struct IngestWork(
        IngestKind Kind,
        string? WatchRoot,
        FileSystemSnapshotTreeDto? Tree,
        IReadOnlyList<FileSystemChangeDto>? Changes,
        SystemInfoDriftProjection? Projection,
        DateTime TakenAtUtc)
    {
        public static IngestWork ForTree(string watchRoot, FileSystemSnapshotTreeDto tree, IReadOnlyList<FileSystemChangeDto> changes, DateTime takenAtUtc)
            => new(IngestKind.FileTree, watchRoot, tree, changes, null, takenAtUtc);

        public static IngestWork ForSystem(SystemInfoDriftProjection projection, DateTime takenAtUtc)
            => new(IngestKind.SystemInfo, null, null, null, projection, takenAtUtc);
    }
}
