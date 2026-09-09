using Lyo.Reporting.Models.Enums;

namespace Lyo.Reporting.Models.Request;

/// <summary>Request that generates a report from a saved definition and/or ad-hoc report JSON.</summary>
public sealed class GenerateReportReq
{
    /// <summary>Existing definition to generate from. Needed when <see cref="ReportDataJson" /> is null.</summary>
    public Guid? ReportDefinitionId { get; set; }

    /// <summary>Ad-hoc report composition JSON when no definition is used. Prefer <see cref="OverrideReportDataJson" /> to override a definition's stored JSON.</summary>
    public string? ReportDataJson { get; set; }

    /// <summary>When set together with <see cref="ReportDefinitionId" />, replaces the definition's stored composition JSON.</summary>
    public string? OverrideReportDataJson { get; set; }

    /// <summary>Output format. When null, resolved from definition, then profile, then Html.</summary>
    public ReportFormat? Format { get; set; }

    /// <summary>Instance parameter values for this generation (merged over the definition defaults).</summary>
    public List<ReportGenerationParameterReq> Parameters { get; set; } = [];

    public string? FileName { get; set; }

    /// <summary>Optional opaque path prefix (consumer-defined; for example storage organization).</summary>
    public string? PathPrefix { get; set; }

    /// <summary>Optional actor stamp; the API host fills this from the authenticated user when omitted.</summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Echo the resolved composition JSON back on the response. Off by default: the rendered output is the deliverable, and the JSON can reach
    /// <c>MaxReportDataJsonBytes</c> (5 MB by default). Callers that preview grids client-side set this, or re-read the generation through query or get.
    /// </summary>
    public bool IncludeReportData { get; set; }
}