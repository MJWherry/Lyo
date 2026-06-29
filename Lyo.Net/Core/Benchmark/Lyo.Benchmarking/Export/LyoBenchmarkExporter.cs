using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Reports;
using Lyo.Benchmark.Models;
using Lyo.Benchmark.Models.Builders;
using ModelReport = Lyo.Benchmark.Models.BenchmarkReport;
using BdnReport = BenchmarkDotNet.Reports.BenchmarkReport;

namespace Lyo.Benchmarking.Export;

/// <summary>
/// BenchmarkDotNet exporter that normalizes a run into the unified <see cref="MicroBenchmarkReport" /> schema and
/// writes <c>&lt;name&gt;.lyobench.json</c> into the artifacts directory. Reads structured statistics directly
/// (nanosecond means, GC bytes, parameters) so no downstream string reparsing is needed.
/// </summary>
public sealed class LyoBenchmarkExporter : IExporter
{
    /// <summary>Singleton instance used by the shared config.</summary>
    public static readonly LyoBenchmarkExporter Default = new();

    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    /// <inheritdoc />
    public string Name => "lyobench";

    /// <inheritdoc />
    public void ExportToLog(Summary summary, ILogger logger)
    {
        // No console output; this exporter only produces a file.
    }

    /// <inheritdoc />
    public IEnumerable<string> ExportToFiles(Summary summary, ILogger consoleLogger)
    {
        var report = Build(summary);
        var artifactsDir = Path.GetDirectoryName(summary.ResultsDirectoryPath) ?? summary.ResultsDirectoryPath;
        Directory.CreateDirectory(artifactsDir);
        var path = Path.Combine(artifactsDir, $"{report.Name}.lyobench.json");
        File.WriteAllText(path, JsonSerializer.Serialize<ModelReport>(report, JsonOptions));
        return [path];
    }

    /// <summary>Builds the normalized report from a BenchmarkDotNet summary (exposed for testing).</summary>
    public static MicroBenchmarkReport Build(Summary summary)
    {
        var assembly = summary.BenchmarksCases.Select(c => c.Descriptor.Type.Assembly).FirstOrDefault();
        var meta = assembly?.GetCustomAttribute<BenchmarkReportAttribute>();
        var (name, title) = ResolveNameAndTitle(meta, assembly);

        var builder = MicroBenchmarkReportBuilder.Create(name, title)
            .WithDescription(meta?.Description)
            .WithRun(summary.Title)
            .WithEnvironment(ReadEnvironment(summary));

        var measurementByCase = new Dictionary<BenchmarkReportRow, BenchmarkMeasurement>();
        var rows = new List<BenchmarkReportRow>();

        foreach (var bdnReport in summary.Reports) {
            var benchmarkCase = bdnReport.BenchmarkCase;
            var mean = bdnReport.ResultStatistics?.Mean ?? double.NaN;
            if (double.IsNaN(mean))
                continue;

            var row = new BenchmarkReportRow {
                ClassName = benchmarkCase.Descriptor.Type.Name,
                Method = benchmarkCase.Descriptor.WorkloadMethod.Name,
                Parameters = ReadParameters(benchmarkCase),
                MeanNs = mean,
                StdDevNs = bdnReport.ResultStatistics?.StandardDeviation,
                AllocatedBytes = ReadAllocated(bdnReport),
                IsBaseline = benchmarkCase.Descriptor.Baseline,
                WorkloadMethod = benchmarkCase.Descriptor.WorkloadMethod,
                BenchmarkType = benchmarkCase.Descriptor.Type,
            };
            rows.Add(row);
        }

        ComputeRatios(rows);

        // Aggregates the worst-case SLA verdict per (class, method) for the suite-level SLO summary.
        var sloAggregates = new Dictionary<(string, string), SloAggregate>();

        foreach (var row in rows) {
            var measurement = new BenchmarkMeasurement {
                Method = row.Method,
                Description = row.WorkloadMethod?.GetCustomAttribute<BenchmarkDescriptionAttribute>()?.Text,
                Parameters = row.Parameters,
                MeanNs = row.MeanNs,
                StdDevNs = row.StdDevNs,
                AllocatedBytes = row.AllocatedBytes,
                IsBaseline = row.IsBaseline,
                RatioToBaseline = row.RatioToBaseline,
            };

            var sla = ResolveSla(row);
            var evaluation = EvaluateSla(row, sla);
            if (evaluation is not null) {
                measurement.ThroughputMbps = evaluation.ThroughputMbps;
                measurement.SlaTarget = evaluation.Target;
                measurement.SlaResult = evaluation.Result;
                measurement.SlaStandard = evaluation.Standard;
                Accumulate(sloAggregates, row, evaluation);
            }

            measurementByCase[row] = measurement;
            builder.AddMeasurement(row.ClassName, measurement);
        }

        DescribeGroups(summary, builder);

        var comparison = BuildComparison(summary, rows, measurementByCase);
        if (comparison is not null)
            builder.WithComparison(comparison);

        foreach (var slo in BuildSloRows(sloAggregates))
            builder.AddSlo(slo);

        return builder.Build();
    }

