using System.Diagnostics;
using Lyo.Common;

namespace Lyo.Job.Models.Request;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class JobParallelRestrictionReq
{
    public Guid OtherJobDefinitionId { get; set; }

    public string? Description { get; set; }

    public bool Enabled { get; set; }

    public JobParallelRestrictionReq() { }

    public JobParallelRestrictionReq(Guid otherJobDefinition, string? description = null, bool? enabled = true)
    {
        OtherJobDefinitionId = otherJobDefinition;
        Description = description;
        Enabled = enabled ?? true;
    }

    public override string ToString() => $"Forbids {OtherJobDefinitionId.Truncated()}, {Description}, Enabled={Enabled}";
}