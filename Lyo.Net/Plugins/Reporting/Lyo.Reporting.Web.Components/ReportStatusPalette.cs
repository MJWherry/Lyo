using Lyo.Common.Core.Conversion;
using Lyo.Web.Primitives;
using MudBlazor;

namespace Lyo.Reporting.Web.Components;

/// <summary>
/// Report generation statuses for <see cref="LyoStatusChip" />, under <c>Palette="report"</c>. Register with <c>AddLyoReportStatusPalette</c>.
/// </summary>
/// <remarks>
/// A string-keyed view of <see cref="ReportColorHelper" />, kept apart from the shared vocabulary because reporting colours <c>Pending</c> as a warning rather
/// than informational.
/// </remarks>
public sealed class ReportStatusPalette : ILyoStatusPalette
{
    /// <summary>Name to supply as <see cref="LyoStatusChip.Palette" />.</summary>
    public const string PaletteName = "report";

    /// <inheritdoc />
    public string Name => PaletteName;

    /// <inheritdoc />
    public LyoChipSpec? Resolve(string? status)
        => TypeConversion.EnumOrNull<ReportGenerationStatus>(status) is { } parsed
            ? new LyoChipSpec(LyoStatusText.Humanize(status), ReportColorHelper.ForStatus(parsed), Icon(parsed))
            : null;

    private static string Icon(ReportGenerationStatus status)
        => status switch {
            ReportGenerationStatus.Succeeded => Icons.Material.Filled.CheckCircle,
            ReportGenerationStatus.Failed => Icons.Material.Filled.Error,
            ReportGenerationStatus.Running => Icons.Material.Filled.PlayArrow,
            ReportGenerationStatus.Pending => Icons.Material.Filled.Schedule,
            var _ => Icons.Material.Filled.Help
        };
}
