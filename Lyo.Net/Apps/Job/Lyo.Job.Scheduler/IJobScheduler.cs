namespace Lyo.Job.Scheduler;

/// <summary>Drives the distributed job scheduler: loads definitions, evaluates schedules, and creates job runs via the Job API.</summary>
public interface IJobScheduler
{
    /// <summary>True while the scheduler is running.</summary>
    bool IsRunning { get; }

    /// <summary>Reloads every enabled job definition from the API. Safe to call from outside; takes the internal lock itself.</summary>
    Task RefreshDefinitionsAsync(CancellationToken ct = default);

    /// <summary>Evaluates every loaded schedule and creates job runs for any that are due. Safe to call from outside; takes the internal lock itself.</summary>
    Task CheckSchedulesAsync(CancellationToken ct = default);
}