using System.Diagnostics;
using Lyo.Common.Extensions;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Extensions;

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

    /// <summary>Message priority (0-9) used when the run was dispatched.</summary>
    public int Priority { get; init; }

    /// <summary>Completion percentage (0-100) reported by the worker. Null until the worker reports progress.</summary>
    public int? ProgressPercent { get; init; }

    /// <summary>Short human-readable progress message reported by the worker.</summary>
    public string? ProgressMessage { get; init; }

    /// <summary>Caller-supplied key for idempotent run creation within a definition.</summary>
    public string? IdempotencyKey { get; init; }

    /// <summary>When true, the worker executed validation only and did not commit side effects.</summary>
    public bool DryRun { get; init; }

    /// <summary>Whether an SLA breach was detected for this run.</summary>
    public bool SlaBreached { get; init; }

    /// <summary>Distributed trace id propagated through the run lifecycle.</summary>
    public string? TraceId { get; init; }

    /// <summary>Parent run when this run is part of a batch or fan-out.</summary>
    public Guid? ParentJobRunId { get; init; }

    /// <summary>Zero-based index within a parent batch. Null when not part of a batch.</summary>
    public int? BatchIndex { get; init; }

    /// <summary>Total items in a parent batch. Null when not part of a batch.</summary>
    public int? BatchTotal { get; init; }

    /// <summary>Snapshot of the definition version at run creation for audit correlation.</summary>
    public int? DefinitionAuditVersion { get; init; }

    /// <summary>Worker instance that started this run. Null when the run has not started or the worker did not register.</summary>
    public Guid? WorkerInstanceId { get; init; }

    /// <summary>Machine name snapshotted from the worker instance at start.</summary>
    public string? WorkerMachineName { get; init; }

    /// <summary>Process id snapshotted from the worker instance at start.</summary>
    public int? WorkerProcessId { get; init; }

    /// <summary>Returns the typed value of the result with the given key (case-insensitive) via <see cref="JobRunParameterExtensions" />, or default when absent / not convertible.</summary>
    public T? GetResultValueAs<T>(string key, string? format = null) => JobRunResults.GetAs<T>(key, format);

    /// <summary>Returns the typed value of the parameter with the given key (case-insensitive) via <see cref="JobRunParameterExtensions" />, or default when absent / not convertible.</summary>
    public T? GetParameterValueAs<T>(string key, string? format = null) => JobRunParameters.GetAs<T>(key, format);

    public Dictionary<string, string?> GetParameterDictionary() => JobRunParameters?.ToDictionary(i => i.Key.ToString(), i => i.Value) ?? new Dictionary<string, string?>();

    public Dictionary<string, string?> GetResultDictionary() => JobRunResults?.ToDictionary(i => i.Key.ToString(), i => i.Value) ?? new Dictionary<string, string?>();

    public override string ToString()
        => $"Job Run Id={Id.Truncated()} Parameters={JobRunParameters?.Count} {(State == JobState.Finished ? $"Results={JobRunResults?.Count} " : "")}State={State} Created={CreatedTimestamp} Started={StartedTimestamp} Finished={FinishedTimestamp}";
}