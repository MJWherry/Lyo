using System.Diagnostics;
using Lyo.Common.Metadata.Records;
using Lyo.Parameters;

namespace Lyo.Reporting.Models.Request;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class ReportGenerationParameterReq : LyoParameterValueBase
{
    public ReportGenerationParameterReq() { }

    public ReportGenerationParameterReq(string key, LyoTypeInfo type, string? value = null, string? description = null)
        : this(key, type.FullName, value, description) { }

    public ReportGenerationParameterReq(string key, string type, string? value = null, string? description = null)
    {
        Key = key;
        Type = type;
        Value = value;
        Description = description;
    }
}