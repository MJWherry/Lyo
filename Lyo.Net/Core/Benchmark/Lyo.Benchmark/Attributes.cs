namespace Lyo.Benchmark;

/// <summary>
/// Assembly metadata for a benchmark project: the stable machine <see cref="Name" /> and display <see cref="Title" /> written into the report. If this attribute is missing,
/// the exporter derives the name from the assembly (for example <c>Lyo.Hashing.Benchmarks</c> -&gt; <c>hashing</c>).
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class BenchmarkReportAttribute : Attribute
{
    /// <summary>Stable machine name (file stem), for example <c>hashing</c>.</summary>
    public string Name { get; }

    /// <summary>Title shown to people, for example <c>Hashing</c>.</summary>
    public string Title { get; }

    /// <summary>
    /// Suite-level methodology copied into <c>MicroBenchmarkReport.Description</c>: what is measured, which data set, and what each benchmark exercises so a row can stand
    /// on its own.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>Stores report metadata for the given machine name and display title.</summary>
    public BenchmarkReportAttribute(string name, string title)
    {
        Name = name;
        Title = title;
    }
}

/// <summary>
/// Plain-language note of what a benchmark class or method exercises. The exporter copies it into the report's group / measurement <c>description</c> so readers do not
/// have to guess intent from method names.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class BenchmarkDescriptionAttribute : Attribute
{
    /// <summary>Description body.</summary>
    public string Text { get; }

    /// <summary>Attaches a description.</summary>
    /// <param name="text">What the class or method measures, and how.</param>
    public BenchmarkDescriptionAttribute(string text) => Text = text;
}

/// <summary>
/// Documents a <see cref="BenchmarkDotNet.Attributes.ParamsAttribute" /> so reported values make sense (for example <c>DataSize = 1048576</c> means "1 MB of bytes").
/// Put one on the benchmark class for each parameter.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class BenchmarkParameterAttribute : Attribute
{
    /// <summary>Name of the parameter.</summary>
    public string Name { get; }

    /// <summary>Unit of the values, for example <c>bytes</c> / <c>rows</c> / <c>items</c>.</summary>
    public string? Unit { get; set; }

    /// <summary>What the parameter changes and why that matters.</summary>
    public string? Description { get; set; }

    /// <summary>Attaches a parameter description.</summary>
    /// <param name="name">Parameter name as the benchmark declares it (for example <c>DataSize</c>).</param>
    public BenchmarkParameterAttribute(string name) => Name = name;
}

/// <summary>
/// Names the row/record/model type a benchmark works on. The exporter reflects over it and writes a structured <c>DatasetDescriptor</c> (columns, types, nesting depth) so
/// the report shows the data shape — nested complexity included — not only a row count.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class BenchmarkDataShapeAttribute : Attribute
{
    /// <summary>Row/model type used for reflection.</summary>
    public Type RowType { get; }

    /// <summary>Optional note on the data set (value ranges, generation strategy, and similar).</summary>
    public string? Notes { get; set; }

    /// <summary>Attaches a data-shape annotation.</summary>
    /// <param name="rowType">Type whose public properties describe the structure.</param>
    public BenchmarkDataShapeAttribute(Type rowType) => RowType = rowType;
}

/// <summary>
/// Declares an SLA / business-standard budget for a benchmark. Put it on a method (that wins) or on the class (default for every method). The exporter compares measured
/// mean / allocation / throughput to those budgets and writes a <c>Meets</c> / <c>Exceeds</c> / <c>Miss</c> verdict plus a target display string, so numbers are judged against
/// an expectation instead of being read in isolation.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class BenchmarkSlaAttribute : Attribute
{
    /// <summary>Latency budget in milliseconds (mean per operation must stay at or under this). 0 means unset.</summary>
    public double MaxMeanMs { get; set; }

    /// <summary>Latency budget in microseconds. 0 means unset.</summary>
    public double MaxMeanUs { get; set; }

    /// <summary>Latency budget in nanoseconds. 0 means unset.</summary>
    public double MaxMeanNs { get; set; }

    /// <summary>Minimum sustained throughput in MB/s; only applied on size-based suites (see <see cref="SizeParam" />). 0 means unset.</summary>
    public double MinThroughputMbps { get; set; }

    /// <summary>
    /// Smallest payload in bytes at which the <see cref="MinThroughputMbps" /> floor is judged. Below that size the throughput target is skipped (not graded a Miss), because
    /// small payloads are dominated by fixed per-call overhead and cannot represent sustained bulk throughput. 0 means always evaluate. Typical value: 65536 (64 KB).
    /// </summary>
    public double MinThroughputSizeBytes { get; set; }

    /// <summary>Parameter that holds the processed byte size, used to derive throughput (default <c>DataSize</c>).</summary>
    public string SizeParam { get; set; } = "DataSize";

    /// <summary>Allocation budget in kilobytes (allocated-per-op must stay at or under this). 0 means unset.</summary>
    public double MaxAllocatedKb { get; set; }

    /// <summary>Business-standard citation for where the budget comes from (for example an industry throughput norm).</summary>
    public string? Standard { get; set; }
}

/// <summary>
/// Marks the benchmark class that owns the report's comparison table (replaces the old magic <c>AlgorithmComparisonBenchmarks</c> class-name convention). Methods join
/// axes via <see cref="ComparisonAxisAttribute" />.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ComparisonSuiteAttribute : Attribute
{
    /// <summary>Optional baseline algorithm name; otherwise the BenchmarkDotNet baseline of the logical group is used.</summary>
    public string? Baseline { get; set; }
}

/// <summary>
/// Puts a benchmark method on the comparison table under the given <see cref="Axis" /> (for example <c>Encrypt</c>, <c>Hash</c>). The algorithm name is taken from the
/// rest of the method name (replaces the old <c>_Encrypt</c>/<c>_Hash</c> suffix matching).
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class ComparisonAxisAttribute : Attribute
{
    /// <summary>Axis or operation label.</summary>
    public string Axis { get; }

    /// <summary>
    /// Optional explicit algorithm name. When null, the name is derived by stripping the axis suffix from the method (for example <c>Sha256_Hash</c> with axis <c>Hash</c> -
    /// &gt; <c>Sha256</c>).
    /// </summary>
    public string? Algorithm { get; set; }

    /// <summary>Attaches a comparison-axis annotation.</summary>
    /// <param name="axis">Axis or operation label, for example <c>Encrypt</c>.</param>
    public ComparisonAxisAttribute(string axis) => Axis = axis;
}