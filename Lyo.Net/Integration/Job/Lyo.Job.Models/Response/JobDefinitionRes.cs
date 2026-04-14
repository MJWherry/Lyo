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
    IReadOnlyList<JobParallelRestrictionRes>? JobParallelRestrictions)
{
    public override string ToString() => $"{Name}, {Description} (Enabled={Enabled})";
}