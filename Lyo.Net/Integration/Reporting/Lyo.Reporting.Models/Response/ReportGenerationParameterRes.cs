using System.Diagnostics;
using Lyo.Reporting.Models.Enums;

namespace Lyo.Reporting.Models.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record ReportGenerationParameterRes(
    Guid Id,
    Guid ReportGenerationId,
    string Key,
    ReportParameterType Type,
    string? Value,
    string? Description,
    byte[]? EncryptedValue)
{
    public override string ToString() => $"({Type}) {Key}={Value} ({Description})";
}