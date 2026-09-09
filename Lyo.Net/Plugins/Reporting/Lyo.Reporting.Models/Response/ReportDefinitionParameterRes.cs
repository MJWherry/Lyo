using System.Diagnostics;
using Lyo.Parameters;

namespace Lyo.Reporting.Models.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record ReportDefinitionParameterRes(
    Guid Id,
    Guid ReportDefinitionId,
    string Key,
    string? Description,
    string Type,
    string? Value,
    byte[]? EncryptedValue,
    bool Required,
    string? ValidationRegex = null,
    int? MinLength = null,
    int? MaxLength = null,
    string? AllowedValues = null,
    string? Options = null,
    DateTime CreatedTimestamp = default,
    DateTime? UpdatedTimestamp = null,
    LyoParameterDefaultKind DefaultKind = LyoParameterDefaultKind.Literal,
    string? DefaultTemplate = null) : ILyoParameterDefinition
{
    public override string ToString() => $"({Type}) {Key}={Value} ({Description})";
}
