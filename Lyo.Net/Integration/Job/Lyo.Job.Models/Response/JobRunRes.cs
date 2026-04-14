using System.Diagnostics;
using Lyo.Common;
using Lyo.Job.Models.Enums;

namespace Lyo.Job.Models.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record JobRunRes(
    Guid Id,
    JobState State,
    JobRunResult? Result,
    DateTime CreatedTimestamp,
    DateTime? StartedTimestamp,
    DateTime? FinishedTimestamp,
    IReadOnlyList<JobRunParameterRes>? JobRunParameters,
    Guid? JobScheduleId,
    JobScheduleRes? JobSchedule,
    bool AllowTriggers,
    Guid? JobTriggerId,
    JobTriggerRes? JobTrigger,
    IReadOnlyList<JobRunResultRes>? JobRunResults,
    Guid JobDefinitionId,
    JobDefinitionRes? JobDefinition,
    JobRunRes? ReRanFromJobRun,
    IReadOnlyList<JobRunLogRes>? JobRunLogs)
{
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