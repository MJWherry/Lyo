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
using SemaphoreConstants = Lyo.Lock.Constants;

namespace Lyo.TestGateway.Components.TestGateway;

public partial class SemaphoreWorkbench
{
    private readonly List<DemoEvent> _events = [];
    private readonly List<IPermitHandle> _heldPermits = [];
    private readonly object _eventsLock = new();
    private bool _busy;
    private long _acquireFailure;
    private long _acquireSuccess;
    private int _acquireDurationCount;
    private double _acquireDurationAverageMs;
    private int _executeDurationCount;
    private double _executeDurationAverageMs;
    private int _holdMilliseconds = 750;
    private string _key = "workbench:local-semaphore";
    private DateTime _lastMetricsRefresh;
    private DemoSummary? _lastRun;
    private int _maxConcurrency = 3;
    private int _releaseDurationCount;
    private double _releaseDurationAverageMs;
    private int _timeoutMilliseconds = 2500;
    private int _workerCount = 8;

    private int HeldPermitCount => _heldPermits.Count;

    private MetricsService? MetricsStore => Metrics as MetricsService;

    private List<DemoEvent> VisibleEvents {
        get {
            lock (_eventsLock)
                return _events.ToList();
        }
    }

    protected override Task OnInitializedAsync() => RefreshMetricsAsync();

    public ValueTask DisposeAsync() => new(ReleaseAllHeldPermitsCoreAsync());

