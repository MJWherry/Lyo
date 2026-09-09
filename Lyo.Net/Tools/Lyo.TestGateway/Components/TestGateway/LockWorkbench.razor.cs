using System.Diagnostics;
using System.Net.Http.Json;
using Lyo.Api.Client;
using Lyo.Images;
using Lyo.Images.Models;
using Lyo.Lock.Abstractions;
using Lyo.Metrics;
using Lyo.TestGateway;
using Lyo.TestGateway.Components;
using Lyo.TestGateway.Models;
using Lyo.Web.Components;
using Lyo.Web.Primitives;
using Lyo.Web.Components.DataGrid;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.JSInterop;
using MudBlazor;
using static Microsoft.AspNetCore.Components.Web.RenderMode;
using LockConstants = Lyo.Lock.Constants;

namespace Lyo.TestGateway.Components.TestGateway;

public partial class LockWorkbench
{
    private readonly List<DemoEvent> _events = [];
    private readonly object _eventsLock = new();
    private bool _busy;
    private long _acquireFailure;
    private long _acquireSuccess;
    private int _acquireDurationCount;
    private double _acquireDurationAverageMs;
    private int _executeDurationCount;
    private double _executeDurationAverageMs;
    private ILockHandle? _heldLock;
    private int _holdMilliseconds = 750;
    private DateTime _lastMetricsRefresh;
    private DemoSummary? _lastRun;
    private string _lockKey = "workbench:local-lock";
    private int _releaseDurationCount;
    private double _releaseDurationAverageMs;
    private int _timeoutMilliseconds = 2500;
    private int _workerCount = 6;

    private MetricsService? MetricsStore => Metrics as MetricsService;

    private List<DemoEvent> VisibleEvents {
        get {
            lock (_eventsLock)
                return _events.ToList();
        }
    }

    protected override Task OnInitializedAsync() => RefreshMetricsAsync();

    public async ValueTask DisposeAsync()
    {
        if (_heldLock == null)
            return;

        await _heldLock.ReleaseAsync();
        _heldLock = null;
    }

