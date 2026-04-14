using System.Collections.Concurrent;

namespace Lyo.Scheduler;

/// <summary>In-memory implementation of ISchedulerStateStore. State is lost on process restart.</summary>
public sealed class InMemorySchedulerStateStore : ISchedulerStateStore
{
    private readonly ConcurrentDictionary<string, DateTime> _lastExecutedSlot = new();
    private readonly ConcurrentDictionary<string, DateTime> _lastRun = new();

    /// <inheritdoc />
    public ValueTask<DateTime?> GetLastRunAsync(string scheduleId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return new(_lastRun.TryGetValue(scheduleId, out var dt) ? dt : null);
    }

    /// <inheritdoc />
    public ValueTask SetLastRunAsync(string scheduleId, DateTime timestamp, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        _lastRun[scheduleId] = timestamp;
        return default;
    }

    /// <inheritdoc />
    public ValueTask<DateTime?> GetLastExecutedSlotAsync(string scheduleId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return new(_lastExecutedSlot.TryGetValue(scheduleId, out var dt) ? dt : null);
    }

    /// <inheritdoc />
    public ValueTask SetLastExecutedSlotAsync(string scheduleId, DateTime slotTimestamp, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        _lastExecutedSlot[scheduleId] = slotTimestamp;
        return default;
    }
}