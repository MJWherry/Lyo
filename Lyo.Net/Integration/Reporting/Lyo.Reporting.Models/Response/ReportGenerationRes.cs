using Lyo.Reporting.Models.Enums;

namespace Lyo.Reporting.Models.Response;

/// <summary>Report generation response.</summary>
public sealed record ReportGenerationRes(
    Guid Id,
    Guid? ReportDefinitionId,
    string ReportDataJson,
    ReportFormat Format,
    ReportGenerationStatus Status,
    Guid? OutputFileId,
    string? OriginalFileName,
    string? ContentType,
    string? ErrorMessage,
    string? PathPrefix,
    string CreatedBy,
    DateTime CreatedTimestamp,
    DateTime? StartedTimestamp,
    DateTime? FinishedTimestamp,
    IReadOnlyList<ReportGenerationParameterRes>? Parameters = null);
