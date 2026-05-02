namespace Lyo.Job.Postgres.Database;

public class JobDefinition
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string Type { get; set; } = null!;

    public string WorkerType { get; set; } = null!;

    public bool Enabled { get; set; }

    /// <summary>Maximum number of automatic retry attempts on failure. 0 = no retries.</summary>
    public int MaxRetryCount { get; set; }

    /// <summary>Base backoff in seconds between retry attempts. 0 = immediate retry. Multiplied by the attempt number for linear backoff.</summary>
    public int RetryBackoffSeconds { get; set; }

    /// <summary>Number of minutes without a heartbeat before a <c>Running</c> job is considered dead. 0 = disabled (no timeout). <see cref="JobMaintenanceService" /> enforces this.</summary>
    public int TimeoutMinutes { get; set; }

    /// <summary>Maximum number of concurrent active runs (Queued + Running). 0 = unlimited.</summary>
    public int MaxConcurrentRuns { get; set; }

    /// <summary>Number of consecutive failures before the scheduler automatically disables this definition. 0 = circuit breaker disabled.</summary>
    public int CircuitBreakerThreshold { get; set; }

    /// <summary>Minutes after the circuit breaker trips before the definition is automatically re-enabled. Only meaningful when <see cref="CircuitBreakerThreshold" /> &gt; 0.</summary>
    public int CircuitBreakerResetMinutes { get; set; }

    /// <summary>UTC timestamp when the circuit breaker last tripped (i.e. when <c>Enabled</c> was set to false by the circuit breaker).</summary>
    public DateTime? CircuitBreakerTrippedAt { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public virtual ICollection<JobParallelRestriction> JobParallelRestrictionBaseJobDefinitions { get; set; } = new List<JobParallelRestriction>();

    public virtual ICollection<JobParallelRestriction> JobParallelRestrictionOtherJobDefinitions { get; set; } = new List<JobParallelRestriction>();

    public virtual ICollection<JobParameter> JobParameters { get; set; } = new List<JobParameter>();

    public virtual ICollection<JobRun> JobRuns { get; set; } = new List<JobRun>();

    public virtual ICollection<JobSchedule> JobSchedules { get; set; } = new List<JobSchedule>();

    public virtual ICollection<JobTrigger> JobTriggerJobDefinitions { get; set; } = new List<JobTrigger>();

    public virtual ICollection<JobTrigger> JobTriggerTriggersJobDefinitions { get; set; } = new List<JobTrigger>();
}