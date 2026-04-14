using System.Diagnostics;
using Lyo.Common;
using Lyo.Query.Models.Enums;

namespace Lyo.Job.Models.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record JobTriggerRes(
    Guid Id,
    Guid TriggersJobDefinitionId,
    string JobResultKey,
    ComparisonOperatorEnum Comparison,
    string? JobResultValue,
    string? Description,
    bool Enabled,
    JobDefinitionRes? JobDefinition,
    IReadOnlyList<JobTriggerParameterRes>? TriggerParameters,
    JobDefinitionRes? TriggersJobDefinition)
{
    public override string ToString() => $"{Id.Truncated()} {Description ?? $"Triggers {TriggersJobDefinitionId.Truncated()} if {JobResultKey} {Comparison} {JobResultValue}"}";
}