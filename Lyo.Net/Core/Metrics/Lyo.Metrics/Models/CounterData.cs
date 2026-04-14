using System.Diagnostics;

namespace Lyo.Metrics.Models;

/// <summary>Represents aggregated counter data.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class CounterData
{
    public string Name { get; init; } = string.Empty;

    public long Value { get; set; }

    public Dictionary<string, string>? Tags { get; init; }

    public override string ToString() => $"Counter: {Name}, Value: {Value}, Tags: {Tags?.Count}";
}