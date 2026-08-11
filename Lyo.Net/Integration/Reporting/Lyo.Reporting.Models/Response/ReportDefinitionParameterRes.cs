using System.Diagnostics;
using Lyo.Reporting.Models.Enums;

namespace Lyo.Reporting.Models.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record ReportDefinitionParameterRes(
    Guid Id,
    Guid ReportDefinitionId,
    string Key,
    string? Description,
    ReportParameterType Type,
    string? Value,
    byte[]? EncryptedValue,
    bool AllowMultiple,
    bool Required,
    string? ValidationRegex = null,
    int? MinLength = null,
    int? MaxLength = null,
    string? AllowedValues = null,
    string? Options = null,
    DateTime CreatedTimestamp = default,
    DateTime? UpdatedTimestamp = null)
{
    public override string ToString() => $"({Type}) {Key}={Value} ({Description})";
}