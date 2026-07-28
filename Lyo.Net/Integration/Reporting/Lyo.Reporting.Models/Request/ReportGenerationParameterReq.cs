using System.Diagnostics;
using Lyo.Reporting.Models.Enums;

namespace Lyo.Reporting.Models.Request;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class ReportGenerationParameterReq
{
    public string Key { get; set; } = null!;

    public string? Description { get; set; }

    public ReportParameterType Type { get; set; }

    public string? Value { get; set; }

    public byte[]? EncryptedValue { get; set; }

    public ReportGenerationParameterReq() { }

    public ReportGenerationParameterReq(string key, ReportParameterType type, string? value = null, string? description = null)
    {
        Key = key;
        Type = type;
        Value = value;
        Description = description;
    }

    public override string ToString() => $"({Type}) {Key}={Value}, {Description}";
}