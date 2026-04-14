using System.Diagnostics;

namespace Lyo.Metrics.Models;

/// <summary>Represents a single metric data point.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class MetricData
{
    public string Name { get; init; } = string.Empty;

    public double Value { get; init; }

    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public Dictionary<string, string>? Tags { get; init; }

    public MetricType Type { get; init; }

    public Exception? Exception { get; init; }

    public override string ToString()
        => $"Metric: {Name}, Value: {Value}, Type: {Type}, Timestamp: {Timestamp}, Tags: {Tags?.Count}, Exception: {(Exception != null ? Exception.Message : "None")}";
}