    private static (string Name, string Title) ResolveNameAndTitle(BenchmarkReportAttribute? meta, Assembly? assembly)
    {
        if (meta is not null)
            return (meta.Name, meta.Title);

        var assemblyName = assembly?.GetName().Name ?? "benchmarks";
        var trimmed = assemblyName;
        const string suffix = ".Benchmarks";
        if (trimmed.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed.Substring(0, trimmed.Length - suffix.Length);
        var segment = trimmed.Split('.').LastOrDefault() ?? trimmed;
        return (segment.ToLowerInvariant(), segment);
    }

    private static BenchmarkEnvironment ReadEnvironment(Summary summary)
    {
        var host = summary.HostEnvironmentInfo;
        return new BenchmarkEnvironment {
            Tool = "BenchmarkDotNet",
            ToolVersion = host.BenchmarkDotNetVersion,
            Os = RuntimeInformation.OSDescription,
            Runtime = RuntimeInformation.FrameworkDescription,
        };
    }

    private static void DescribeGroups(Summary summary, MicroBenchmarkReportBuilder builder)
    {
        var types = summary.BenchmarksCases
            .Select(c => c.Descriptor.Type)
            .GroupBy(t => t.Name)
            .Select(g => g.First());

        foreach (var type in types) {
            builder.DescribeGroup(
                type.Name,
                type.GetCustomAttribute<BenchmarkDescriptionAttribute>()?.Text,
                ReadParameterDescriptors(type),
                ReadDataset(type));
        }
    }

    private static List<ParameterDescriptor> ReadParameterDescriptors(Type type)
        => type.GetCustomAttributes<BenchmarkParameterAttribute>()
            .Select(a => new ParameterDescriptor { Name = a.Name, Unit = a.Unit, Description = a.Description })
            .ToList();

    private static DatasetDescriptor? ReadDataset(Type type)
    {
        var shape = type.GetCustomAttribute<BenchmarkDataShapeAttribute>();
        if (shape is null)
            return null;

        var columns = BuildColumns(shape.RowType, new HashSet<Type>());
        return new DatasetDescriptor {
            TypeName = FriendlyTypeName(shape.RowType),
            ColumnCount = columns.Count,
            MaxNestingDepth = MaxDepth(columns),
            Columns = columns,
            Notes = shape.Notes,
        };
    }

    private static List<ColumnDescriptor> BuildColumns(Type type, HashSet<Type> visited)
    {
        var columns = new List<ColumnDescriptor>();
        if (!visited.Add(type))
            return columns; // cycle guard

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
            if (!property.CanRead || property.GetIndexParameters().Length > 0)
                continue;
            columns.Add(DescribeProperty(property, visited));
        }

        visited.Remove(type);
        return columns;
    }

