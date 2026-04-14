using System.Diagnostics;
using Lyo.Common;
using Lyo.Query.Models.Enums;

namespace Lyo.Job.Models.Request;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class JobTriggerReq
{
    public Guid JobDefinitionId { get; set; }

    public Guid TriggersJobDefinitionId { get; set; }

    public string JobResultKey { get; set; } = null!;

    public ComparisonOperatorEnum Comparison { get; set; }

    public string? JobResultValue { get; set; }

    public string? Description { get; set; }

    public bool Enabled { get; set; } = true;

    public List<JobTriggerParameterReq> CreateTriggerParameters { get; set; } = [];

    public override string ToString() => Description ?? $"Triggers {TriggersJobDefinitionId.Truncated()} if {JobResultKey} {Comparison} {JobResultValue}";
}