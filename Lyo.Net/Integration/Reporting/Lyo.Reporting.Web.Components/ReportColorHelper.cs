using Lyo.Reporting.Models.Enums;
using MudBlazor;

namespace Lyo.Reporting.Web.Components;

/// <summary>MudBlazor color/icon helpers for report definition and generation grids.</summary>
public static class ReportColorHelper
{
    /// <summary>Chip color for generation status.</summary>
    public static Color ForStatus(ReportGenerationStatus status)
        => status switch {
            ReportGenerationStatus.Succeeded => Color.Success,
            ReportGenerationStatus.Failed => Color.Error,
            ReportGenerationStatus.Running => Color.Info,
            ReportGenerationStatus.Pending => Color.Warning,
            _ => Color.Default
        };

    /// <summary>Chip color for report format.</summary>
    public static Color ForFormat(ReportFormat format)
        => format switch {
            ReportFormat.Pdf => Color.Error,
            ReportFormat.Csv => Color.Success,
            ReportFormat.Xlsx => Color.Info,
            ReportFormat.Json => Color.Secondary,
            ReportFormat.Html => Color.Primary,
            _ => Color.Default
        };
}
