using System.Diagnostics;
using Lyo.Parameters;

namespace Lyo.Job.Models.Request;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class JobTriggerParameterReq : LyoParameterOverrideBase
{
    /// <summary>Trigger parameters apply unless the caller opts out, unlike the other parameter shapes.</summary>
    public JobTriggerParameterReq() => Enabled = true;

    public Guid JobTriggerId { get; set; }
}
