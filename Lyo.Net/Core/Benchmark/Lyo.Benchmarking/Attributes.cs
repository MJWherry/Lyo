namespace Lyo.Benchmarking;

/// <summary>
/// Assembly-level metadata for a benchmark project: the stable machine <see cref="Name" /> and display <see cref="Title" /> used in the emitted report. When absent, the
/// exporter derives the name from the assembly name (e.g. <c>Lyo.Hashing.Benchmarks</c> -&gt; <c>hashing</c>).
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class BenchmarkReportAttribute : Attribute
{
    /// <summary>Stable machine name (data file stem), e.g. <c>hashing</c>.</summary>
    public string Name { get; }

    /// <summary>Human-readable display title, e.g. <c>Hashing</c>.</summary>
    public string Title { get; }

    /// <summary>
    /// Suite-level methodology copied into <c>MicroBenchmarkReport.Description</c>: what is measured, the data set, and what each benchmark exercises so individual rows are
    /// self-explanatory.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>Creates report metadata with the given machine name and display title.</summary>
    public BenchmarkReportAttribute(string name, string title)
    {
        Name = name;
        Title = title;
    }
}

/// <summary>
/// Human-readable description of what a benchmark class or method exercises. Picked up by the exporter and written into the report's group / measurement <c>description</c>
/// so readers do not have to infer intent from method names.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class BenchmarkDescriptionAttribute : Attribute
{
    /// <summary>Description text.</summary>
    public string Text { get; }

    /// <summary>Creates a description annotation.</summary>
    /// <param name="text">What the class/method measures and how.</param>
    public BenchmarkDescriptionAttribute(string text) => Text = text;
}

/// <summary>
/// Explains a <see cref="BenchmarkDotNet.Attributes.ParamsAttribute" /> parameter so reported values are meaningful (e.g. <c>DataSize = 1048576</c> is "1 MB of bytes").
/// Apply once per parameter on the benchmark class.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class BenchmarkParameterAttribute : Attribute
{
    /// <summary>Parameter name.</summary>
    public string Name { get; }

    /// <summary>Unit of the parameter values, e.g. <c>bytes</c> / <c>rows</c> / <c>items</c>.</summary>
    public string? Unit { get; set; }

    /// <summary>What the parameter controls and why it matters.</summary>
    public string? Description { get; set; }

    /// <summary>Creates a parameter description.</summary>
    /// <param name="name">Parameter name as declared by the benchmark (e.g. <c>DataSize</c>).</param>
    public BenchmarkParameterAttribute(string name) => Name = name;
}

/// <summary>
/// Declares the row/record/model type a benchmark operates on. The exporter reflects over it to emit a structured <c>DatasetDescriptor</c> (columns, types, nesting depth) so
/// the report captures the data structure - including nested complexity - rather than just a row count.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class BenchmarkDataShapeAttribute : Attribute
{
    /// <summary>The reflected row/model type.</summary>
    public Type RowType { get; }

    /// <summary>Optional note about the data set (e.g. value ranges or generation strategy).</summary>
    public string? Notes { get; set; }

    /// <summary>Creates a data-shape annotation.</summary>
    /// <param name="rowType">The type whose public properties describe the data structure.</param>
    public BenchmarkDataShapeAttribute(Type rowType) => RowType = rowType;
}

/// <summary>
/// Declares a service-level objective / business-standard budget for a benchmark. Apply on a method (wins) or on the class (default for every method). The exporter compares
/// the measured mean / allocation / throughput against the declared budgets and emits a <c>Meets</c> / <c>Exceeds</c> / <c>Miss</c> verdict plus a target display string, so reported
/// numbers can be judged against an expectation rather than read in isolation.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class BenchmarkSlaAttribute : Attribute
{
    /// <summary>Latency budget in milliseconds (mean per operation must be at or under this). 0 = unset.</summary>
    public double MaxMeanMs { get; set; }

    /// <summary>Latency budget in microseconds. 0 = unset.</summary>
    public double MaxMeanUs { get; set; }

    /// <summary>Latency budget in nanoseconds. 0 = unset.</summary>
    public double MaxMeanNs { get; set; }

    /// <summary>Minimum sustained throughput in MB/s; evaluated only for size-based suites (see <see cref="SizeParam" />). 0 = unset.</summary>
    public double MinThroughputMbps { get; set; }

    /// <summary>
    /// Smallest payload (in bytes) at which the <see cref="MinThroughputMbps" /> floor is judged. Below this size the throughput target is skipped (not graded a Miss), because
    /// small payloads are dominated by fixed per-call overhead and cannot represent sustained bulk throughput. 0 = always evaluate. Typical value: 65536 (64 KB).
    /// </summary>
    public double MinThroughputSizeBytes { get; set; }

    /// <summary>Name of the parameter carrying the processed byte size, used to derive throughput (default <c>DataSize</c>).</summary>
    public string SizeParam { get; set; } = "DataSize";

    /// <summary>Allocation budget in kilobytes (allocated-per-op must be at or under this). 0 = unset.</summary>
    public double MaxAllocatedKb { get; set; }

    /// <summary>Business-standard reference text explaining where the budget comes from (e.g. an industry throughput norm).</summary>
    public string? Standard { get; set; }
}

/// <summary>
/// Marks the benchmark class that drives the report's comparison table (replaces the old magic <c>AlgorithmComparisonBenchmarks</c> class-name convention). Methods opt into
/// axes with <see cref="ComparisonAxisAttribute" />.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ComparisonSuiteAttribute : Attribute
{
    /// <summary>Optional baseline algorithm name; falls back to the BenchmarkDotNet baseline of the logical group.</summary>
    public string? Baseline { get; set; }
}

/// <summary>
/// Declares that a benchmark method participates in the comparison table under the given <see cref="Axis" /> (e.g. <c>Encrypt</c>, <c>Hash</c>), with the algorithm name
/// taken from the remainder of the method name (replaces the old <c>_Encrypt</c>/<c>_Hash</c> suffix string-matching).
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class ComparisonAxisAttribute : Attribute
{
    /// <summary>Axis/operation label.</summary>
    public string Axis { get; }

    /// <summary>
    /// Optional explicit algorithm name. When null, the algorithm is derived by stripping the axis suffix from the method name (e.g. <c>Sha256_Hash</c> with axis <c>Hash</c> -
    /// &gt; <c>Sha256</c>).
    /// </summary>
    public string? Algorithm { get; set; }

    /// <summary>Creates a comparison-axis annotation.</summary>
    /// <param name="axis">Axis/operation label, e.g. <c>Encrypt</c>.</param>
    public ComparisonAxisAttribute(string axis) => Axis = axis;
}