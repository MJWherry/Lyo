using System.Diagnostics;
using Lyo.Common.Extensions;
using Lyo.Job.Models.Enums;

namespace Lyo.Job.Models.Response;

/// <summary>
/// Represents a single job run. Uses init-only properties rather than a positional record constructor to prevent breaking API changes on field reordering and to make
/// large-object construction readable.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record JobRunRes
{
    public Guid Id { get; init; }

    public JobState State { get; init; }

    public JobRunResult? Result { get; init; }

    public DateTime CreatedTimestamp { get; init; }

    public DateTime? StartedTimestamp { get; init; }

    public DateTime? FinishedTimestamp { get; init; }

    public IReadOnlyList<JobRunParameterRes>? JobRunParameters { get; init; }

    public Guid? JobScheduleId { get; init; }

    public JobScheduleRes? JobSchedule { get; init; }

    public bool AllowTriggers { get; init; }

    public Guid? JobTriggerId { get; init; }

    public JobTriggerRes? JobTrigger { get; init; }

    public IReadOnlyList<JobRunResultRes>? JobRunResults { get; init; }

    public Guid JobDefinitionId { get; init; }

    public JobDefinitionRes? JobDefinition { get; init; }

    public JobRunRes? ReRanFromJobRun { get; init; }

    public IReadOnlyList<JobRunLogRes>? JobRunLogs { get; init; }

    /// <summary>The scheduled slot this run was created for. Used to enforce idempotency across multiple scheduler instances.</summary>
    public DateTime? ScheduledSlotUtc { get; init; }

    /// <summary>How many times this job has been retried (0 = first attempt).</summary>
    public int RetryAttempt { get; init; }

    /// <summary>UTC timestamp of the last heartbeat from the worker. Null until the first heartbeat arrives.</summary>
    public DateTime? LastHeartbeatUtc { get; init; }

    public T? GetResultValueAs<T>(string key, string? format = null)
    {
        var strValue = JobRunResults?.FirstOrDefault(i => i.Key.Equals(key))?.Value;
        return strValue.ToScalar<T>(format);
    }

    public T? GetParameterValueAs<T>(string key, string? format = null)
    {
        var strValue = JobRunParameters?.FirstOrDefault(i => i.Key.Equals(key))?.Value;
        return strValue.ToScalar<T>(format);
    }

    public Dictionary<string, string?> GetParameterDictionary() => JobRunParameters?.ToDictionary(i => i.Key.ToString(), i => i.Value) ?? new Dictionary<string, string?>();

    public Dictionary<string, string?> GetResultDictionary() => JobRunResults?.ToDictionary(i => i.Key.ToString(), i => i.Value) ?? new Dictionary<string, string?>();

    public override string ToString()
        => $"Job Run Id={Id.Truncated()} Parameters={JobRunParameters?.Count} {(State == JobState.Finished ? $"Results={JobRunResults?.Count} " : "")}State={State} Created={CreatedTimestamp} Started={StartedTimestamp} Finished={FinishedTimestamp}";
}