namespace Lyo.Benchmark.Models;

/// <summary>
/// Micro-benchmark report (BenchmarkDotNet). <see cref="Groups" /> mirror benchmark classes; <see cref="Comparison" /> is populated only when a suite opts into a comparison
/// axis (e.g. an algorithm shoot-out).
/// </summary>
public sealed class MicroBenchmarkReport : BenchmarkReport
{
    /// <summary>Benchmark classes and their per-method measurements.</summary>
    public List<BenchmarkGroup> Groups { get; set; } = [];

    /// <summary>Optional algorithm/strategy comparison table (size x algorithm), driven by comparison-axis metadata.</summary>
    public ComparisonTable? Comparison { get; set; }

    /// <summary>SLA / business-standard assessment rows aggregated from benchmarks that declare a budget.</summary>
    public List<SloRow> Slo { get; set; } = [];

    /// <summary>Letter grades with rationales (optional, mirrors the load report).</summary>
    public List<GradeRow> Grades { get; set; } = [];
}

/// <summary>A benchmark class and the measurements it produced.</summary>
public sealed class BenchmarkGroup
{
    /// <summary>Benchmark class name (without namespace).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>What this class measures and how (the scenario behind its measurements).</summary>
    public string? Description { get; set; }

    /// <summary>Explanations and units for the parameters exercised by this class (e.g. <c>DataSize</c> in bytes).</summary>
    public List<ParameterDescriptor> Parameters { get; set; } = [];

    /// <summary>Structure of the data the benchmark operated on (columns, types, nesting), when a shape type is declared.</summary>
    public DatasetDescriptor? Dataset { get; set; }

    /// <summary>Measurements for every (method x parameter-set) case in the class.</summary>
    public List<BenchmarkMeasurement> Measurements { get; set; } = [];
}

/// <summary>A single benchmark case: one method at one parameter combination.</summary>
public sealed class BenchmarkMeasurement
{
    /// <summary>Benchmark method name.</summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>What this specific benchmark method exercises.</summary>
    public string? Description { get; set; }

    /// <summary>Parameter values for this case (name -> stringified value), e.g. <c>DataSize -> 1048576</c>.</summary>
    public Dictionary<string, string?> Parameters { get; set; } = [];

    /// <summary>Mean execution time in nanoseconds.</summary>
    public double MeanNs { get; set; }

    /// <summary>Standard deviation in nanoseconds.</summary>
    public double? StdDevNs { get; set; }

    /// <summary>Managed bytes allocated per operation.</summary>
    public double? AllocatedBytes { get; set; }

    /// <summary>Ratio of this case's mean to its baseline (1.0 = baseline), when a baseline exists in the logical group.</summary>
    public double? RatioToBaseline { get; set; }

    /// <summary>Whether this case is the baseline of its logical group.</summary>
    public bool IsBaseline { get; set; }

    /// <summary>Comparison axis label (e.g. <c>Encrypt</c>), set when the method participates in the comparison table.</summary>
    public string? Axis { get; set; }

    /// <summary>Derived throughput in MB/s for size-based benchmarks (bytes processed / mean), when a size parameter is known.</summary>
    public double? ThroughputMbps { get; set; }

    /// <summary>SLA budget display string (e.g. <c>&lt;= 2 ms</c>, <c>&gt;= 300 MB/s</c>), when a budget is declared.</summary>
    public string? SlaTarget { get; set; }

    /// <summary>SLA verdict: <c>Meets</c> / <c>Exceeds</c> / <c>Miss</c>, when a budget is declared.</summary>
    public string? SlaResult { get; set; }

    /// <summary>Business-standard reference text behind the SLA budget.</summary>
    public string? SlaStandard { get; set; }
}

/// <summary>Comparison table grouping comparable measurements by axis (operation) and parameter.</summary>
public sealed class ComparisonTable
{
    /// <summary>Baseline algorithm/strategy name (rows are ratio'd against it).</summary>
    public string? Baseline { get; set; }

    /// <summary>What the comparison contrasts and under what conditions.</summary>
    public string? Description { get; set; }

