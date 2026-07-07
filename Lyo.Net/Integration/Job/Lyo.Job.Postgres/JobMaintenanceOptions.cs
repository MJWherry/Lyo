namespace Lyo.Job.Postgres;

/// <summary>Options for <see cref="JobMaintenanceService" />.</summary>
public sealed class JobMaintenanceOptions
{
    /// <summary>Default configuration section name.</summary>
    public const string SectionName = "JobMaintenance";

    /// <summary>Interval in seconds between maintenance ticks (dead-job detection, circuit breaker reset, retention purge). Default 30.</summary>
    public int CheckIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Global default retention in days for finished job runs (including their logs, parameters, and results). 0 disables purging. Definitions can override this via their
    /// own <c>RetentionDays</c>. Default 0 (keep forever).
    /// </summary>
    public int DefaultRetentionDays { get; set; }

    /// <summary>Maximum number of job runs deleted per maintenance tick, to keep purge transactions small. Default 500.</summary>
    public int PurgeBatchSize { get; set; } = 500;

    /// <summary>Minutes after the last heartbeat before a worker instance registration is considered stale and removed. Default 5.</summary>
    public int WorkerInstanceStaleMinutes { get; set; } = 5;

    /// <summary>Validates the options and returns the list of validation failures (empty when valid).</summary>
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

        return errors;
    }

    /// <summary>Validates the options, throwing <see cref="ArgumentException" /> when invalid.</summary>
    public void Validate()
    {
        var errors = GetValidationErrors();
        if (errors.Count > 0)
            throw new ArgumentException($"Invalid {nameof(JobMaintenanceOptions)}: {string.Join(" ", errors)}");
    }
}
