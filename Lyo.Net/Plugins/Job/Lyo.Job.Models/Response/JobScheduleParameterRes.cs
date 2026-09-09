using System.Diagnostics;
using Lyo.Common.Core.Extensions;
using Lyo.Parameters;

namespace Lyo.Job.Models.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record JobScheduleParameterRes(
    Guid Id,
    Guid JobScheduleId,
    string Key,
    string Type,
    string? Value,
    string? Description,
    byte[]? EncryptedValue,
    bool Enabled) : ILyoParameterValue
{
    public override string ToString() => $"Id={Id.Truncated(4, 4)} TriggerId={JobScheduleId.Truncated()} ({Type}) {Key}={Value} ({Description})";
}
