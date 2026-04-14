using System.Diagnostics;

namespace Lyo.Metrics.Models;

/// <summary>Represents histogram/timing data.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class HistogramData
{
    public string Name { get; init; } = string.Empty;

    public List<double> Values { get; init; } = new();

    public Dictionary<string, string>? Tags { get; init; }

    public double Min => Values.Count > 0 ? Values.Min() : 0;

    public double Max => Values.Count > 0 ? Values.Max() : 0;

    public double Average => Values.Count > 0 ? Values.Average() : 0;

    public double Sum => Values.Sum();

    public int Count => Values.Count;

    public override string ToString() => $"Histogram: {Name}, Count: {Count}, Min: {Min}, Max: {Max}, Average: {Average}, Sum: {Sum}, Tags: {Tags?.Count}";
}