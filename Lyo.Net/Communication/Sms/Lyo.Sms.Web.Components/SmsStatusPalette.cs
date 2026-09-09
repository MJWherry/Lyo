using Lyo.Web.Primitives;
using MudBlazor;

namespace Lyo.Sms.Web.Components;

/// <summary>
/// Carrier delivery statuses for <see cref="LyoStatusChip" />, keyed as <c>Palette="sms"</c>. Register via <c>AddLyoSmsStatusPalette</c>.
/// </summary>
/// <remarks>
/// String-keyed view of <see cref="SmsColorHelper" />. Carrier words do not line up cleanly with the shared vocabulary: <c>sent</c> and <c>received</c> are terminal
/// successes here, and <c>accepted</c> is still in flight.
/// </remarks>
public sealed class SmsStatusPalette : ILyoStatusPalette
{
    /// <summary>Value to pass as <see cref="LyoStatusChip.Palette" />.</summary>
    public const string PaletteName = "sms";

    /// <inheritdoc />
    public string Name => PaletteName;

    /// <inheritdoc />
    public LyoChipSpec? Resolve(string? status)
    {
        var key = LyoStatusText.Normalize(status);
        if (key.Length == 0)
            return null;

        var color = SmsColorHelper.ForStatus(status);
        return color == Color.Default ? null : new LyoChipSpec(LyoStatusText.Humanize(key), color, Icon(key));
    }

    private static string Icon(string key)
        => key switch {
            "delivered" or "read" or "sent" or "received" => Icons.Material.Filled.CheckCircle,
            "queued" or "accepted" or "scheduled" => Icons.Material.Filled.Schedule,
            "sending" or "receiving" => Icons.Material.Filled.PlayArrow,
            "failed" or "undelivered" => Icons.Material.Filled.Error,
            "canceled" or "cancelled" => Icons.Material.Filled.Cancel,
            var _ => Icons.Material.Filled.Warning
        };
}
