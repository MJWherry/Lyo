using System.Text.Json.Serialization;

namespace Lyo.Benchmark.Models;

/// <summary>
/// Polymorphic root of a benchmark report. The <c>type</c> discriminator selects the concrete shape: <see cref="MicroBenchmarkReport" /> (<c>micro</c>) for BenchmarkDotNet
/// micro-benchmarks or <see cref="LoadTestReport" /> (<c>load</c>) for k6-style load tests. Only the envelope below is shared; each subtype carries its own divergent payload (mirrors
/// the <c>WhereClause</c> AST in Lyo.Query.Models).
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(MicroBenchmarkReport), "micro")]
[JsonDerivedType(typeof(LoadTestReport), "load")]
public abstract class BenchmarkReport
{
    /// <summary>Schema version of this document, e.g. <see cref="BenchmarkSchema.Current" /> (<c>lyo.bench/v1</c>).</summary>
    public string Schema { get; set; } = BenchmarkSchema.Current;

    /// <summary>Stable machine name for the suite/category (e.g. <c>hashing</c>, <c>query-api</c>); used as the data file stem.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Human-readable display title for the suite.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Suite-level methodology: what is measured and how (e.g. the data set, payload kinds, and what each benchmark exercises). Rendered as an introduction so individual rows
    /// are self-explanatory.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>Identifier of the run that produced this report (BenchmarkDotNet run id or k6 results folder name).</summary>
    public string RunId { get; set; } = string.Empty;

    /// <summary>UTC timestamp the report was generated.</summary>
    public DateTimeOffset GeneratedAt { get; set; }

    /// <summary>UTC timestamp the benchmark run started, when known.</summary>
    public DateTimeOffset? RunStarted { get; set; }

    /// <summary>UTC timestamp the benchmark run finished, when known.</summary>
    public DateTimeOffset? RunEnded { get; set; }

    /// <summary>Total wall-clock duration of the run, in seconds, when known.</summary>
    public double? DurationSeconds { get; set; }

    /// <summary>Tool / host / hardware environment that produced the measurements.</summary>
    public BenchmarkEnvironment? Environment { get; set; }

    /// <summary>Free-form caveats, methodology notes, or warnings rendered alongside the data.</summary>
    public List<string> Notes { get; set; } = [];
}

/// <summary>Schema version constants for <see cref="BenchmarkReport.Schema" />.</summary>
public static class BenchmarkSchema
{
    /// <summary>Current benchmark report schema version.</summary>
    public const string Current = "lyo.bench/v1";
}