    private static ColumnDescriptor DescribeProperty(PropertyInfo property, HashSet<Type> visited)
    {
        var type = property.PropertyType;
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        var column = new ColumnDescriptor { Name = property.Name };

        if (IsScalar(underlying)) {
            column.Type = FriendlyTypeName(type);
            column.Kind = "scalar";
            return column;
        }

        var element = GetEnumerableElementType(underlying);
        if (element is not null) {
            var elementUnderlying = Nullable.GetUnderlyingType(element) ?? element;
            column.Kind = "collection";
            column.Type = FriendlyTypeName(element) + "[]";
            if (!IsScalar(elementUnderlying)) {
                var children = BuildColumns(elementUnderlying, visited);
                if (children.Count > 0)
                    column.Children = children;
            }

            return column;
        }

        column.Kind = "object";
        column.Type = FriendlyTypeName(underlying);
        var objectChildren = BuildColumns(underlying, visited);
        if (objectChildren.Count > 0)
            column.Children = objectChildren;
        return column;
    }

    private static int MaxDepth(List<ColumnDescriptor> columns)
    {
        var max = 0;
        foreach (var column in columns) {
            if (column.Children is { Count: > 0 } children)
                max = Math.Max(max, 1 + MaxDepth(children));
        }

        return max;
    }

    private static bool IsScalar(Type type)
        => type.IsPrimitive
           || type.IsEnum
           || type == typeof(string)
           || type == typeof(decimal)
           || type == typeof(DateTime)
           || type == typeof(DateTimeOffset)
           || type == typeof(TimeSpan)
           || type == typeof(Guid);

    private static Type? GetEnumerableElementType(Type type)
    {
        if (type == typeof(string))
            return null;
        if (type.IsArray)
            return type.GetElementType();
        foreach (var iface in new[] { type }.Concat(type.GetInterfaces())) {
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return iface.GetGenericArguments()[0];
        }

        return null;
    }

    private static string FriendlyTypeName(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type);
        if (underlying is not null)
            return FriendlyTypeName(underlying) + "?";

