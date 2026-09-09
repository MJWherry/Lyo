using System.Diagnostics;
using Lyo.Parameters;

namespace Lyo.Reporting.Models.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record ReportGenerationParameterRes(
    Guid Id,
    Guid ReportGenerationId,
    string Key,
    string Type,
    string? Value,
    string? Description,
    byte[]? EncryptedValue) : ILyoParameterValue
{
    public override string ToString() => $"({Type}) {Key}={Value} ({Description})";
}
