using Lyo.Schedule.Models;
using Lyo.Scheduler.Models;

namespace Lyo.Scheduler;

/// <summary>Service for scheduling and executing actions at specific times.</summary>
public interface ISchedulerService
{
    /// <summary>Whether the scheduler is currently running.</summary>
    bool IsRunning { get; }

    /// <summary>Adds a schedule. Replaces existing schedule with the same ID.</summary>
    /// <param name="id">Unique identifier for the schedule.</param>
    /// <param name="name">Optional display name.</param>
    /// <param name="definition">The schedule definition (when to run).</param>
    /// <param name="action">The action to execute when the schedule fires.</param>
    void AddSchedule(string id, string? name, ScheduleDefinition definition, Func<CancellationToken, Task> action);

    /// <summary>Removes a schedule by ID. Returns true if removed.</summary>
    bool RemoveSchedule(string scheduleId);

    /// <summary>Gets all registered schedules (without actions).</summary>
    IReadOnlyCollection<ScheduleInfo> GetSchedules();

    /// <summary>Gets schedules ordered by next run time (soonest first). Schedules with no next run appear last.</summary>
    /// <param name="asOf">The reference time for calculating next run. Uses UtcNow if null.</param>
    IReadOnlyList<ScheduleWithNextRun> GetSchedulesOrderedByNextRun(DateTime? asOf = null);

    /// <summary>Gets upcoming run occurrences across all schedules, merged and ordered by run time. Frequently-running schedules appear multiple times.</summary>
    /// <param name="asOf">The reference time. Uses UtcNow if null.</param>
    /// <param name="maxRuns">Maximum total runs to return. Default 100.</param>
    IReadOnlyList<ScheduleRun> GetUpcomingRuns(DateTime? asOf = null, int maxRuns = 100);

    /// <summary>Gets a schedule by ID, or null if not found.</summary>
    ScheduleInfo? GetSchedule(string scheduleId);

    /// <summary>Starts the scheduler. Has no effect if already running.</summary>
    Task StartAsync(CancellationToken ct = default);

    /// <summary>Stops the scheduler gracefully.</summary>
    Task StopAsync(CancellationToken ct = default);
}