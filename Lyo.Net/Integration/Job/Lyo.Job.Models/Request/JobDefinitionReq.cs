using System.Diagnostics;

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
        => $"{Name}, {Description} (Enabled={Enabled}) Params(C={CreateParameters.Count}) " + $"Schedules(C={CreateSchedules.Count}) " + $"Triggers(C={CreateTriggers.Count})";
}