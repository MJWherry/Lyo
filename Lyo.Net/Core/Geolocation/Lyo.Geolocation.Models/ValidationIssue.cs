using System.Diagnostics;
using Lyo.Geolocation.Models.Enums;

namespace Lyo.Geolocation.Models;

[DebuggerDisplay("{ToString(),nq}")]
public class ValidationIssue
{
    public ValidationSeverity Severity { get; set; }

    public string Field { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public override string ToString() => $"ValidationIssue: [{Severity}] {Field} ({Code}): {Message}";
}