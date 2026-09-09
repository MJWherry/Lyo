using Lyo.Web.Primitives;
using MudBlazor;

namespace Lyo.MessageQueue.RabbitMq.Web.Components;

/// <summary>
/// Broker queue states for <see cref="LyoStatusChip" />, keyed as <c>Palette="rabbitmq"</c>. Register via <c>AddLyoRabbitMqStatusPalette</c>.
/// </summary>
/// <remarks>
/// String-keyed view of <see cref="RabbitMqColorHelper.ForState" />. The broker can report states this client does not list (<c>flow</c>, <c>idle</c>,
/// <c>syncing</c>, and others), so any state other than <c>running</c> is treated as a warning, not an error.
/// </remarks>
public sealed class RabbitMqStatusPalette : ILyoStatusPalette
{
    /// <summary>Value to pass as <see cref="LyoStatusChip.Palette" />.</summary>
    public const string PaletteName = "rabbitmq";

    /// <inheritdoc />
    public string Name => PaletteName;

    /// <inheritdoc />
    public LyoChipSpec? Resolve(string? status)
    {
        var key = LyoStatusText.Normalize(status);
        if (key.Length == 0)
            return null;

        var color = RabbitMqColorHelper.ForState(status);
        var icon = key switch {
            "running" => Icons.Material.Filled.PlayArrow,
            "idle" or "syncing" => Icons.Material.Filled.HourglassTop,
            "flow" => Icons.Material.Filled.Warning,
            var _ => Icons.Material.Filled.Help
        };
        return new LyoChipSpec(LyoStatusText.Humanize(key), color, icon);
    }
}
