using Lyo.Reporting.Models.Enums;

namespace Lyo.Reporting.Models.Response;

/// <summary>
/// Report generation response. <see cref="ReportDataJson" /> is null on generate and rerun responses unless the caller asked for it (<c>IncludeReportData</c>), because the
/// composition JSON can reach several megabytes and most callers only need the status and output identifiers. Query and get responses still include it.
/// </summary>
public sealed record ReportGenerationRes(
    Guid Id,
    Guid? ReportDefinitionId,
    string? ReportDataJson,
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