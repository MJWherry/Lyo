using System.Diagnostics;
using Lyo.Job.Models.Enums;

namespace Lyo.Job.Models.Request;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class JobDefinitionReq
{
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string Type { get; set; }

    public string WorkerType { get; set; } = null!;

    public bool Enabled { get; set; } = true;

    /// <summary>Maximum number of automatic retry attempts after a failure. 0 = no retries.</summary>
    public int MaxRetryCount { get; set; }

    /// <summary>Base backoff in seconds between retries. 0 = immediate. Each attempt waits <c>RetryBackoffSeconds × attempt</c> seconds.</summary>
    public int RetryBackoffSeconds { get; set; }

    /// <summary>Minutes without a heartbeat before a Running job is considered dead and failed. 0 = disabled.</summary>
    public int TimeoutMinutes { get; set; }

    /// <summary>Maximum concurrent active runs (Queued + Running) for this definition. 0 = unlimited.</summary>
    public int MaxConcurrentRuns { get; set; }

    /// <summary>Consecutive failures before the scheduler auto-disables this definition. 0 = circuit breaker off.</summary>
    public int CircuitBreakerThreshold { get; set; }

    /// <summary>Minutes before the circuit breaker auto-resets and re-enables the definition. 0 = never auto-reset.</summary>
    public int CircuitBreakerResetMinutes { get; set; }

    /// <summary>How the retry delay grows across attempts.</summary>
    public JobRetryBackoffType RetryBackoffType { get; set; } = JobRetryBackoffType.Linear;

    /// <summary>Message priority (0-9) applied to runs of this definition. Higher values are consumed first. 0 = default.</summary>
    public int Priority { get; set; }

    /// <summary>Days to keep finished runs before they are purged by the maintenance service. 0 = use the host's global default.</summary>
    public int RetentionDays { get; set; }

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
    public string? AlertWebhookUrl { get; set; }

    /// <summary>Default blackout calendar id applied to every schedule unless a schedule sets its own.</summary>
    public Guid? JobBlackoutCalendarId { get; set; }

    /// <summary>Inline default blackout calendar applied to every schedule unless a schedule sets its own.</summary>
    public JobBlackoutCalendarReq? CreateBlackoutCalendar { get; set; }

    public List<JobParameterReq> CreateParameters { get; set; } = [];

    public List<JobScheduleReq> CreateSchedules { get; set; } = [];

    public List<JobTriggerReq> CreateTriggers { get; set; } = [];

    public List<JobParallelRestrictionReq> CreateParallelRestrictions { get; set; } = [];

    public JobDefinitionReq() { }

    public JobDefinitionReq(string name, string? description = null, bool enabled = true)
    {
        Name = name;
        Description = description;
        Enabled = enabled;
    }

    public override string ToString()
        => $"{Name}, {Description} (Enabled={Enabled}) Params(C={CreateParameters.Count}) " + $"Schedules(C={CreateSchedules.Count}) Triggers(C={CreateTriggers.Count}) " +
            $"Blackout(Id={JobBlackoutCalendarId}, Inline={CreateBlackoutCalendar != null})";
}