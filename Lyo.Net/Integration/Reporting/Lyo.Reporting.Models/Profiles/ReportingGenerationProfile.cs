using Lyo.Reporting.Models.Enums;

namespace Lyo.Reporting.Models.Profiles;

/// <summary>Host-registered defaults keyed by <see cref="Key"/> (matches definition <c>GenerationProfileKey</c>).</summary>
public sealed class ReportingGenerationProfile
{
    public required string Key { get; init; }

    public ReportFormat? DefaultFormat { get; init; }

    public string? DefaultFileName { get; init; }

    public string? DefaultPathPrefix { get; init; }
}
