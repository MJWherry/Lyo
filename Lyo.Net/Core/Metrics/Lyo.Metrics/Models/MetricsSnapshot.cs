using System.Diagnostics;

namespace Lyo.Metrics.Models;

/// <summary>A snapshot of all metrics at a point in time. Can be serialized for export or persistence.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class MetricsSnapshot
{
    /// <summary>All counter metrics.</summary>
    public Dictionary<string, CounterData> Counters { get; set; } = new();

    /// <summary>All gauge metrics.</summary>
    public Dictionary<string, GaugeData> Gauges { get; set; } = new();

    /// <summary>All histogram metrics.</summary>
    public Dictionary<string, HistogramData> Histograms { get; set; } = new();

    /// <summary>Total number of metrics recorded.</summary>
    public long TotalMetricsRecorded { get; set; }

    /// <summary>When this snapshot was created.</summary>
    public DateTime ExportTime { get; set; }

    public override string ToString()
        => $"MetricsSnapshot: Counters: {Counters.Count}, Gauges: {Gauges.Count}, Histograms: {Histograms.Count}, TotalMetricsRecorded: {TotalMetricsRecorded}, ExportTime: {ExportTime}";
}