using Lyo.Reporting.Models.Response;

namespace Lyo.Reporting.Models.Providers;

/// <summary>Input passed to <see cref="IReportDataProvider.BuildAsync" />.</summary>
public sealed class ReportDataProviderRequest
{
    public Guid? ReportDefinitionId { get; init; }

    /// <summary>Typed generation parameters for this generation.</summary>
    public IReadOnlyList<ReportGenerationParameterRes> Parameters { get; init; } = [];

    /// <summary>JSON object map of Key to Value synthesized from <see cref="Parameters" />, for providers that still expect a blob.</summary>
    public string? ParametersJson { get; init; }

    /// <summary>Seed composition JSON from the definition and/or override, before the provider runs.</summary>
    public string? ReportDataJson { get; init; }

    public IServiceProvider Services { get; init; } = null!;
}