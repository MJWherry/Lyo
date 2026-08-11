using System.Diagnostics;
using Lyo.Reporting.Models.Enums;

namespace Lyo.Reporting.Models.Request;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class ReportDefinitionParameterReq
{
    public Guid ReportDefinitionId { get; set; }

    public string Key { get; set; } = null!;

    public string? Description { get; set; }

    public ReportParameterType Type { get; set; }

    /// <summary>Default value for generate (UI: Default Value).</summary>
    public string? Value { get; set; }

    public bool Required { get; set; }

    public byte[]? EncryptedValue { get; set; }

    public bool AllowMultiple { get; set; }

    public string? ValidationRegex { get; set; }

    public int? MinLength { get; set; }

    public int? MaxLength { get; set; }

    /// <summary>Pipe-separated list of allowed values (e.g. <c>A|B|C</c>).</summary>
    public string? AllowedValues { get; set; }

    /// <summary>JSON picker source (static items or root QueryReq). Null = no options picker; scalar <see cref="Value" /> remains the default.</summary>
    public string? Options { get; set; }

    public override string ToString() => $"({Type}) {Key}={Value}, {Description}";
}