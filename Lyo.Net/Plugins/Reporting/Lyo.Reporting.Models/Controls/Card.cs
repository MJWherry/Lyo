using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Lyo.Reporting.Models.Controls;

/// <summary>A labeled KPI-style value. Place several in a <see cref="Grid" /> to sit side by side.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class Card : Control
{
    /// <summary>Card label.</summary>
    public string? Label { get; set; }

    /// <summary>Card value.</summary>
    public object? Value { get; set; }

    /// <summary>Width as a CSS value (for example "50%", "200px").</summary>
    public string? Width { get; set; }

    /// <summary>Alignment (left, right, center, or justify).</summary>
    public string? Alignment { get; set; }

    /// <summary>Whether the value is emphasized.</summary>
    public bool Emphasized { get; set; }

    /// <summary>Formatter for the value. Not persisted in JSON.</summary>
    [JsonIgnore]
    public Func<object?, string>? ValueFormatter { get; set; }

    public override string ToString() => $"Card: {Label ?? "(No Label)"} = {Value}";
}
