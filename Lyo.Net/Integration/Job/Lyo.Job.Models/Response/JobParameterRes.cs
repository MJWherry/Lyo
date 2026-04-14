using System.Diagnostics;
using Lyo.Common;
using Lyo.Job.Models.Enums;

namespace Lyo.Job.Models.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record JobParameterRes(
    Guid Id,
    Guid JobDefinitionId,
    string Key,
    string? Description,
    JobParameterType Type,
    string? Value,
    byte[]? EncryptedValue,
    bool AllowMultiple,
    bool Enabled,
    bool Required)
{
    public override string ToString() => $"Id={Id.Truncated(4, 4)} DefinitionId={JobDefinitionId.Truncated()} ({Type}) {Key}={Value} ({Description})";
}