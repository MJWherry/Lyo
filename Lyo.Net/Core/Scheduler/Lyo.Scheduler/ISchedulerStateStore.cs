namespace Lyo.Scheduler;

/// <summary>Stores last execution state for schedules. Used to prevent duplicate runs and for metrics. Implement with CacheService for persistence.</summary>
public interface ISchedulerStateStore
{
    /// <summary>Gets the last execution timestamp for a schedule, if any.</summary>
    ValueTask<DateTime?> GetLastRunAsync(string scheduleId, CancellationToken ct = default);

    /// <summary>Sets the last execution timestamp for a schedule.</summary>
    ValueTask SetLastRunAsync(string scheduleId, DateTime timestamp, CancellationToken ct = default);

    /// <summary>Gets the last scheduled run time we executed for (to avoid duplicate runs at the same slot).</summary>
    ValueTask<DateTime?> GetLastExecutedSlotAsync(string scheduleId, CancellationToken ct = default);

    /// <summary>Sets the last executed slot timestamp.</summary>
    ValueTask SetLastExecutedSlotAsync(string scheduleId, DateTime slotTimestamp, CancellationToken ct = default);
}