        return type switch {
            _ when type == typeof(int) => "int",
            _ when type == typeof(long) => "long",
            _ when type == typeof(short) => "short",
            _ when type == typeof(byte) => "byte",
            _ when type == typeof(bool) => "bool",
            _ when type == typeof(double) => "double",
            _ when type == typeof(float) => "float",
            _ when type == typeof(decimal) => "decimal",
            _ when type == typeof(string) => "string",
            _ => type.Name,
        };
    }

    private static Dictionary<string, string?> ReadParameters(BenchmarkDotNet.Running.BenchmarkCase benchmarkCase)
    {
        var result = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var item in benchmarkCase.Parameters.Items)
            result[item.Name] = item.Value?.ToString();
        return result;
    }

    private static double? ReadAllocated(BenchmarkDotNet.Reports.BenchmarkReport bdnReport)
    {
        try {
            var bytes = bdnReport.GcStats.GetBytesAllocatedPerOperation(bdnReport.BenchmarkCase);
            return bytes;
        }
        catch {
            return null;
        }
    }

    private static void ComputeRatios(List<BenchmarkReportRow> rows)
    {
        // Logical group = same class + same parameter signature; baseline method within it is the reference.
        foreach (var group in rows.GroupBy(r => (r.ClassName, Key: ParamKey(r.Parameters)))) {
            var baseline = group.FirstOrDefault(r => r.IsBaseline);
            if (baseline is null || baseline.MeanNs <= 0)
                continue;
            foreach (var row in group)
                row.RatioToBaseline = row.MeanNs / baseline.MeanNs;
        }
    }

    private static ComparisonTable? BuildComparison(
        Summary summary,
        List<BenchmarkReportRow> rows,
        Dictionary<BenchmarkReportRow, BenchmarkMeasurement> measurementByCase)
    {
        var suiteType = summary.BenchmarksCases
            .Select(c => c.Descriptor.Type)
            .Distinct()
            .FirstOrDefault(t => t.GetCustomAttribute<ComparisonSuiteAttribute>() is not null);

        if (suiteType is null)
            return null;

        var suiteAttr = suiteType.GetCustomAttribute<ComparisonSuiteAttribute>();
        var suiteRows = rows.Where(r => r.ClassName == suiteType.Name && r.WorkloadMethod is not null).ToList();

        var groups = new List<ComparisonGroup>();
        string? baselineName = suiteAttr?.Baseline;

        foreach (var axisGroup in GroupByAxis(suiteRows)) {
            var axis = axisGroup.Key;
            var comparisonRows = new List<ComparisonRow>();

            // Resolve baseline algorithm name from the [Benchmark(Baseline=true)] case if not explicitly set.
            if (baselineName is null) {
                var baselineEntry = axisGroup.Value.FirstOrDefault(x => x.Row.IsBaseline);
                if (baselineEntry.Row is not null)
                    baselineName = baselineEntry.Algorithm;
            }

            // Reference mean per parameter set (for the baseline algorithm).
            var baselineMeanByParam = axisGroup.Value
                .Where(x => baselineName != null && x.Algorithm == baselineName)
                .GroupBy(x => ParamKey(x.Row.Parameters))
                .ToDictionary(g => g.Key, g => g.First().Row.MeanNs);

            foreach (var entry in axisGroup.Value) {
                var paramKey = ParamKey(entry.Row.Parameters);
                double? ratio = null;
                if (baselineMeanByParam.TryGetValue(paramKey, out var baseMean) && baseMean > 0)
                    ratio = entry.Row.MeanNs / baseMean;

                var evaluation = EvaluateSla(entry.Row, ResolveSla(entry.Row));
                comparisonRows.Add(new ComparisonRow {
                    Algorithm = entry.Algorithm,
                    Parameters = entry.Row.Parameters,
                    ParamLabel = ParamLabel(entry.Row.Parameters),
                    MeanNs = entry.Row.MeanNs,
                    AllocatedBytes = entry.Row.AllocatedBytes,
                    RatioToBaseline = ratio,
                    ThroughputMbps = evaluation?.ThroughputMbps,
                    SlaTarget = evaluation?.Target,
                    SlaResult = evaluation?.Result,
                });
            }

            groups.Add(new ComparisonGroup { Axis = axis, Rows = comparisonRows });
        }

        if (groups.Count == 0)
            return null;

        return new ComparisonTable {
            Baseline = baselineName,
            Description = suiteType.GetCustomAttribute<BenchmarkDescriptionAttribute>()?.Text,
            Parameters = ReadParameterDescriptors(suiteType),
            Groups = groups,
        };
    }

    private static Dictionary<string, List<(BenchmarkReportRow Row, string Algorithm)>> GroupByAxis(List<BenchmarkReportRow> suiteRows)
    {
        var byAxis = new Dictionary<string, List<(BenchmarkReportRow, string)>>(StringComparer.Ordinal);
        foreach (var row in suiteRows) {
            var axisAttr = row.WorkloadMethod!.GetCustomAttribute<ComparisonAxisAttribute>();
            if (axisAttr is null)
                continue;
            var algorithm = axisAttr.Algorithm ?? StripAxis(row.Method, axisAttr.Axis);
            if (!byAxis.TryGetValue(axisAttr.Axis, out var list)) {
                list = [];
                byAxis[axisAttr.Axis] = list;
            }

            list.Add((row, algorithm));
        }

        return byAxis;
    }

    private static string StripAxis(string method, string axis)
    {
        if (method.EndsWith(axis, StringComparison.Ordinal))
            method = method.Substring(0, method.Length - axis.Length);
        return method.TrimEnd('_');
    }

    private static string ParamKey(Dictionary<string, string?> parameters)
        => string.Join("|", parameters.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => $"{p.Key}={p.Value}"));

    private static string? ParamLabel(Dictionary<string, string?> parameters)
    {
        if (parameters.Count == 0)
            return null;
        if (parameters.TryGetValue("DataSize", out var dataSize) && long.TryParse(dataSize, out var bytes))
            return FormatDataSize(bytes);
        var first = parameters.OrderBy(p => p.Key, StringComparer.Ordinal).First();
        return first.Value;
    }

    private static string FormatDataSize(long n)
    {
        const long mb = 1024 * 1024;
        if (n >= mb)
            return n % mb == 0 ? $"{n / mb} MB" : $"{n / (double)mb:0.0} MB";
        if (n >= 1024)
            return n % 1024 == 0 ? $"{n / 1024} KB" : $"{n / 1024.0:0.0} KB";
        return $"{n} B";
    }

    private static BenchmarkSlaAttribute? ResolveSla(BenchmarkReportRow row)
        => row.WorkloadMethod?.GetCustomAttribute<BenchmarkSlaAttribute>()
           ?? row.BenchmarkType?.GetCustomAttribute<BenchmarkSlaAttribute>();

    /// <summary>
    /// Compares a measured row against its declared SLA: builds the target display string, derives throughput when a
    /// size parameter is present, and grades the outcome Miss / Exceeds / Meets. Returns null when no budget applies.
    /// </summary>
    private static SlaEvaluation? EvaluateSla(BenchmarkReportRow row, BenchmarkSlaAttribute? sla)
    {
        if (sla is null)
            return null;

        long? sizeBytes = null;
        if (row.Parameters.TryGetValue(sla.SizeParam, out var sizeStr) && long.TryParse(sizeStr, out var parsedSize))
            sizeBytes = parsedSize;

        double? throughputMbps = null;
        if (sla.MinThroughputMbps > 0 && row.MeanNs > 0 && sizeBytes is long bytes) {
            var seconds = row.MeanNs / 1e9;
            throughputMbps = bytes / seconds / 1_000_000.0;
        }

        var targets = new List<string>();
        var evaluated = 0;
        var misses = 0;
        var comfortable = 0;

        var maxNs = LatencyBudgetNs(sla);
        if (maxNs is double budgetNs && budgetNs > 0) {
            evaluated++;
            targets.Add("<= " + FormatLatencyBudget(sla));
            if (row.MeanNs > budgetNs) misses++;
            else if (row.MeanNs <= 0.5 * budgetNs) comfortable++;
        }

        // Throughput is only a fair target for bulk payloads; below MinThroughputSizeBytes the result is
        // dominated by fixed per-call overhead, so it is skipped (not counted as a Miss) at small sizes.
        var throughputApplies = sla.MinThroughputMbps > 0
            && throughputMbps is not null
            && (sla.MinThroughputSizeBytes <= 0 || (sizeBytes is long sz && sz >= sla.MinThroughputSizeBytes));
        if (throughputApplies && throughputMbps is double tp) {
            evaluated++;
            targets.Add(">= " + TrimNum(sla.MinThroughputMbps) + " MB/s");
            if (tp < sla.MinThroughputMbps) misses++;
            else if (tp >= 1.5 * sla.MinThroughputMbps) comfortable++;
        }

        if (sla.MaxAllocatedKb > 0 && row.AllocatedBytes is double allocBytes) {
            evaluated++;
            targets.Add("<= " + TrimNum(sla.MaxAllocatedKb) + " KB alloc");
            var allocKb = allocBytes / 1024.0;
            if (allocKb > sla.MaxAllocatedKb) misses++;
            else if (allocKb <= 0.5 * sla.MaxAllocatedKb) comfortable++;
        }

        if (evaluated == 0)
            return null;

        var result = misses > 0 ? "Miss" : comfortable == evaluated ? "Exceeds" : "Meets";
        var latest = FormatMean(row.MeanNs);
        if (throughputMbps is double tpLatest)
            latest += $" ({tpLatest:0} MB/s)";

        return new SlaEvaluation {
            Target = string.Join(", ", targets),
            Result = result,
            Standard = sla.Standard,
            Latest = latest,
            ThroughputMbps = throughputMbps,
        };
    }

    private static double? LatencyBudgetNs(BenchmarkSlaAttribute sla)
    {
        if (sla.MaxMeanNs > 0) return sla.MaxMeanNs;
        if (sla.MaxMeanUs > 0) return sla.MaxMeanUs * 1_000;
        if (sla.MaxMeanMs > 0) return sla.MaxMeanMs * 1_000_000;
        return null;
    }

    private static string FormatLatencyBudget(BenchmarkSlaAttribute sla)
    {
        if (sla.MaxMeanMs > 0) return TrimNum(sla.MaxMeanMs) + " ms";
        if (sla.MaxMeanUs > 0) return TrimNum(sla.MaxMeanUs) + " \u00b5s";
        return TrimNum(sla.MaxMeanNs) + " ns";
    }

    private static string FormatMean(double ns)
    {
        if (ns >= 1_000_000) return $"{ns / 1_000_000:0.##} ms";
        if (ns >= 1_000) return $"{ns / 1_000:0.##} \u00b5s";
        return $"{ns:0.##} ns";
    }

    private static string TrimNum(double v)
        => v == Math.Floor(v) ? ((long)v).ToString() : v.ToString("0.##");

    private static readonly Dictionary<string, int> SlaSeverity = new(StringComparer.Ordinal) {
        ["Exceeds"] = 0,
        ["Meets"] = 1,
        ["Miss"] = 2,
    };

    private static void Accumulate(
        Dictionary<(string, string), SloAggregate> aggregates,
        BenchmarkReportRow row,
        SlaEvaluation evaluation)
    {
        var key = (row.ClassName, row.Method);
        if (!aggregates.TryGetValue(key, out var aggregate)) {
            aggregate = new SloAggregate {
                Method = row.Method,
                Target = evaluation.Target,
                Standard = evaluation.Standard,
                WorstResult = evaluation.Result,
                WorstLatest = evaluation.Latest,
            };
            aggregates[key] = aggregate;
            return;
        }

        if (Severity(evaluation.Result) > Severity(aggregate.WorstResult)) {
            aggregate.WorstResult = evaluation.Result;
            aggregate.WorstLatest = evaluation.Latest;
        }
    }

    private static int Severity(string result) => SlaSeverity.TryGetValue(result, out var s) ? s : 1;

    private static IEnumerable<SloRow> BuildSloRows(Dictionary<(string, string), SloAggregate> aggregates)
        => aggregates.Values
            .OrderBy(a => a.Method, StringComparer.Ordinal)
            .Select(a => new SloRow {
                Area = a.Method,
                Target = a.Standard is { Length: > 0 } ? $"{a.Target} \u2014 {a.Standard}" : a.Target,
                Latest = a.WorstLatest,
                Result = a.WorstResult,
            });

    /// <summary>Per-(class, method) running aggregate used to roll measurements up into one SLO row.</summary>
    private sealed class SloAggregate
    {
        public string Method { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public string? Standard { get; set; }
        public string WorstResult { get; set; } = string.Empty;
        public string WorstLatest { get; set; } = string.Empty;
    }

    /// <summary>Intermediate per-case row carrying enough state to compute ratios and comparison groups.</summary>
    public sealed class BenchmarkReportRow
    {
        /// <summary>Benchmark class name.</summary>
        public string ClassName { get; set; } = string.Empty;

        /// <summary>Benchmark method name.</summary>
        public string Method { get; set; } = string.Empty;

        /// <summary>Parameter values.</summary>
        public Dictionary<string, string?> Parameters { get; set; } = [];

        /// <summary>Mean (ns).</summary>
        public double MeanNs { get; set; }

        /// <summary>Standard deviation (ns).</summary>
        public double? StdDevNs { get; set; }

        /// <summary>Allocated bytes per op.</summary>
        public double? AllocatedBytes { get; set; }

        /// <summary>Whether this is the baseline of its logical group.</summary>
        public bool IsBaseline { get; set; }

        /// <summary>Ratio to baseline.</summary>
        public double? RatioToBaseline { get; set; }

        /// <summary>Reflected workload method (for axis / SLA attribute lookup).</summary>
        public MethodInfo? WorkloadMethod { get; set; }

        /// <summary>Reflected benchmark class type (for class-level SLA fallback).</summary>
        public Type? BenchmarkType { get; set; }
    }

    /// <summary>Outcome of evaluating a benchmark row against a declared SLA budget.</summary>
    private sealed class SlaEvaluation
    {
        public string Target { get; set; } = string.Empty;
        public string Result { get; set; } = string.Empty;
        public string? Standard { get; set; }
        public string Latest { get; set; } = string.Empty;
        public double? ThroughputMbps { get; set; }
    }
}
