using System.Text.Json.Serialization;

namespace Lyo.Benchmark.Models;

/// <summary>
/// Polymorphic root of a benchmark report. The <c>type</c> discriminator picks the concrete shape: <see cref="MicroBenchmarkReport" /> (<c>micro</c>) for BenchmarkDotNet
/// micro-benchmarks, or <see cref="LoadTestReport" /> (<c>load</c>) for k6-style load tests. Only the envelope below is shared; each subtype carries its own payload (same idea as
/// the <c>WhereClause</c> AST in Lyo.Query.Models).
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(MicroBenchmarkReport), "micro")]
[JsonDerivedType(typeof(LoadTestReport), "load")]
public abstract class BenchmarkReport
{
    /// <summary>Schema version of this document, for example <see cref="BenchmarkSchema.Current" /> (<c>lyo.bench/v1</c>).</summary>
    public string Schema { get; set; } = BenchmarkSchema.Current;

    /// <summary>Stable machine name for the suite or category (for example <c>hashing</c>, <c>query-api</c>); used as the data file stem.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Display title for the suite.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Suite-level methodology: what is measured and how (data set, payload kinds, and what each benchmark exercises). Shown as an introduction so individual rows can
    /// stand on their own.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>Id of the run that produced this report (BenchmarkDotNet run id or k6 results folder name).</summary>
    public string RunId { get; set; } = string.Empty;

    /// <summary>UTC time at which the report was written.</summary>
    public DateTimeOffset GeneratedAt { get; set; }

    /// <summary>UTC time the benchmark run began, when known.</summary>
    public DateTimeOffset? RunStarted { get; set; }

    /// <summary>UTC time the benchmark run finished, when known.</summary>
    public DateTimeOffset? RunEnded { get; set; }

    /// <summary>Wall-clock length of the run in seconds, when known.</summary>
    public double? DurationSeconds { get; set; }

    /// <summary>Tool, host, and hardware that produced the measurements.</summary>
    public BenchmarkEnvironment? Environment { get; set; }

    /// <summary>Free-form caveats, methodology notes, or warnings shown next to the data.</summary>
    public List<string> Notes { get; set; } = [];
}

/// <summary>Schema version constants used by <see cref="BenchmarkReport.Schema" />.</summary>
public static class BenchmarkSchema
{
    /// <summary>Schema version currently written by exporters.</summary>
    public const string Current = "lyo.bench/v1";
}