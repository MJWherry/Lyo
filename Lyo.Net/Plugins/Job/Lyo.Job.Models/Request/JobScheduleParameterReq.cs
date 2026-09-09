using System.Diagnostics;
using Lyo.Parameters;

namespace Lyo.Job.Models.Request;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class JobScheduleParameterReq : LyoParameterOverrideBase
{
    public Guid JobScheduleId { get; set; }
}