    /// <summary>Explanations and units for the parameters that label the comparison rows (e.g. <c>DataSize</c> in bytes).</summary>
    public List<ParameterDescriptor> Parameters { get; set; } = [];

    /// <summary>One group per comparison axis (e.g. Encrypt, Decrypt).</summary>
    public List<ComparisonGroup> Groups { get; set; } = [];
}

/// <summary>All comparison rows for a single axis (operation).</summary>
public sealed class ComparisonGroup
{
    /// <summary>Axis label, e.g. <c>Encrypt</c> / <c>Hash</c>.</summary>
    public string Axis { get; set; } = string.Empty;

    /// <summary>Rows for this axis across algorithms and parameter sets.</summary>
    public List<ComparisonRow> Rows { get; set; } = [];
}

/// <summary>One algorithm at one parameter set within a <see cref="ComparisonGroup" />.</summary>
public sealed class ComparisonRow
{
    /// <summary>Algorithm/strategy name (method name with the axis suffix removed).</summary>
    public string Algorithm { get; set; } = string.Empty;

    /// <summary>Parameter values for this row (name -> stringified value).</summary>
    public Dictionary<string, string?> Parameters { get; set; } = [];

    /// <summary>Human-readable label for the primary parameter (e.g. <c>1 MB</c>); used to group rows in the UI.</summary>
    public string? ParamLabel { get; set; }

    /// <summary>Mean execution time in nanoseconds.</summary>
    public double MeanNs { get; set; }

    /// <summary>Managed bytes allocated per operation.</summary>
    public double? AllocatedBytes { get; set; }

    /// <summary>Ratio of this row's mean to the baseline algorithm at the same parameter set.</summary>
    public double? RatioToBaseline { get; set; }

    /// <summary>Derived throughput in MB/s for size-based comparisons, when a size parameter is known.</summary>
    public double? ThroughputMbps { get; set; }

    /// <summary>SLA budget display string, when a budget is declared.</summary>
    public string? SlaTarget { get; set; }

    /// <summary>SLA verdict: <c>Meets</c> / <c>Exceeds</c> / <c>Miss</c>, when a budget is declared.</summary>
    public string? SlaResult { get; set; }
}

/// <summary>Explains a benchmark parameter so values like <c>DataSize = 1048576</c> are meaningful.</summary>
public sealed class ParameterDescriptor
{
    /// <summary>Parameter name as it appears in <see cref="BenchmarkMeasurement.Parameters" /> (e.g. <c>DataSize</c>).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Unit of the parameter values, e.g. <c>bytes</c> / <c>rows</c> / <c>items</c>.</summary>
    public string? Unit { get; set; }

    /// <summary>What the parameter controls and why it matters.</summary>
    public string? Description { get; set; }
}

/// <summary>Describes the structure of the data a benchmark operated on (the "what was tested" shape).</summary>
public sealed class DatasetDescriptor
{
    /// <summary>CLR type name of the row/record/model (without namespace).</summary>
    public string TypeName { get; set; } = string.Empty;

    /// <summary>Number of top-level columns/properties.</summary>
    public int ColumnCount { get; set; }

    /// <summary>Deepest level of object/collection nesting (0 = flat).</summary>
    public int MaxNestingDepth { get; set; }

    /// <summary>Top-level columns, each with its type and kind (and nested children where applicable).</summary>
    public List<ColumnDescriptor> Columns { get; set; } = [];

    /// <summary>Free-form note about the data set (e.g. generation strategy, value ranges).</summary>
    public string? Notes { get; set; }
}

/// <summary>One column/property within a <see cref="DatasetDescriptor" />.</summary>
public sealed class ColumnDescriptor
{
    /// <summary>Property name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Friendly type name, e.g. <c>int</c>, <c>string</c>, <c>DateTime</c>, or a nested type name.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Shape of the column: <c>scalar</c>, <c>object</c>, or <c>collection</c>.</summary>
    public string Kind { get; set; } = "scalar";

    /// <summary>Nested columns for <c>object</c>/<c>collection</c> kinds (the element shape for collections).</summary>
    public List<ColumnDescriptor>? Children { get; set; }
}