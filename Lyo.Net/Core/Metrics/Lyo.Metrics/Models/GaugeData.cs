using System.Diagnostics;

namespace Lyo.Metrics.Models;

/// <summary>Represents gauge data.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class GaugeData
{
    public string Name { get; init; } = string.Empty;

    public double Value { get; set; }

    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    public Dictionary<string, string>? Tags { get; init; }

    public override string ToString() => $"Gauge: {Name}, Value: {Value}, LastUpdated: {LastUpdated}, Tags: {Tags?.Count}";
}