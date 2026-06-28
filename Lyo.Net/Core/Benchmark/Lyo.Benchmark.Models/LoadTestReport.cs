using System.Collections.Generic;

namespace Lyo.Benchmark.Models;

/// <summary>
/// Load-test report (k6). Captures per-scenario latency/throughput plus endpoint rollups, SLO assessment, and grades.
/// </summary>
public sealed class LoadTestReport : BenchmarkReport
{
    /// <summary>Per-case query structure (where clauses, sort, includes, selection field count) behind the scenarios.</summary>
    public List<LoadCase> Cases { get; set; } = [];

    /// <summary>Per-scenario results (e.g. load / stress / spike / soak for each endpoint).</summary>
    public List<LoadScenario> Scenarios { get; set; } = [];

    /// <summary>Aggregated pass-rates grouped by endpoint family.</summary>
    public List<EndpointRollup> Rollups { get; set; } = [];

    /// <summary>Business-standard / SLO assessment rows.</summary>
    public List<SloRow> Slo { get; set; } = [];

    /// <summary>Letter grades with rationales.</summary>
    public List<GradeRow> Grades { get; set; } = [];
}

/// <summary>
/// Describes the structure of a single load-test query case so latencies are interpretable (e.g. how many
/// where clauses, which filters/sorts/includes, and how many fields the projection selects).
/// </summary>
public sealed class LoadCase
{
    /// <summary>Case identifier, matching <see cref="Hotspot.Case" /> (e.g. <c>complex_querynode</c>).</summary>
    public string Case { get; set; } = string.Empty;

    /// <summary>Endpoint family, e.g. <c>query</c> / <c>queryproject</c>.</summary>
    public string? Endpoint { get; set; }

    /// <summary>What the case exercises and why it is interesting.</summary>
    public string? Description { get; set; }

    /// <summary>Number of where-clause predicates in the request body.</summary>
    public int? WhereClauses { get; set; }

    /// <summary>Filter fields/values applied (display strings).</summary>
    public List<string>? Filters { get; set; }

    /// <summary>Sort fields applied (display strings); empty when relying on server default (PK) order.</summary>
    public List<string>? SortFields { get; set; }

    /// <summary>Include/navigation branches loaded, e.g. <c>contactaddresses.address</c>.</summary>
    public List<string>? Includes { get; set; }

    /// <summary>Number of fields the projection/selection returns (for QueryProject cases).</summary>
    public int? SelectionFieldCount { get; set; }
}

/// <summary>A single load-test scenario run.</summary>
public sealed class LoadScenario
{
    /// <summary>Scenario name, e.g. <c>query_load</c>.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Profile dimension, e.g. <c>load</c> / <c>stress</c> / <c>spike</c> / <c>soak</c>.</summary>
    public string? Profile { get; set; }

    /// <summary>Endpoint family, e.g. <c>query</c> / <c>queryproject</c>.</summary>
    public string? Endpoint { get; set; }

    /// <summary>HTTP request duration distribution (milliseconds).</summary>
    public MetricStat Latency { get; set; } = new() { Unit = "ms" };

    /// <summary>Requests per second.</summary>
    public double? Throughput { get; set; }

    /// <summary>Total request count.</summary>
    public long Requests { get; set; }

    /// <summary>Overall check pass-rate (percent).</summary>
    public double? ChecksPass { get; set; }

    /// <summary>Status-code success rate (percent).</summary>
    public double? StatusPass { get; set; }

    /// <summary>Response-shape success rate (percent).</summary>
    public double? ShapePass { get; set; }

    /// <summary>Latency-threshold success rate (percent).</summary>
    public double? LatencyPass { get; set; }

    /// <summary>Iterations dropped by k6 (saturation indicator).</summary>
    public long DroppedIterations { get; set; }

    /// <summary>Slowest per-case latencies within the scenario.</summary>
    public List<Hotspot> Hotspots { get; set; } = [];
}

/// <summary>Aggregated pass-rates for an endpoint family across scenarios.</summary>
public sealed class EndpointRollup
{
    /// <summary>Endpoint family label.</summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>Total requests across the family.</summary>
    public long TotalRequests { get; set; }

    /// <summary>Weighted check pass-rate (percent).</summary>
    public double? ChecksPass { get; set; }

    /// <summary>Weighted status success rate (percent).</summary>
    public double? StatusPass { get; set; }

    /// <summary>Weighted shape success rate (percent).</summary>
    public double? ShapePass { get; set; }

    /// <summary>Weighted latency success rate (percent).</summary>
    public double? LatencyPass { get; set; }
}

/// <summary>One SLO / business-standard assessment row.</summary>
public sealed class SloRow
{
    /// <summary>Area under assessment.</summary>
    public string Area { get; set; } = string.Empty;

    /// <summary>Target threshold (display string).</summary>
    public string Target { get; set; } = string.Empty;

    /// <summary>Latest observed value (display string).</summary>
    public string Latest { get; set; } = string.Empty;

    /// <summary>Outcome, e.g. <c>Meets</c> / <c>Exceeds target</c> / <c>Miss</c>.</summary>
    public string Result { get; set; } = string.Empty;
}

/// <summary>A letter grade with rationale for a category.</summary>
public sealed class GradeRow
{
    /// <summary>Graded category.</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Letter grade.</summary>
    public string Grade { get; set; } = string.Empty;

    /// <summary>Explanation of the grade.</summary>
    public string Rationale { get; set; } = string.Empty;
}

/// <summary>A slow per-case latency within a scenario.</summary>
public sealed class Hotspot
{
    /// <summary>Case identifier (e.g. query case name).</summary>
    public string Case { get; set; } = string.Empty;

    /// <summary>Average latency in milliseconds.</summary>
    public double? Avg { get; set; }

    /// <summary>95th-percentile latency in milliseconds.</summary>
    public double? P95 { get; set; }

    /// <summary>99th-percentile latency in milliseconds.</summary>
    public double? P99 { get; set; }
}
