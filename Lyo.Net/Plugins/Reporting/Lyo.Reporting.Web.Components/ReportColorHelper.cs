using Lyo.Common.Core.Conversion;
using Lyo.Web.Components;
using Lyo.Web.Primitives;
using Lyo.Web.Components.DataGrid;
using Lyo.Web.Components.LyoType;
using MudBlazor;

namespace Lyo.Reporting.Web.Components;

/// <summary>MudBlazor color and icon helpers for report definition and generation grids.</summary>
public static class ReportColorHelper
{
    /// <summary>Chip color for a generation status.</summary>
    public static Color ForStatus(ReportGenerationStatus status)
        => status switch {
            ReportGenerationStatus.Succeeded => Color.Success,
            ReportGenerationStatus.Failed => Color.Error,
            ReportGenerationStatus.Running => Color.Info,
            ReportGenerationStatus.Pending => Color.Warning,
            var _ => Color.Default
        };

    /// <summary>
    /// Format chip using named <see cref="LyoChipHue" /> values, not Mud Primary/Secondary/Tertiary.
    /// Those theme slots are green, gray, and surface in the default Lyo palette, so Html/Csv/status all collapsed to the same green.
    /// </summary>
    public static LyoChipSpec ForFormatChip(ReportFormat format)
        => FormatHue(format) is { } hue ? LyoChips.Of(format.ToString(), hue) : LyoChips.Of(format.ToString());

    /// <summary>Format chip from a projected name; unknown names stay an em dash.</summary>
    public static LyoChipSpec ForFormatChip(string? text)
        => TypeConversion.EnumOrNull<ReportFormat>(text) is { } format ? ForFormatChip(format) : LyoChips.Of(text);

    /// <summary>Identity hue per format. Unknown future members get a default chip, not a guessed degree.</summary>
    private static LyoChipHue? FormatHue(ReportFormat format)
        => format switch {
            ReportFormat.Html => LyoChipHue.Blue,
            ReportFormat.Pdf => LyoChipHue.Rose,
            ReportFormat.Csv => LyoChipHue.Teal,
            ReportFormat.Xlsx => LyoChipHue.Lime,
            ReportFormat.Json => LyoChipHue.Violet,
            var _ => null
        };

    /// <summary>Chip color for a status name; unknown names use <see cref="Color.Default" />.</summary>
    public static Color ForStatus(string? text)
        => TypeConversion.EnumOrNull<ReportGenerationStatus>(text) is { } status ? ForStatus(status) : Color.Default;

    /// <summary>Human-readable duration derived from milliseconds.</summary>
    public static string FormatDuration(double? ms) => LyoDurationDisplay.Format(ms);

    /// <summary>Elapsed render time in milliseconds, or null when generation has not begun.</summary>
    public static double? GetDurationMs(DateTime? started, DateTime? finished) => LyoDurationDisplay.GetDurationMs(started, finished);

    /// <summary>Elapsed render time. Running generations (started, not finished) show time so far.</summary>
    public static string FormatDurationFromDates(DateTime? started, DateTime? finished) => LyoDurationDisplay.FormatFromDates(started, finished);

    /// <summary>Chip color for generation duration. Running stays at info; completed uses speed buckets.</summary>
    public static Color ForDuration(DateTime? started, DateTime? finished) => LyoDurationDisplay.ForDuration(started, finished);

    /// <summary>Chip color for a completed duration: quick success, then info / warning / error.</summary>
    public static Color ForDurationMs(double ms) => LyoDurationDisplay.ForDurationMs(ms);

    /// <summary>Chip color for a report parameter type (CLR FullName or a catalog alias).</summary>
    public static Color ForParameterType(string? type) => LyoTypeUi.ForType(type);
}