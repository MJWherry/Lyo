using Lyo.Reporting.Models.Response;

namespace Lyo.Reporting.Models.Providers;

/// <summary>Input to <see cref="IReportDataProvider.BuildAsync" />.</summary>
public sealed class ReportDataProviderRequest
{
    public Guid? ReportDefinitionId { get; init; }

    /// <summary>Typed generation parameters for this run.</summary>
    public IReadOnlyList<ReportGenerationParameterRes> Parameters { get; init; } = [];

    /// <summary>JSON object map of Key→Value synthesized from <see cref="Parameters" /> for providers that still expect a blob.</summary>
    public string? ParametersJson { get; init; }

    /// <summary>Seed composition JSON from definition and/or override before provider runs.</summary>
    public string? ReportDataJson { get; init; }

    public IServiceProvider Services { get; init; } = null!;
}