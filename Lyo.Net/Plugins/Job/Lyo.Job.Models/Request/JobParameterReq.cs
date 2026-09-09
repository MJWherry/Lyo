using System.Diagnostics;
using Lyo.Parameters;

namespace Lyo.Job.Models.Request;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class JobParameterReq : LyoParameterDefinitionBase
{
    public Guid JobDefinitionId { get; set; }

    public bool Enabled { get; set; }

    /// <summary>Display order among parameters on this definition. Smaller values appear first.</summary>
    public int Order { get; set; }
}
