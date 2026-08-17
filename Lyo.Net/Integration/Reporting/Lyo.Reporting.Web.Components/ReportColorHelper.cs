using Lyo.Web.Components.DataGrid;
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
            var _ => Color.Default
        };

    /// <summary>Chip color for report format.</summary>
    public static Color ForFormat(ReportFormat format)
        => format switch {
            ReportFormat.Pdf => Color.Error,
            ReportFormat.Csv => Color.Success,
            ReportFormat.Xlsx => Color.Info,
            ReportFormat.Json => Color.Secondary,
            ReportFormat.Html => Color.Primary,
            var _ => Color.Default
        };

    /// <summary>Chip color for a format name; unknown values use <see cref="Color.Default" />.</summary>
    public static Color ForFormat(string? text)
        => Enum.TryParse<ReportFormat>(text, out var format) ? ForFormat(format) : Color.Default;

    /// <summary>Chip color for a status name; unknown values use <see cref="Color.Default" />.</summary>
    public static Color ForStatus(string? text)
        => Enum.TryParse<ReportGenerationStatus>(text, out var status) ? ForStatus(status) : Color.Default;

    /// <summary>Human-readable duration from milliseconds.</summary>
    public static string FormatDuration(double? ms) => LyoDurationDisplay.Format(ms);

    /// <summary>Elapsed render time in milliseconds, or null when generation has not started.</summary>
    public static double? GetDurationMs(DateTime? started, DateTime? finished) => LyoDurationDisplay.GetDurationMs(started, finished);

    /// <summary>Elapsed render time. Running generations (started, not finished) show time-so-far.</summary>
    public static string FormatDurationFromDates(DateTime? started, DateTime? finished) => LyoDurationDisplay.FormatFromDates(started, finished);

    /// <summary>Chip color for generation duration. Running stays info; completed uses speed buckets.</summary>
    public static Color ForDuration(DateTime? started, DateTime? finished) => LyoDurationDisplay.ForDuration(started, finished);

    /// <summary>Chip color for a completed duration: fast success, then info / warning / error.</summary>
    public static Color ForDurationMs(double ms) => LyoDurationDisplay.ForDurationMs(ms);

    /// <summary>Chip color for a report parameter type.</summary>
    public static Color ForParameterType(ReportParameterType type)
        => type switch {
            ReportParameterType.Guid => Color.Secondary,
            ReportParameterType.Bool => Color.Success,
            ReportParameterType.Int or ReportParameterType.Long or ReportParameterType.Decimal => Color.Info,
            ReportParameterType.DateTime or ReportParameterType.DateOnly or ReportParameterType.TimeOnly => Color.Tertiary,
            ReportParameterType.Json or ReportParameterType.Xml or ReportParameterType.Regex => Color.Warning,
            ReportParameterType.Enum => Color.Primary,
            var _ => Color.Default
        };
}