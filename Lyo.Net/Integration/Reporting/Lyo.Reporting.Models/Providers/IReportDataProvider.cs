namespace Lyo.Reporting.Models.Providers;

/// <summary>
/// Host-registered provider that builds report composition (or a pre-rendered file) for a <see cref="Profiles.ReportingGenerationProfile" /> key. Registered on the API host,
/// not workers.
/// </summary>
public interface IReportDataProvider
{
    /// <summary>Matches <c>ReportDefinition.GenerationProfileKey</c> / profile registry key.</summary>
    string ProfileKey { get; }

    Task<ReportDataProviderResult> BuildAsync(ReportDataProviderRequest request, CancellationToken ct = default);
}