using System.Collections.Concurrent;
using System.Diagnostics;
using Lyo.Exceptions;
using Lyo.Metrics;
using Lyo.Schedule.Models;
using Lyo.Scheduler.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Scheduler;

/// <summary>In-process scheduler that executes actions at scheduled times with logging, metrics, and optional state persistence.</summary>
public sealed class SchedulerService : ISchedulerService, IDisposable
{
    private const string MetricPrefix = "lyo.scheduler";
    private static readonly (string, string)[] DefaultTags = [("component", "scheduler")];
    private readonly ILogger<SchedulerService> _logger;
    private readonly IMetrics _metrics;

    private readonly SchedulerOptions _options;
    private readonly ConcurrentDictionary<string, (ScheduleDefinition Definition, string? Name, Func<CancellationToken, Task> Action)> _schedules = new();
    private readonly ISchedulerStateStore _stateStore;
    private CancellationTokenSource? _cts;
    private bool _disposed;
    private Task? _runTask;

    /// <summary>Creates a new scheduler service.</summary>
    public SchedulerService(SchedulerOptions? options = null, ILogger<SchedulerService>? logger = null, IMetrics? metrics = null, ISchedulerStateStore? stateStore = null)
    {
        _options = options ?? new SchedulerOptions();
        _logger = logger ?? NullLogger<SchedulerService>.Instance;
        _metrics = _options.EnableMetrics && metrics != null ? metrics : NullMetrics.Instance;
        _stateStore = stateStore ?? new InMemorySchedulerStateStore();
    }

    /// <summary>Disposes the scheduler and stops it if running.</summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _cts?.Cancel();
        _cts?.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public bool IsRunning => _runTask != null && !_runTask.IsCompleted;

