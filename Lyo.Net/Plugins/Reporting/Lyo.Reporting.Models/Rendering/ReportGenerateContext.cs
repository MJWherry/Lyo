using Lyo.Reporting.Models.Enums;
using Lyo.Reporting.Models.Request;

namespace Lyo.Reporting.Models.Rendering;

/// <summary>Mutable context handed through generation hooks.</summary>
public class ReportGenerateContext
{
    public Guid GenerationId { get; init; }

    public Guid? ReportDefinitionId { get; init; }

    public GenerateReportReq Request { get; init; } = null!;

    public ReportFormat Format { get; init; }

    public string ReportDataJson { get; set; } = null!;

    /// <summary>Staged output path after render (from IoTemp).</summary>
    public string? StagedFilePath { get; set; }

    public string? ContentType { get; set; }

    public string? FileName { get; set; }

    /// <summary>Optional opaque id of a persisted output (set by consumer hooks, for example a FileStorage file id).</summary>
    public Guid? OutputFileId { get; set; }

    public string? PathPrefix { get; set; }

    public IServiceProvider Services { get; init; } = null!;

    public IDictionary<string, object?> Items { get; } = new Dictionary<string, object?>();
}