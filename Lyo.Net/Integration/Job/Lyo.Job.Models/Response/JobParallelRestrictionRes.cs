using System.Diagnostics;

namespace Lyo.Job.Models.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record JobParallelRestrictionRes(Guid Id, Guid JobDefinitionId, Guid OtherJobDefinitionId, string? Description, bool Enabled, JobDefinitionRes? OtherJobDefinition)
{
    public override string ToString() => $"{Id} {Description}";
}