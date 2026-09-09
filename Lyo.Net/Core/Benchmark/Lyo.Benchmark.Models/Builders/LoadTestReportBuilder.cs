namespace Lyo.Benchmark.Models.Builders;

/// <summary>
/// Fluent builder for <see cref="LoadTestReport" />. The k6 normalizer (and tests) use it to assemble load-test reports on the same schema as the micro-benchmark path.
/// </summary>
public sealed class LoadTestReportBuilder
{
    private readonly LoadTestReport _report;

    private LoadTestReportBuilder(string name, string title) => _report = new() { Name = name, Title = title, GeneratedAt = DateTimeOffset.UtcNow };

    /// <summary>Opens a load-test report builder with the given machine name and display title.</summary>
    public static LoadTestReportBuilder Create(string name, string title) => new(name, title);

    /// <summary>Writes the suite-level methodology description.</summary>
    public LoadTestReportBuilder WithDescription(string? description)
    {
        _report.Description = description;
        return this;
    }

    /// <summary>Appends a query-case structure descriptor.</summary>
    public LoadTestReportBuilder AddCase(LoadCase loadCase)
    {
        _report.Cases.Add(loadCase);
        return this;
    }

    /// <summary>Writes the run id and, if supplied, the generation timestamp.</summary>
    public LoadTestReportBuilder WithRun(string runId, DateTimeOffset? generatedAt = null)
    {
        _report.RunId = runId;
        if (generatedAt.HasValue)
            _report.GeneratedAt = generatedAt.Value;

        return this;
    }

    /// <summary>Writes the producing environment.</summary>
    public LoadTestReportBuilder WithEnvironment(BenchmarkEnvironment environment)
    {
        _report.Environment = environment;
        return this;
    }

    /// <summary>Appends a methodology note or caveat.</summary>
    public LoadTestReportBuilder AddNote(string note)
    {
        if (!string.IsNullOrWhiteSpace(note))
            _report.Notes.Add(note);

        return this;
    }

    /// <summary>Appends a scenario result.</summary>
    public LoadTestReportBuilder AddScenario(LoadScenario scenario)
    {
        _report.Scenarios.Add(scenario);
        return this;
    }

    /// <summary>Appends an endpoint rollup.</summary>
    public LoadTestReportBuilder AddRollup(EndpointRollup rollup)
    {
        _report.Rollups.Add(rollup);
        return this;
    }

    /// <summary>Appends an SLO assessment row.</summary>
    public LoadTestReportBuilder AddSlo(SloRow slo)
    {
        _report.Slo.Add(slo);
        return this;
    }

    /// <summary>Appends a grade row.</summary>
    public LoadTestReportBuilder AddGrade(GradeRow grade)
    {
        _report.Grades.Add(grade);
        return this;
    }

    /// <summary>Returns the finished report.</summary>
    public LoadTestReport Build() => _report;
}