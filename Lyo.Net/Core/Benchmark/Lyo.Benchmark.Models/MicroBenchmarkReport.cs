namespace Lyo.Benchmark.Models;

/// <summary>
/// Micro-benchmark report (BenchmarkDotNet). <see cref="Groups" /> follow benchmark classes; <see cref="Comparison" /> is filled only when a suite opts into a comparison
/// axis (an algorithm shoot-out, for example).
/// </summary>
public sealed class MicroBenchmarkReport : BenchmarkReport
{
    /// <summary>Benchmark classes plus their per-method measurements.</summary>
    public List<BenchmarkGroup> Groups { get; set; } = [];

    /// <summary>Optional algorithm/strategy comparison table (size × algorithm), driven by comparison-axis metadata.</summary>
    public ComparisonTable? Comparison { get; set; }

    /// <summary>SLA / business-standard rows rolled up from benchmarks that declare a budget.</summary>
    public List<SloRow> Slo { get; set; } = [];

    /// <summary>Optional letter grades with rationales (same shape as the load report).</summary>
    public List<GradeRow> Grades { get; set; } = [];
}

/// <summary>One benchmark class and the measurements it produced.</summary>
public sealed class BenchmarkGroup
{
    /// <summary>Benchmark class name (namespace omitted).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>What this class measures and how (the scenario behind the numbers).</summary>
    public string? Description { get; set; }

    /// <summary>Explanations and units for parameters this class exercises (for example <c>DataSize</c> in bytes).</summary>
    public List<ParameterDescriptor> Parameters { get; set; } = [];

    /// <summary>Shape of the data the benchmark used (columns, types, nesting), when a shape type is declared.</summary>
    public DatasetDescriptor? Dataset { get; set; }

    /// <summary>Measurements for every (method × parameter-set) case in the class.</summary>
    public List<BenchmarkMeasurement> Measurements { get; set; } = [];
}

/// <summary>One benchmark case: a method at one parameter combination.</summary>
public sealed class BenchmarkMeasurement
{
    /// <summary>Name of the benchmark method.</summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>What this method exercises.</summary>
    public string? Description { get; set; }

    /// <summary>Parameter values for this case (name → stringified value), for example <c>DataSize -> 1048576</c>.</summary>
    public Dictionary<string, string?> Parameters { get; set; } = [];

    /// <summary>Mean runtime in nanoseconds.</summary>
    public double MeanNs { get; set; }

    /// <summary>Standard deviation in nanoseconds.</summary>
    public double? StdDevNs { get; set; }

    /// <summary>Managed bytes allocated per operation.</summary>
    public double? AllocatedBytes { get; set; }

    /// <summary>This case's mean divided by the group baseline (1.0 is the baseline), when a baseline exists in the logical group.</summary>
    public double? RatioToBaseline { get; set; }

    /// <summary>True when this case is the baseline of its logical group.</summary>
    public bool IsBaseline { get; set; }

    /// <summary>Comparison axis label (for example <c>Encrypt</c>), set when the method is on the comparison table.</summary>
    public string? Axis { get; set; }

    /// <summary>Throughput in MB/s for size-based benchmarks (bytes processed / mean), when a size parameter is known.</summary>
    public double? ThroughputMbps { get; set; }

    /// <summary>SLA budget display string (for example <c>&lt;= 2 ms</c>, <c>&gt;= 300 MB/s</c>), when a budget is declared.</summary>
    public string? SlaTarget { get; set; }

    /// <summary>SLA verdict: <c>Meets</c> / <c>Exceeds</c> / <c>Miss</c>, when a budget is declared.</summary>
    public string? SlaResult { get; set; }

    /// <summary>Business-standard citation behind the SLA budget.</summary>
    public string? SlaStandard { get; set; }
}

/// <summary>Comparison table that groups comparable measurements by axis (operation) and parameter.</summary>
public sealed class ComparisonTable
{
    /// <summary>Baseline algorithm/strategy name (other rows are ratio'd against it).</summary>
    public string? Baseline { get; set; }

    /// <summary>What the comparison contrasts, and under which conditions.</summary>
    public string? Description { get; set; }

