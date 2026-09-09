using Lyo.Exceptions.Models;

namespace Lyo.Job.Postgres;

/// <summary>Options used by <see cref="JobMaintenanceService" />.</summary>
public sealed class JobMaintenanceOptions
{
    /// <summary>Default name of the configuration section.</summary>
    public const string SectionName = "JobMaintenance";

    /// <summary>Seconds between maintenance ticks (dead-job detection, circuit breaker reset, retention purge). Default 30.</summary>
    public int CheckIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Global default retention, in days, for finished job runs (including their logs, parameters, and results). 0 disables purging. Definitions can override this via their own
    /// <c>RetentionDays</c>. Default 0 (keep forever).
    /// </summary>
    public int DefaultRetentionDays { get; set; }

    /// <summary>Most job runs deleted per maintenance tick, so purge transactions stay small. Default 500.</summary>
    public int PurgeBatchSize { get; set; } = 500;

    /// <summary>Minutes after the last heartbeat before a worker instance registration is treated as stale and removed. Default 5.</summary>
    public int WorkerInstanceStaleMinutes { get; set; } = 5;

    /// <summary>
    /// Minutes a due <c>Queued</c> run may sit untouched before the maintenance service re-publishes its dispatch message (recovery for lost publishes, delayed retries, and
    /// crashed suppressed-dispatch owners). Duplicate deliveries are harmless. <c>StartedJobRun</c> only transitions <c>Queued -&gt; Running</c> once. Set above the worst-case
    /// legitimate queue wait for your workers. 0 disables recovery. Default 10.
    /// </summary>
    public int QueuedRunRedispatchMinutes { get; set; } = 10;

    /// <summary>Most stuck queued runs re-published per maintenance tick. Default 200.</summary>
    public int QueuedRunRedispatchBatchSize { get; set; } = 200;

    /// <summary>
    /// Absolute ceiling, in minutes, for a <c>Running</c>/<c>Cancelling</c> run whose definition sets no <c>TimeoutMinutes</c>. Heartbeat-based dead-job detection cannot help
    /// those runs, so without a ceiling a crashed worker leaves them active forever. Set well above your longest legitimate run. 0 disables the ceiling. Default 1440 (24 hours).
    /// </summary>
    public int OrphanedRunTimeoutMinutes { get; set; } = 1440;

    /// <summary>Most active runs inspected per maintenance tick by dead-job detection and SLA checks. Default 1000.</summary>
    public int ActiveRunScanBatchSize { get; set; } = 1000;

    /// <summary>Checks the options and returns the list of validation failures (empty when valid).</summary>
    public IReadOnlyList<string> GetValidationErrors()
    {
        var errors = new List<string>();
        if (CheckIntervalSeconds <= 0)
            errors.Add($"{nameof(CheckIntervalSeconds)} must be greater than 0.");

        if (DefaultRetentionDays < 0)
            errors.Add($"{nameof(DefaultRetentionDays)} must be 0 or greater.");

        if (PurgeBatchSize <= 0)
            errors.Add($"{nameof(PurgeBatchSize)} must be greater than 0.");

        if (WorkerInstanceStaleMinutes <= 0)
            errors.Add($"{nameof(WorkerInstanceStaleMinutes)} must be greater than 0.");

        if (QueuedRunRedispatchMinutes < 0)
            errors.Add($"{nameof(QueuedRunRedispatchMinutes)} must be 0 or greater.");

        if (QueuedRunRedispatchBatchSize <= 0)
            errors.Add($"{nameof(QueuedRunRedispatchBatchSize)} must be greater than 0.");

        if (OrphanedRunTimeoutMinutes < 0)
            errors.Add($"{nameof(OrphanedRunTimeoutMinutes)} must be 0 or greater.");

        if (ActiveRunScanBatchSize <= 0)
            errors.Add($"{nameof(ActiveRunScanBatchSize)} must be greater than 0.");

        return errors;
    }

    /// <summary>Checks the options and throws <see cref="ValidationException" /> when they are invalid.</summary>
    public void Validate()
    {
        var errors = GetValidationErrors();
        if (errors.Count > 0)
            throw new ValidationException($"Invalid {nameof(JobMaintenanceOptions)}: {string.Join(" ", errors)}");
    }
}