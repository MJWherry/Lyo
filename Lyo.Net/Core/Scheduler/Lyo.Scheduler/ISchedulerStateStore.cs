namespace Lyo.Scheduler;

/// <summary>Persists last-execution state for schedules. Used to skip duplicate runs and to feed metrics. Back with CacheService when state must survive restarts.</summary>
public interface ISchedulerStateStore
{
    /// <summary>Returns the last execution timestamp for a schedule, if one exists.</summary>
    ValueTask<DateTime?> GetLastRunAsync(string scheduleId, CancellationToken ct = default);

    /// <summary>Records the last execution timestamp for a schedule.</summary>
    ValueTask SetLastRunAsync(string scheduleId, DateTime timestamp, CancellationToken ct = default);

    /// <summary>Returns the last scheduled slot we actually ran, so the same slot is not executed twice.</summary>
    ValueTask<DateTime?> GetLastExecutedSlotAsync(string scheduleId, CancellationToken ct = default);

    /// <summary>Records the last executed slot timestamp.</summary>
    ValueTask SetLastExecutedSlotAsync(string scheduleId, DateTime slotTimestamp, CancellationToken ct = default);
}