using Lyo.Reporting.Models.Enums;

namespace Lyo.Reporting.Models.Request;

/// <summary>Legacy update payload for a report generation. Prefer <see cref="GenerateReportReq" /> — generations are read-only on the API surface.</summary>
public sealed class ReportGenerationReq
{
    public Guid? ReportDefinitionId { get; set; }

    public string ReportDataJson { get; set; } = null!;

    public ReportFormat Format { get; set; }

    public ReportGenerationStatus Status { get; set; }

    public List<ReportGenerationParameterReq> Parameters { get; set; } = [];

    /// <summary>Optional opaque id of a persisted output (consumer-defined).</summary>
    public Guid? OutputFileId { get; set; }

    public string? OriginalFileName { get; set; }

    public string? ContentType { get; set; }

    public string? ErrorMessage { get; set; }

    public string? PathPrefix { get; set; }

    public string? CreatedBy { get; set; }
}