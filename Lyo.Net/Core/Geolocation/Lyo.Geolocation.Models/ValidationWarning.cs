using System.Diagnostics;

namespace Lyo.Geolocation.Models;

[DebuggerDisplay("{ToString(),nq}")]
public class ValidationWarning
{
    public string Field { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string? Suggestion { get; set; }

    public override string ToString()
        => $"ValidationWarning: {Field}: {Message}";
}
