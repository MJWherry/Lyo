using System.ComponentModel.DataAnnotations;
using Lyo.Job.Models.Enums;

namespace Lyo.Job.Postgres.Database;

public class JobDefinition
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = null!;

    [MaxLength(100)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(25)]
    public string Type { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string WorkerType { get; set; } = null!;

    public bool Enabled { get; set; }

    /// <summary>Maximum number of automatic retry attempts on failure. 0 = no retries.</summary>
    public int MaxRetryCount { get; set; }

    /// <summary>Base backoff in seconds between retry attempts. 0 = immediate retry. Multiplied by the attempt number for linear backoff.</summary>
    public int RetryBackoffSeconds { get; set; }

    /// <summary>How the retry delay grows across attempts: Linear (backoff × attempt) or Exponential (backoff × 2^(attempt-1) with jitter). Stored as string.</summary>
    [Required]
    [MaxLength(12)]
    public string RetryBackoffType { get; set; } = nameof(JobRetryBackoffType.Linear);

    /// <summary>Message priority (0-9) applied to runs of this definition. Higher values are consumed first when the worker queue supports priorities. 0 = default.</summary>
    public int Priority { get; set; }

    /// <summary>Days to keep finished runs (with logs, parameters, and results) before the maintenance service purges them. 0 = use the host's global default.</summary>
    public int RetentionDays { get; set; }

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

    /// <summary>Maximum number of runs that may be created per hour. 0 = unlimited.</summary>
    public int MaxRunsPerHour { get; set; }

    /// <summary>Expected run duration in minutes, used for SLA tracking. 0 = not configured.</summary>
    public int ExpectedDurationMinutes { get; set; }

    /// <summary>Minutes after a run is queued within which it must start, or SLA is breached. 0 = not configured.</summary>
    public int MustStartByMinutes { get; set; }

    /// <summary>Whether to emit an alert when a run fails.</summary>
    public bool AlertOnFailure { get; set; }

    /// <summary>Consecutive failures before an alert is emitted. 0 = alert on every failure when <see cref="AlertOnFailure" /> is true.</summary>
    public int AlertAfterConsecutiveFailures { get; set; }

    /// <summary>Optional webhook URL to POST alert payloads to.</summary>
    [MaxLength(500)]
    public string? AlertWebhookUrl { get; set; }

    /// <summary>Monotonic version bumped on definition changes for audit correlation with runs.</summary>
    public int DefinitionVersion { get; set; } = 1;

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public virtual ICollection<JobParallelRestriction> JobParallelRestrictionBaseJobDefinitions { get; set; } = new List<JobParallelRestriction>();

    public virtual ICollection<JobParallelRestriction> JobParallelRestrictionOtherJobDefinitions { get; set; } = new List<JobParallelRestriction>();

    public virtual ICollection<JobParameter> JobParameters { get; set; } = new List<JobParameter>();

    public virtual ICollection<JobRun> JobRuns { get; set; } = new List<JobRun>();

    public virtual ICollection<JobSchedule> JobSchedules { get; set; } = new List<JobSchedule>();

    public virtual ICollection<JobTrigger> JobTriggerJobDefinitions { get; set; } = new List<JobTrigger>();

    public virtual ICollection<JobTrigger> JobTriggerTriggersJobDefinitions { get; set; } = new List<JobTrigger>();

    public virtual ICollection<JobWorkflowStep> JobWorkflowSteps { get; set; } = new List<JobWorkflowStep>();
}