    /// <summary>Explanations and units for the parameters that label comparison rows (for example <c>DataSize</c> in bytes).</summary>
    public List<ParameterDescriptor> Parameters { get; set; } = [];

    /// <summary>One group per comparison axis (Encrypt, Decrypt, and similar).</summary>
    public List<ComparisonGroup> Groups { get; set; } = [];
}

/// <summary>Comparison rows for one axis (operation).</summary>
public sealed class ComparisonGroup
{
    /// <summary>Axis label, for example <c>Encrypt</c> / <c>Hash</c>.</summary>
    public string Axis { get; set; } = string.Empty;

    /// <summary>Rows for this axis across algorithms and parameter sets.</summary>
    public List<ComparisonRow> Rows { get; set; } = [];
}

/// <summary>One algorithm at one parameter set inside a <see cref="ComparisonGroup" />.</summary>
public sealed class ComparisonRow
{
    /// <summary>Algorithm/strategy name (method name with the axis suffix stripped).</summary>
    public string Algorithm { get; set; } = string.Empty;

    /// <summary>Parameter values for this row (name → stringified value).</summary>
    public Dictionary<string, string?> Parameters { get; set; } = [];

    /// <summary>Readable label for the primary parameter (for example <c>1 MB</c>); used to group rows in the UI.</summary>
    public string? ParamLabel { get; set; }

    /// <summary>Mean runtime in nanoseconds.</summary>
    public double MeanNs { get; set; }

    /// <summary>Managed bytes allocated per operation.</summary>
    public double? AllocatedBytes { get; set; }

    /// <summary>This row's mean divided by the baseline algorithm at the same parameter set.</summary>
    public double? RatioToBaseline { get; set; }

    /// <summary>Throughput in MB/s for size-based comparisons, when a size parameter is known.</summary>
    public double? ThroughputMbps { get; set; }

    /// <summary>SLA budget display string, when a budget is declared.</summary>
    public string? SlaTarget { get; set; }

    /// <summary>SLA verdict: <c>Meets</c> / <c>Exceeds</c> / <c>Miss</c>, when a budget is declared.</summary>
    public string? SlaResult { get; set; }
}

/// <summary>Documents a benchmark parameter so values such as <c>DataSize = 1048576</c> make sense.</summary>
public sealed class ParameterDescriptor
{
    /// <summary>Parameter name as it appears in <see cref="BenchmarkMeasurement.Parameters" /> (for example <c>DataSize</c>).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Unit of the values, for example <c>bytes</c> / <c>rows</c> / <c>items</c>.</summary>
    public string? Unit { get; set; }

    /// <summary>What the parameter changes and why that matters.</summary>
    public string? Description { get; set; }
}

/// <summary>Shape of the data a benchmark used (the "what was tested" structure).</summary>
public sealed class DatasetDescriptor
{
    /// <summary>CLR type name of the row/record/model (namespace omitted).</summary>
    public string TypeName { get; set; } = string.Empty;

    /// <summary>Count of top-level columns/properties.</summary>
    public int ColumnCount { get; set; }

    /// <summary>Deepest object/collection nesting (0 means flat).</summary>
    public int MaxNestingDepth { get; set; }

    /// <summary>Top-level columns, each with type and kind (plus nested children when present).</summary>
    public List<ColumnDescriptor> Columns { get; set; } = [];

    /// <summary>Free-form note on the data set (generation strategy, value ranges, and similar).</summary>
    public string? Notes { get; set; }
}

/// <summary>One column or property inside a <see cref="DatasetDescriptor" />.</summary>
public sealed class ColumnDescriptor
{
    /// <summary>Name of the property.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Friendly type name, for example <c>int</c>, <c>string</c>, <c>DateTime</c>, or a nested type name.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Column shape: <c>scalar</c>, <c>object</c>, or <c>collection</c>.</summary>
    public string Kind { get; set; } = "scalar";

    /// <summary>Nested columns for <c>object</c>/<c>collection</c> kinds (element shape for collections).</summary>
    public List<ColumnDescriptor>? Children { get; set; }
}