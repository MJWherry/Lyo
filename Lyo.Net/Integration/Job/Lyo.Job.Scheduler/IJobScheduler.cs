namespace Lyo.Job.Scheduler;

/// <summary>Controls the distributed job scheduler: loads definitions, evaluates schedules, and creates job runs via the Job API.</summary>
public interface IJobScheduler
{
    /// <summary>Whether the scheduler is currently running.</summary>
    bool IsRunning { get; }

    /// <summary>
    /// Reloads all enabled job definitions from the API. Safe to call externally; acquires the internal lock automatically.
    /// </summary>
    Task RefreshDefinitionsAsync(CancellationToken ct = default);

    /// <summary>
    /// Evaluates all loaded schedules and creates job runs for any that are due. Safe to call externally; acquires the
    /// internal lock automatically.
    /// </summary>
    Task CheckSchedulesAsync(CancellationToken ct = default);
}
