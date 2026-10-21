using System.Diagnostics;
using Lyo.Common.Metadata.Records;
using Lyo.Parameters;

namespace Lyo.Job.Models.Request;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class JobRunParameterReq : LyoParameterOverrideBase
{
    public JobRunParameterReq() { }

    public JobRunParameterReq(string key, LyoTypeInfo type, string? value = null, string? description = null)
        : this(key, type.FullName, value, description) { }

    public JobRunParameterReq(string key, string type, string? value = null, string? description = null)
    {
        Key = key;
        Type = type;
        Value = value;
        Description = description;
    }
}