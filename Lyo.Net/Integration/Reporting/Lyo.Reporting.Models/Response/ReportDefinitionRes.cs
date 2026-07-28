using Lyo.Reporting.Models.Enums;

namespace Lyo.Reporting.Models.Response;

/// <summary>Report definition response.</summary>
public sealed record ReportDefinitionRes(
    Guid Id,
    string Name,
    string? Description,
    string ReportDataJson,
    string? Tags,
    bool IsActive,
    ReportFormat? DefaultFormat,
    string? DefaultFileName,
    string? DefaultPathPrefix,
    string? GenerationProfileKey,
    string? CreatedBy,
    DateTime CreatedTimestamp,
    DateTime? UpdatedTimestamp,
    IReadOnlyList<ReportDefinitionParameterRes>? Parameters = null);