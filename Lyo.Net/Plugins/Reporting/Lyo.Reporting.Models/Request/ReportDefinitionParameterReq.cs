using System.Diagnostics;
using Lyo.Parameters;

namespace Lyo.Reporting.Models.Request;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class ReportDefinitionParameterReq : LyoParameterDefinitionBase
{
    public Guid ReportDefinitionId { get; set; }
}
