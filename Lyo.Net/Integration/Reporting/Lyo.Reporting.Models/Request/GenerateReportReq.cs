using Lyo.Reporting.Models.Enums;

namespace Lyo.Reporting.Models.Request;

/// <summary>Request to generate a report from a saved definition and/or ad-hoc report JSON.</summary>
public sealed class GenerateReportReq
{
    /// <summary>Existing definition to generate from. Required when <see cref="ReportDataJson"/> is null.</summary>
    public Guid? ReportDefinitionId { get; set; }

    /// <summary>
    /// Ad-hoc report composition JSON when not using a definition.
    /// Prefer <see cref="OverrideReportDataJson"/> to override a definition's stored JSON.
    /// </summary>
    public string? ReportDataJson { get; set; }

    /// <summary>When set with <see cref="ReportDefinitionId"/>, replaces the definition's stored composition JSON.</summary>
    public string? OverrideReportDataJson { get; set; }

    /// <summary>Output format. When null, resolved from definition → profile → Html.</summary>
    public ReportFormat? Format { get; set; }

    /// <summary>Instance parameter values for this generation (merged over definition defaults).</summary>
    public List<ReportGenerationParameterReq> Parameters { get; set; } = [];

    public string? FileName { get; set; }

    /// <summary>Optional opaque path prefix (consumer-defined; e.g. storage organization).</summary>
    public string? PathPrefix { get; set; }

    /// <summary>Optional actor stamp; API host fills from the authenticated user when omitted.</summary>
    public string? CreatedBy { get; set; }
}
