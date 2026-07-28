using Lyo.Reporting.Models.Enums;

namespace Lyo.Reporting.Models.Rendering;

/// <summary>Renders a report composition into a staged output file for a given format.</summary>
public interface IReportRenderer
{
    bool CanRender(ReportFormat format);

    Task<ReportRenderResult> RenderAsync(ReportRenderRequest request, CancellationToken ct = default);
}