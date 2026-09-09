using System.Diagnostics;
using Lyo.Common.Core.Extensions;
using Lyo.Parameters;
using Lyo.Job.Models.Enums;

namespace Lyo.Job.Models.Response;

/// <summary>
/// One job run. Uses init-only properties rather than a positional record constructor so field reordering does not break the API and large-object construction stays
/// readable.
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

    /// <summary>Scheduled slot this run was created for. Used to enforce idempotency across multiple scheduler instances.</summary>
    public DateTime? ScheduledSlotUtc { get; init; }

    /// <summary>How many times this job has been retried (0 is the first attempt).</summary>
    public int RetryAttempt { get; init; }

    /// <summary>UTC time of the last heartbeat from the worker. Null until the first heartbeat arrives.</summary>
    public DateTime? LastHeartbeatUtc { get; init; }

    /// <summary>Message priority (0-9) used when this run was dispatched.</summary>
    public int Priority { get; init; }

    /// <summary>Completion percent (0-100) reported by the worker. Null until the worker reports progress.</summary>
    public int? ProgressPercent { get; init; }

    /// <summary>Short human-readable progress text reported by the worker.</summary>
    public string? ProgressMessage { get; init; }

    /// <summary>Caller-supplied key for idempotent run creation on a definition.</summary>
    public string? IdempotencyKey { get; init; }

    /// <summary>When true, the worker ran validation only and did not commit side effects.</summary>
    public bool DryRun { get; init; }

    /// <summary>Whether an SLA breach was found for this run.</summary>
    public bool SlaBreached { get; init; }

    /// <summary>Distributed trace id carried through the run lifecycle.</summary>
    public string? TraceId { get; init; }

    /// <summary>Parent run when this run belongs to a batch or fan-out.</summary>
    public Guid? ParentJobRunId { get; init; }

    /// <summary>Zero-based index inside a parent batch. Null when not part of a batch.</summary>
    public int? BatchIndex { get; init; }

    /// <summary>Total items in a parent batch. Null when this run is not part of a batch.</summary>
    public int? BatchTotal { get; init; }

    /// <summary>Snapshot of the definition version at run creation, for audit correlation.</summary>
    public int? DefinitionAuditVersion { get; init; }

    /// <summary>Worker instance that started this run. Null when the run has not started or the worker never registered.</summary>
    public Guid? WorkerInstanceId { get; init; }

    /// <summary>Machine name copied from the worker instance at start.</summary>
    public string? WorkerMachineName { get; init; }

    /// <summary>Process id copied from the worker instance at start.</summary>
    public int? WorkerProcessId { get; init; }

    /// <summary>
    /// Typed value of the result with the given key (case-insensitive) via <see cref="LyoKeyedValueExtensions" />, or default when absent or not convertible.
    /// </summary>
    public T? GetResultValueAs<T>(string key, string? format = null) => JobRunResults.GetAs<T>(key, format);

    /// <summary>
    /// Typed value of the parameter with the given key (case-insensitive) via <see cref="LyoKeyedValueExtensions" />, or default when absent or not convertible.
    /// </summary>
    public T? GetParameterValueAs<T>(string key, string? format = null) => JobRunParameters.GetAs<T>(key, format);

    public Dictionary<string, string?> GetParameterDictionary() => JobRunParameters?.ToDictionary(i => i.Key.ToString(), i => i.Value) ?? new Dictionary<string, string?>();

    public Dictionary<string, string?> GetResultDictionary() => JobRunResults?.ToDictionary(i => i.Key.ToString(), i => i.Value) ?? new Dictionary<string, string?>();

    public override string ToString()
        => $"Job Run Id={Id.Truncated()} Parameters={JobRunParameters?.Count} {(State == JobState.Finished ? $"Results={JobRunResults?.Count} " : "")}State={State} Created={CreatedTimestamp} Started={StartedTimestamp} Finished={FinishedTimestamp}";
}