    private async Task AcquireHeldLockAsync()
    {
        if (_heldLock != null) {
            SetStatus("The UI already holds this lock.", Severity.Warning);
            return;
        }

        _busy = true;
        try {
            var handle = await LockService.AcquireAsync(_lockKey, TimeSpan.FromMilliseconds(_timeoutMilliseconds));
            if (handle == null) {
                AppendEvent("Acquire", $"UI failed to acquire '{_lockKey}' within {_timeoutMilliseconds} ms.");
                SetStatus("Lock acquisition timed out.", Severity.Warning);
                return;
            }

            _heldLock = handle;
            AppendEvent("Acquire", $"UI acquired '{_lockKey}' and is holding it.");
            SetStatus("Lock acquired. You can now test a competing acquire.", Severity.Success);
            await RefreshMetricsAsync();
        }
        catch (Exception ex) {
            AppendEvent("Acquire Error", ex.Message);
            SetStatus(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }

    private async Task ReleaseHeldLockAsync()
    {
        if (_heldLock == null) {
            SetStatus("There is no held lock to release.", Severity.Warning);
            return;
        }

        _busy = true;
        try {
            await _heldLock.ReleaseAsync();
            _heldLock = null;
            AppendEvent("Release", $"UI released '{_lockKey}'.");
            SetStatus("Held lock released.", Severity.Success);
            await RefreshMetricsAsync();
        }
        catch (Exception ex) {
            AppendEvent("Release Error", ex.Message);
            SetStatus(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }

    private async Task TryCompetingAcquireAsync()
    {
        if (_heldLock == null) {
            SetStatus("Acquire and hold the lock first.", Severity.Warning);
            return;
        }

        _busy = true;
        try {
            var stopwatch = Stopwatch.StartNew();
            var competingHandle = await LockService.AcquireAsync(_lockKey, TimeSpan.FromMilliseconds(_timeoutMilliseconds));
            stopwatch.Stop();
            if (competingHandle == null) {
                AppendEvent("Competing Acquire", $"Second acquire attempt timed out after {stopwatch.Elapsed.TotalMilliseconds:0} ms while the UI held '{_lockKey}'.");
                SetStatus("Competing acquire timed out as expected.", Severity.Success);
                await RefreshMetricsAsync();
                return;
            }

            await competingHandle.ReleaseAsync();
            AppendEvent("Competing Acquire", $"Second acquire unexpectedly succeeded after {stopwatch.Elapsed.TotalMilliseconds:0} ms.");
            SetStatus("Competing acquire succeeded unexpectedly.", Severity.Warning);
            await RefreshMetricsAsync();
        }
        catch (Exception ex) {
            AppendEvent("Competing Acquire Error", ex.Message);
            SetStatus(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }

    private Task RunLockedDemoAsync() => RunDemoAsync(true);

    private Task RunUnlockedDemoAsync() => RunDemoAsync(false);

    private async Task RunDemoAsync(bool useLock)
    {
        if (_heldLock != null) {
            SetStatus("Release the manually-held lock before running the burst demo.", Severity.Warning);
            return;
        }

        _busy = true;
        try {
            var startedAt = DateTime.UtcNow;
            var stopwatch = Stopwatch.StartNew();
            var startGate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var activeWorkers = 0;
            var completedWorkers = 0;
            var timedOutWorkers = 0;
            var maxConcurrent = 0;
            AppendEvent("Demo", $"Starting {_workerCount} workers {(useLock ? "with" : "without")} lock protection for '{_lockKey}'.");
            var tasks = Enumerable.Range(1, _workerCount)
                .Select(async workerId => {
                    await startGate.Task;
                    if (useLock) {
                        try {
                            await LockService.ExecuteWithLockAsync(
                                _lockKey, async ct => {
                                    var current = Interlocked.Increment(ref activeWorkers);
                                    UpdateMax(ref maxConcurrent, current);
                                    AppendEvent("Enter", $"Worker {workerId} entered the critical section. concurrent={current}");
                                    try {
                                        await Task.Delay(_holdMilliseconds, ct);
                                    }
                                    finally {
                                        var remaining = Interlocked.Decrement(ref activeWorkers);
                                        AppendEvent("Exit", $"Worker {workerId} left the critical section. remaining={remaining}");
                                    }
                                }, TimeSpan.FromMilliseconds(_timeoutMilliseconds));

                            Interlocked.Increment(ref completedWorkers);
                            AppendEvent("Complete", $"Worker {workerId} completed under lock.");
                        }
                        catch (TimeoutException) {
                            Interlocked.Increment(ref timedOutWorkers);
                            AppendEvent("Timeout", $"Worker {workerId} timed out waiting for '{_lockKey}'.");
                        }
                    }
                    else {
                        var current = Interlocked.Increment(ref activeWorkers);
                        UpdateMax(ref maxConcurrent, current);
                        AppendEvent("Enter", $"Worker {workerId} entered without a lock. concurrent={current}");
                        try {
                            await Task.Delay(_holdMilliseconds);
                        }
                        finally {
                            var remaining = Interlocked.Decrement(ref activeWorkers);
                            AppendEvent("Exit", $"Worker {workerId} left the unlocked section. remaining={remaining}");
                        }

                        Interlocked.Increment(ref completedWorkers);
                        AppendEvent("Complete", $"Worker {workerId} completed without a lock.");
                    }
                })
                .ToList();

            startGate.TrySetResult(true);
            await Task.WhenAll(tasks);
            stopwatch.Stop();
            var summary = useLock ? maxConcurrent == 1 && timedOutWorkers == 0 ? "Workers were serialized correctly; only one entered at a time." : "The locked run did not stay fully serialized, or some workers timed out." : maxConcurrent > 1 ? "Unlocked workers overlapped, showing the race you avoid with the lock service." : "Unlocked workers happened to avoid overlap on this run.";
            _lastRun = new() {
                Completed = completedWorkers,
                Elapsed = stopwatch.Elapsed,
                MaxConcurrent = maxConcurrent,
                StartedAt = startedAt,
                Summary = summary,
                TimedOut = timedOutWorkers,
                UsedLock = useLock,
                WorkerCount = _workerCount
            };

            SetStatus(summary, useLock ? maxConcurrent == 1 && timedOutWorkers == 0 ? Severity.Success : Severity.Warning : Severity.Info);
            await RefreshMetricsAsync();
        }
        catch (Exception ex) {
            AppendEvent("Demo Error", ex.Message);
            SetStatus(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }

    private Task RefreshMetricsAsync()
    {
        if (MetricsStore == null)
            return Task.CompletedTask;

        var tags = new[] { (LockConstants.Metrics.Tags.Key, _lockKey) };
        _acquireSuccess = MetricsStore.GetCounterValue(LockConstants.Metrics.AcquireSuccess, tags);
        _acquireFailure = MetricsStore.GetCounterValue(LockConstants.Metrics.AcquireFailure, tags);
        var acquireDuration = MetricsStore.GetHistogram(LockConstants.Metrics.AcquireDuration, tags);
        _acquireDurationCount = acquireDuration?.Count ?? 0;
        _acquireDurationAverageMs = acquireDuration?.Average ?? 0;
        var releaseDuration = MetricsStore.GetHistogram(LockConstants.Metrics.ReleaseDuration, tags);
        _releaseDurationCount = releaseDuration?.Count ?? 0;
        _releaseDurationAverageMs = releaseDuration?.Average ?? 0;
        var executeDuration = MetricsStore.GetHistogram(LockConstants.Metrics.ExecuteDuration, tags);
        _executeDurationCount = executeDuration?.Count ?? 0;
        _executeDurationAverageMs = executeDuration?.Average ?? 0;
        _lastMetricsRefresh = DateTime.UtcNow;
        return Task.CompletedTask;
    }

    private void ClearEvents()
    {
        lock (_eventsLock)
            _events.Clear();

        SetStatus("Event log cleared.", Severity.Info);
    }

    private void AppendEvent(string stage, string message)
    {
        lock (_eventsLock) {
            _events.Insert(0, new(DateTime.UtcNow, stage, message));
            if (_events.Count > 40)
                _events.RemoveRange(40, _events.Count - 40);
        }
    }

    private static void UpdateMax(ref int target, int candidate)
    {
        int snapshot;
        do {
            snapshot = target;
            if (candidate <= snapshot)
                return;
        } while (Interlocked.CompareExchange(ref target, candidate, snapshot) != snapshot);
    }

    private sealed record DemoEvent(DateTime Timestamp, string Stage, string Message);

    private sealed class DemoSummary
    {
        public int Completed { get; init; }

        public TimeSpan Elapsed { get; init; }

        public int MaxConcurrent { get; init; }

        public DateTime StartedAt { get; init; }

        public required string Summary { get; init; }

        public int TimedOut { get; init; }

        public required bool UsedLock { get; init; }

        public int WorkerCount { get; init; }
    }
}