    private async Task AcquirePermitAsync()
    {
        _busy = true;
        try {
            var handle = await SemaphoreService.AcquireAsync(_key, _maxConcurrency, TimeSpan.FromMilliseconds(_timeoutMilliseconds));
            if (handle == null) {
                AppendEvent("Acquire", $"UI failed to acquire a permit for '{_key}' within {_timeoutMilliseconds} ms.");
                SetStatus("Permit acquisition timed out.", Severity.Warning);
                return;
            }

            _heldPermits.Add(handle);
            AppendEvent("Acquire", $"UI acquired permit {_heldPermits.Count}/{_maxConcurrency} for '{_key}'.");
            SetStatus("Permit acquired.", Severity.Success);
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

    private async Task AcquireUntilFullAsync()
    {
        if (HeldPermitCount >= _maxConcurrency) {
            SetStatus("All permits are already held by the UI.", Severity.Warning);
            return;
        }

        _busy = true;
        try {
            while (HeldPermitCount < _maxConcurrency) {
                var handle = await SemaphoreService.AcquireAsync(_key, _maxConcurrency, TimeSpan.FromMilliseconds(_timeoutMilliseconds));
                if (handle == null) {
                    AppendEvent("Acquire", $"Timed out before filling all permits for '{_key}'.");
                    SetStatus("Could not acquire every permit before timeout.", Severity.Warning);
                    await RefreshMetricsAsync();
                    return;
                }

                _heldPermits.Add(handle);
                AppendEvent("Acquire", $"UI acquired permit {_heldPermits.Count}/{_maxConcurrency} for '{_key}'.");
            }

            SetStatus("All permits are now held by the UI.", Severity.Success);
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

    private async Task ReleasePermitsAsync()
    {
        if (HeldPermitCount == 0) {
            SetStatus("There are no held permits to release.", Severity.Warning);
            return;
        }

        _busy = true;
        try {
            await ReleaseAllHeldPermitsCoreAsync();
            AppendEvent("Release", $"UI released all permits for '{_key}'.");
            SetStatus("Held permits released.", Severity.Success);
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
        if (HeldPermitCount < _maxConcurrency) {
            SetStatus("Acquire all permits first so the semaphore is saturated.", Severity.Warning);
            return;
        }

        _busy = true;
        try {
            var stopwatch = Stopwatch.StartNew();
            var competingHandle = await SemaphoreService.AcquireAsync(_key, _maxConcurrency, TimeSpan.FromMilliseconds(_timeoutMilliseconds));
            stopwatch.Stop();
            if (competingHandle == null) {
                AppendEvent("Competing Acquire", $"Additional permit timed out after {stopwatch.Elapsed.TotalMilliseconds:0} ms while the UI held all permits for '{_key}'.");
                SetStatus("Competing acquire timed out as expected.", Severity.Success);
                await RefreshMetricsAsync();
                return;
            }

            await competingHandle.ReleaseAsync();
            AppendEvent("Competing Acquire", $"Additional permit unexpectedly succeeded after {stopwatch.Elapsed.TotalMilliseconds:0} ms.");
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

    private Task RunSemaphoreDemoAsync() => RunDemoAsync(true);

    private Task RunUnlockedDemoAsync() => RunDemoAsync(false);

    private async Task RunDemoAsync(bool useSemaphore)
    {
        if (HeldPermitCount != 0) {
            SetStatus("Release the manually-held permits before running the burst demo.", Severity.Warning);
            return;
        }

        _busy = true;
        try {
            var startGate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var stopwatch = Stopwatch.StartNew();
            var activeWorkers = 0;
            var completedWorkers = 0;
            var timedOutWorkers = 0;
            var maxConcurrent = 0;
            var startedAt = DateTime.UtcNow;
            AppendEvent("Demo", $"Starting {_workerCount} workers {(useSemaphore ? "with" : "without")} semaphore protection for '{_key}' (max={_maxConcurrency}).");
            var tasks = Enumerable.Range(1, _workerCount)
                .Select(async workerId => {
                    await startGate.Task;
                    if (useSemaphore) {
                        try {
                            await SemaphoreService.ExecuteAsync(
                                _key, _maxConcurrency, async ct => {
                                    var current = Interlocked.Increment(ref activeWorkers);
                                    UpdateMax(ref maxConcurrent, current);
                                    AppendEvent("Enter", $"Worker {workerId} entered the semaphore-protected section. concurrent={current}");
                                    try {
                                        await Task.Delay(_holdMilliseconds, ct);
                                    }
                                    finally {
                                        var remaining = Interlocked.Decrement(ref activeWorkers);
                                        AppendEvent("Exit", $"Worker {workerId} left the semaphore-protected section. remaining={remaining}");
                                    }
                                }, TimeSpan.FromMilliseconds(_timeoutMilliseconds));

                            Interlocked.Increment(ref completedWorkers);
                            AppendEvent("Complete", $"Worker {workerId} completed under the semaphore.");
                        }
                        catch (TimeoutException) {
                            Interlocked.Increment(ref timedOutWorkers);
                            AppendEvent("Timeout", $"Worker {workerId} timed out waiting for a permit on '{_key}'.");
                        }
                    }
                    else {
                        var current = Interlocked.Increment(ref activeWorkers);
                        UpdateMax(ref maxConcurrent, current);
                        AppendEvent("Enter", $"Worker {workerId} entered without a semaphore. concurrent={current}");
                        try {
                            await Task.Delay(_holdMilliseconds);
                        }
                        finally {
                            var remaining = Interlocked.Decrement(ref activeWorkers);
                            AppendEvent("Exit", $"Worker {workerId} left the unlocked section. remaining={remaining}");
                        }

                        Interlocked.Increment(ref completedWorkers);
                        AppendEvent("Complete", $"Worker {workerId} completed without a semaphore.");
                    }
                })
                .ToList();

            startGate.TrySetResult(true);
            await Task.WhenAll(tasks);
            stopwatch.Stop();
            var summary = useSemaphore ? maxConcurrent <= _maxConcurrency && timedOutWorkers == 0 ? $"Workers respected the semaphore limit; no more than {_maxConcurrency} ran at once." : "The semaphore run exceeded the expected limit or some workers timed out." : maxConcurrent > _maxConcurrency ? "Unlocked workers exceeded the permit limit, showing the overlap the semaphore avoids." : "Unlocked workers happened to stay within the permit limit on this run.";
            _lastRun = new(completedWorkers, _workerCount, timedOutWorkers, maxConcurrent, _maxConcurrency, stopwatch.Elapsed, startedAt, summary, useSemaphore);
            SetStatus(summary, useSemaphore ? maxConcurrent <= _maxConcurrency && timedOutWorkers == 0 ? Severity.Success : Severity.Warning : Severity.Info);
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

        var tags = new[] { (SemaphoreConstants.SemaphoreMetrics.Tags.Key, _key) };
        _acquireSuccess = MetricsStore.GetCounterValue(SemaphoreConstants.SemaphoreMetrics.AcquireSuccess, tags);
        _acquireFailure = MetricsStore.GetCounterValue(SemaphoreConstants.SemaphoreMetrics.AcquireFailure, tags);
        var acquireDuration = MetricsStore.GetHistogram(SemaphoreConstants.SemaphoreMetrics.AcquireDuration, tags);
        _acquireDurationCount = acquireDuration?.Count ?? 0;
        _acquireDurationAverageMs = acquireDuration?.Average ?? 0;
        var releaseDuration = MetricsStore.GetHistogram(SemaphoreConstants.SemaphoreMetrics.ReleaseDuration, tags);
        _releaseDurationCount = releaseDuration?.Count ?? 0;
        _releaseDurationAverageMs = releaseDuration?.Average ?? 0;
        var executeDuration = MetricsStore.GetHistogram(SemaphoreConstants.SemaphoreMetrics.ExecuteDuration, tags);
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

    private async Task ReleaseAllHeldPermitsCoreAsync()
    {
        foreach (var permit in _heldPermits.ToList())
            await permit.ReleaseAsync();

        _heldPermits.Clear();
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

    private sealed record DemoSummary(int Completed, int WorkerCount, int TimedOut, int MaxConcurrent, int MaxAllowed, TimeSpan Elapsed, DateTime StartedAt, string Summary, bool UsedSemaphore);
}
