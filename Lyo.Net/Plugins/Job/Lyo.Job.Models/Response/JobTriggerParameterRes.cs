using System.Diagnostics;
using Lyo.Common.Core.Extensions;
using Lyo.Parameters;

namespace Lyo.Job.Models.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record JobTriggerParameterRes(Guid Id, Guid JobTriggerId, string Key, string Type, string? Value, string? Description, byte[]? EncryptedValue, bool Enabled)
    : ILyoParameterValue
{
    public override string ToString() => $"Id={Id.Truncated(4, 4)} TriggerId={JobTriggerId.Truncated()} ({Type}) {Key}={Value} ({Description})";
}
