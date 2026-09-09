using Lyo.Reporting.Models.Enums;

namespace Lyo.Reporting.Models.Request;

/// <summary>Create or update payload for a report definition.</summary>
public sealed class ReportDefinitionReq
{
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    /// <summary>Serialized report composition JSON for <see cref="Models.Report{T}" />.</summary>
    public string ReportDataJson { get; set; } = null!;

    public string? Tags { get; set; }

    public bool IsActive { get; set; } = true;

    public ReportFormat? DefaultFormat { get; set; }

    public string? DefaultFileName { get; set; }

    public string? DefaultPathPrefix { get; set; }

    /// <summary>Key into the host <c>ReportingGenerationProfile</c> and <c>IReportDataProvider</c> registries.</summary>
    public string? GenerationProfileKey { get; set; }

    /// <summary>Parameters created alongside the definition (nested create).</summary>
    public List<ReportDefinitionParameterReq> CreateParameters { get; set; } = [];
}