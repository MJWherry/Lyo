using System.Diagnostics;

namespace Lyo.Job.Models.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record JobDefinitionRes(
    Guid Id,
    string Name,
    string? Description,
    string Type,
    string WorkerType,
    bool Enabled,
    IReadOnlyList<JobParameterRes>? JobParameters,
    IReadOnlyList<JobScheduleRes>? JobSchedules,
    IReadOnlyList<JobTriggerRes>? JobTriggers,
    IReadOnlyList<JobParallelRestrictionRes>? JobParallelRestrictions,
    int MaxRetryCount = 0,
    int RetryBackoffSeconds = 0,
    int TimeoutMinutes = 0,
    int MaxConcurrentRuns = 0,
    int CircuitBreakerThreshold = 0,
    int CircuitBreakerResetMinutes = 0,
    DateTime? CircuitBreakerTrippedAt = null)
{
    public override string ToString() => $"{Name}, {Description} (Enabled={Enabled})";
}