using Lyo.Schedule.Models;
using Lyo.Scheduler.Models;

namespace Lyo.Scheduler;

/// <summary>Schedules and runs actions at specified times.</summary>
public interface ISchedulerService
{
    /// <summary>True when the scheduler loop is running.</summary>
    bool IsRunning { get; }

    /// <summary>Registers a schedule. Replaces any existing schedule with the same ID.</summary>
    /// <param name="id">Unique identifier for the schedule.</param>
    /// <param name="name">Optional display name.</param>
    /// <param name="definition">When the schedule should fire.</param>
    /// <param name="action">Work to run when the schedule fires.</param>
    void AddSchedule(string id, string? name, ScheduleDefinition definition, Func<CancellationToken, Task> action);

    /// <summary>Removes a schedule by ID. Returns true when a schedule was removed.</summary>
    bool RemoveSchedule(string scheduleId);

    /// <summary>Returns every registered schedule (actions omitted).</summary>
    IReadOnlyCollection<ScheduleInfo> GetSchedules();

    /// <summary>Returns schedules ordered by next run (soonest first). Schedules with no next run appear last.</summary>
    /// <param name="asOf">Reference time for the next-run calculation. Uses UtcNow when null.</param>
    IReadOnlyList<ScheduleWithNextRun> GetSchedulesOrderedByNextRun(DateTime? asOf = null);

    /// <summary>Returns upcoming occurrences across all schedules, merged and ordered by run time. Frequent schedules may appear more than once.</summary>
    /// <param name="asOf">Reference time. Uses UtcNow when null.</param>
    /// <param name="maxRuns">Maximum total runs to return. Default 100.</param>
    IReadOnlyList<ScheduleRun> GetUpcomingRuns(DateTime? asOf = null, int maxRuns = 100);

    /// <summary>Returns the schedule with the given ID, or null when it is missing.</summary>
    ScheduleInfo? GetSchedule(string scheduleId);

    /// <summary>Starts the scheduler. No-op when it is already running.</summary>
    Task StartAsync(CancellationToken ct = default);

    /// <summary>Stops the scheduler and waits for a graceful shutdown.</summary>
    Task StopAsync(CancellationToken ct = default);
}