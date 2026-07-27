namespace Lyo.Reporting.Models.Providers;

/// <summary>
/// Provider output: composition JSON for a renderer, and/or a pre-rendered file path that skips <c>IReportRenderer</c>.
/// </summary>
public sealed class ReportDataProviderResult
{
    public string? ReportDataJson { get; init; }

    /// <summary>When set, generation stages this file and skips registered renderers.</summary>
    public string? PreRenderedFilePath { get; init; }

    public string? ContentType { get; init; }

    public string? FileName { get; init; }
}
