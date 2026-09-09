using System.Diagnostics;
using Lyo.Job.Models.Enums;

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
    DateTime? CircuitBreakerTrippedAt = null,
    JobRetryBackoffType RetryBackoffType = JobRetryBackoffType.Linear,
    int Priority = 0,
    int RetentionDays = 0,
    int MaxRunsPerHour = 0,
    int ExpectedDurationMinutes = 0,
    int MustStartByMinutes = 0,
    bool AlertOnFailure = false,
    int AlertAfterConsecutiveFailures = 0,
    string? AlertWebhookUrl = null,
    int DefinitionVersion = 1)
{
    public override string ToString() => $"{Name}, {Description} (Enabled={Enabled})";
}