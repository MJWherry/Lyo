using System.Diagnostics;
using Lyo.Common;
using Lyo.Job.Models.Enums;

namespace Lyo.Job.Models.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record JobScheduleParameterRes(
    Guid Id,
    Guid JobScheduleId,
    string Key,
    JobParameterType Type,
    string? Value,
    string? Description,
    byte[]? EncryptedValue,
    bool Enabled)
{
    public override string ToString() => $"Id={Id.Truncated(4, 4)} TriggerId={JobScheduleId.Truncated()} ({Type}) {Key}={Value} ({Description})";
}