    /// <inheritdoc />
    public void AddSchedule(string id, string? name, ScheduleDefinition definition, Func<CancellationToken, Task> action)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(id, nameof(id));
        ArgumentHelpers.ThrowIfNull(definition, nameof(definition));
        ArgumentHelpers.ThrowIfNull(action, nameof(action));
        definition.Validate();
        _schedules[id] = (definition, name, action);
        _logger.LogInformation("Added schedule {ScheduleId} ({ScheduleName})", id, name ?? id);
        _metrics.IncrementCounter($"{MetricPrefix}.schedules.added", 1, new[] { ("schedule_id", id) }.Concat(DefaultTags));
    }

    /// <inheritdoc />
    public bool RemoveSchedule(string scheduleId)
    {
        if (string.IsNullOrWhiteSpace(scheduleId))
            return false;

        var removed = _schedules.TryRemove(scheduleId, out var _);
        if (removed) {
            _logger.LogInformation("Removed schedule {ScheduleId}", scheduleId);
            _metrics.IncrementCounter($"{MetricPrefix}.schedules.removed", 1, new[] { ("schedule_id", scheduleId) }.Concat(DefaultTags));
        }

        return removed;
    }

    /// <inheritdoc />
    public IReadOnlyCollection<ScheduleInfo> GetSchedules() => _schedules.Select(kv => new ScheduleInfo(kv.Key, kv.Value.Name, kv.Value.Definition)).ToList().AsReadOnly();

    /// <inheritdoc />
    public IReadOnlyList<ScheduleWithNextRun> GetSchedulesOrderedByNextRun(DateTime? asOf = null)
    {
        var reference = asOf ?? DateTime.UtcNow;
        return _schedules.Select(kv => {
                var info = new ScheduleInfo(kv.Key, kv.Value.Name, kv.Value.Definition);
                var nextRun = ScheduleCalculator.GetNextRun(kv.Value.Definition, reference);
                return new ScheduleWithNextRun(info, nextRun);
            })
            .OrderBy(x => x.NextRun.HasValue ? 0 : 1)
            .ThenBy(x => x.NextRun ?? DateTime.MaxValue)
            .ToList()
            .AsReadOnly();
    }

    /// <inheritdoc />
    public IReadOnlyList<ScheduleRun> GetUpcomingRuns(DateTime? asOf = null, int maxRuns = 100)
    {
        var reference = asOf ?? DateTime.UtcNow;
        var perSchedule = Math.Max(1, maxRuns / Math.Max(1, _schedules.Count));
        return _schedules.SelectMany(kv => {
                var info = new ScheduleInfo(kv.Key, kv.Value.Name, kv.Value.Definition);
                return ScheduleCalculator.GetNextRuns(kv.Value.Definition, reference, perSchedule).Select(runAt => new ScheduleRun(info, runAt));
            })
            .OrderBy(x => x.RunAt)
            .Take(maxRuns)
            .ToList()
            .AsReadOnly();
    }

    /// <inheritdoc />
    public ScheduleInfo? GetSchedule(string scheduleId)
    {
        if (string.IsNullOrWhiteSpace(scheduleId) || !_schedules.TryGetValue(scheduleId, out var entry))
            return null;

        return new(scheduleId, entry.Name, entry.Definition);
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken ct = default)
    {
        if (IsRunning) {
            _logger.LogDebug("Scheduler already running");
            return Task.CompletedTask;
        }

        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _runTask = RunAsync(_cts.Token);
        _logger.LogInformation("Scheduler started");
        _metrics.IncrementCounter($"{MetricPrefix}.started", 1, DefaultTags);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken ct = default)
    {
        if (!IsRunning) {
            _logger.LogDebug("Scheduler not running");
            return;
        }

        _cts?.Cancel();
        if (_runTask != null)
            await _runTask.ConfigureAwait(false);

        _cts?.Dispose();
        _cts = null;
        _runTask = null;
        _logger.LogInformation("Scheduler stopped");
        _metrics.IncrementCounter($"{MetricPrefix}.stopped", 1, DefaultTags);
    }

    private async Task RunAsync(CancellationToken ct)
    {
        _logger.LogDebug("Scheduler loop started, check interval {Interval}ms", _options.CheckIntervalMs);
        while (!ct.IsCancellationRequested) {
            try {
                await CheckAndExecuteDueSchedulesAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) {
                break;
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Error in scheduler loop");
                _metrics.RecordError($"{MetricPrefix}.loop_error", ex, DefaultTags);
            }

            try {
                await Task.Delay(_options.CheckIntervalMs, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) {
                break;
            }
        }

        _logger.LogDebug("Scheduler loop ended");
    }

    private async Task CheckAndExecuteDueSchedulesAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        foreach (var kv in _schedules.ToList()) {
            var (definition, _, _) = kv.Value;
            if (!definition.Enabled || ct.IsCancellationRequested)
                continue;

            var nextRun = ScheduleCalculator.GetNextRun(definition, now);
            if (!nextRun.HasValue)
                continue;

            var dueThreshold = now.AddMilliseconds(_options.CheckIntervalMs + 1000);
            if (nextRun.Value > dueThreshold)
                continue;

            var lastSlot = await _stateStore.GetLastExecutedSlotAsync(kv.Key, ct).ConfigureAwait(false);
            if (lastSlot.HasValue && Math.Abs((lastSlot.Value - nextRun.Value).TotalSeconds) < 1)
                continue;

            await ExecuteScheduleAsync(kv.Key, kv.Value, nextRun.Value, ct).ConfigureAwait(false);
        }
    }

    private async Task ExecuteScheduleAsync(
        string scheduleId,
        (ScheduleDefinition Definition, string? Name, Func<CancellationToken, Task> Action) entry,
        DateTime scheduledTime,
        CancellationToken ct)
    {
        await _stateStore.SetLastExecutedSlotAsync(scheduleId, scheduledTime, ct).ConfigureAwait(false);
        _logger.LogInformation("Executing schedule {ScheduleId} ({ScheduleName})", scheduleId, entry.Name ?? scheduleId);
        var tags = DefaultTags.ToList();
        tags.Add(("schedule_id", scheduleId));
        var tagsArray = tags.ToArray();
        _metrics.IncrementCounter($"{MetricPrefix}.executions.started", 1, tagsArray);
        var stopwatch = Stopwatch.StartNew();
        var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        if (_options.ActionTimeout.HasValue)
            cts.CancelAfter(_options.ActionTimeout.Value);

        async Task ExecuteAndRecordAsync()
        {
            try {
                await entry.Action(cts.Token).ConfigureAwait(false);
                await _stateStore.SetLastRunAsync(scheduleId, DateTime.UtcNow, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested && !ct.IsCancellationRequested) {
                _logger.LogWarning("Schedule {ScheduleId} timed out after {Timeout}", scheduleId, _options.ActionTimeout);
                _metrics.IncrementCounter($"{MetricPrefix}.executions.timeout", 1, tagsArray);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Schedule {ScheduleId} execution failed", scheduleId);
                _metrics.RecordError($"{MetricPrefix}.executions.error", ex, tagsArray);
                if (!_options.RunInBackground)
                    throw;
            }
            finally {
                stopwatch.Stop();
                _metrics.RecordTiming($"{MetricPrefix}.executions.duration", stopwatch.Elapsed, tagsArray);
                _metrics.IncrementCounter($"{MetricPrefix}.executions.completed", 1, tagsArray);
                cts.Dispose();
            }
        }

        if (_options.RunInBackground)
            _ = Task.Run(() => ExecuteAndRecordAsync(), ct);
        else
            await ExecuteAndRecordAsync().ConfigureAwait(false);
    }
}