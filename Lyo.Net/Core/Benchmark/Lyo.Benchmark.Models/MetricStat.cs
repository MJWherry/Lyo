namespace Lyo.Benchmark.Models;

/// <summary>
/// Distribution of one measured metric (latency, duration, and similar) used by both report kinds. Micro-benchmarks fill it in nanoseconds (<see cref="Unit" /> =
/// <c>ns</c>); load tests use milliseconds (<c>ms</c>).
/// </summary>
public sealed class MetricStat
{
    /// <summary>Smallest observed value.</summary>
    public double? Min { get; set; }

    /// <summary>Median (50th percentile).</summary>
    public double? P50 { get; set; }

    /// <summary>90th-percentile value.</summary>
    public double? P90 { get; set; }

    /// <summary>95th-percentile value.</summary>
    public double? P95 { get; set; }

    /// <summary>99th-percentile value.</summary>
    public double? P99 { get; set; }

    /// <summary>Arithmetic average.</summary>
    public double? Avg { get; set; }

    /// <summary>Largest observed value.</summary>
    public double? Max { get; set; }

    /// <summary>Unit of the values above, for example <c>ns</c> or <c>ms</c>.</summary>
    public string Unit { get; set; } = "ns";
}