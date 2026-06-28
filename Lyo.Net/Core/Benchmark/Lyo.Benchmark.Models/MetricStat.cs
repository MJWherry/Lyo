namespace Lyo.Benchmark.Models;

/// <summary>
/// A distribution of a single measured metric (latency, duration, etc.) shared by both report kinds.
/// Micro-benchmarks populate it in nanoseconds (<see cref="Unit" /> = <c>ns</c>); load tests in milliseconds (<c>ms</c>).
/// </summary>
public sealed class MetricStat
{
    /// <summary>Minimum observed value.</summary>
    public double? Min { get; set; }

    /// <summary>50th percentile (median).</summary>
    public double? P50 { get; set; }

    /// <summary>90th percentile.</summary>
    public double? P90 { get; set; }

    /// <summary>95th percentile.</summary>
    public double? P95 { get; set; }

    /// <summary>99th percentile.</summary>
    public double? P99 { get; set; }

    /// <summary>Arithmetic mean.</summary>
    public double? Avg { get; set; }

    /// <summary>Maximum observed value.</summary>
    public double? Max { get; set; }

    /// <summary>Unit of the values above, e.g. <c>ns</c> or <c>ms</c>.</summary>
    public string Unit { get; set; } = "ns";
}
