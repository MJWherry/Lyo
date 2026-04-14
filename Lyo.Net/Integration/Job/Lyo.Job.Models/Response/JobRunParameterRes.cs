using System.Diagnostics;
using Lyo.Common;
using Lyo.Job.Models.Enums;

namespace Lyo.Job.Models.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record JobRunParameterRes(Guid Id, Guid JobRunId, string Key, JobParameterType Type, string? Value, string? Description, byte[]? EncryptedValue, bool Enabled)
{
    public override string ToString() => $"Id={Id.Truncated(4, 4)} RunId={JobRunId.Truncated(4, 4)} ({Type}) {Key}={Value} ({Description})";
}