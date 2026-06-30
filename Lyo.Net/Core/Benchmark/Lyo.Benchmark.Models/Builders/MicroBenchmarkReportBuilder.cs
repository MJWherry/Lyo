using System;
using System.Collections.Generic;

namespace Lyo.Benchmark.Models.Builders;

/// <summary>
/// Fluent builder for <see cref="MicroBenchmarkReport" />. Used by the BenchmarkDotNet exporter and by tests to
/// construct reports consistently (mirrors the builder style of <c>WhereClauseBuilder</c> in Lyo.Query.Models).
/// </summary>
public sealed class MicroBenchmarkReportBuilder
{
    private readonly MicroBenchmarkReport _report;
    private readonly Dictionary<string, BenchmarkGroup> _groups = new(StringComparer.Ordinal);

    private MicroBenchmarkReportBuilder(string name, string title)
    {
        _report = new MicroBenchmarkReport { Name = name, Title = title, GeneratedAt = DateTimeOffset.UtcNow };
    }

    /// <summary>Starts a builder for a micro-benchmark report with the given machine name and display title.</summary>
    public static MicroBenchmarkReportBuilder Create(string name, string title) => new(name, title);

    /// <summary>Sets the suite-level methodology description.</summary>
    public MicroBenchmarkReportBuilder WithDescription(string? description)
    {
        _report.Description = description;
        return this;
    }

    /// <summary>Sets the run id and (optionally) the generation timestamp.</summary>
    public MicroBenchmarkReportBuilder WithRun(string runId, DateTimeOffset? generatedAt = null)
    {
        _report.RunId = runId;
        if (generatedAt.HasValue)
            _report.GeneratedAt = generatedAt.Value;
        return this;
    }

    /// <summary>Sets the producing environment.</summary>
    public MicroBenchmarkReportBuilder WithEnvironment(BenchmarkEnvironment environment)
    {
        _report.Environment = environment;
        return this;
    }

    /// <summary>Records the wall-clock window the run occupied (start, end, and total duration).</summary>
    public MicroBenchmarkReportBuilder WithTiming(DateTimeOffset? started, DateTimeOffset? ended, double? durationSeconds)
    {
        _report.RunStarted = started;
        _report.RunEnded = ended;
        _report.DurationSeconds = durationSeconds;
        return this;
    }

    /// <summary>Appends a methodology note / caveat.</summary>
    public MicroBenchmarkReportBuilder AddNote(string note)
    {
        if (!string.IsNullOrWhiteSpace(note))
            _report.Notes.Add(note);
        return this;
    }

    /// <summary>Adds a measurement to the named group (creating the group on first use).</summary>
    public MicroBenchmarkReportBuilder AddMeasurement(string group, BenchmarkMeasurement measurement)
    {
        GetOrAddGroup(group).Measurements.Add(measurement);
        return this;
    }

    /// <summary>Attaches descriptive context (description, parameter legend, data shape) to the named group.</summary>
    public MicroBenchmarkReportBuilder DescribeGroup(
        string group,
        string? description = null,
        IEnumerable<ParameterDescriptor>? parameters = null,
        DatasetDescriptor? dataset = null)
    {
        var bucket = GetOrAddGroup(group);
        if (description is not null)
            bucket.Description = description;
        if (parameters is not null)
            bucket.Parameters = new List<ParameterDescriptor>(parameters);
        if (dataset is not null)
            bucket.Dataset = dataset;
        return this;
    }

    private BenchmarkGroup GetOrAddGroup(string group)
    {
        if (!_groups.TryGetValue(group, out var bucket)) {
            bucket = new BenchmarkGroup { Name = group };
            _groups[group] = bucket;
            _report.Groups.Add(bucket);
        }

        return bucket;
    }

    /// <summary>Sets the comparison table.</summary>
    public MicroBenchmarkReportBuilder WithComparison(ComparisonTable comparison)
    {
        _report.Comparison = comparison;
        return this;
    }

    /// <summary>Appends an SLA / business-standard assessment row.</summary>
    public MicroBenchmarkReportBuilder AddSlo(SloRow row)
    {
        if (row is not null)
            _report.Slo.Add(row);
        return this;
    }

    /// <summary>Appends a letter-grade row.</summary>
    public MicroBenchmarkReportBuilder AddGrade(GradeRow row)
    {
        if (row is not null)
            _report.Grades.Add(row);
        return this;
    }

    /// <summary>Returns the assembled report (groups sorted by name for stable output).</summary>
    public MicroBenchmarkReport Build()
    {
        _report.Groups.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
        return _report;
    }
}
