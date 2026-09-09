namespace Lyo.Web.Primitives;

/// <summary>
/// The status vocabulary every Lyo surface shares: outcomes, lifecycle states, and health. <see cref="LyoStatusChip" /> falls back to this whenever no domain palette
/// recognizes a status, so a package with no palette of its own still gets sensible colours.
/// </summary>
/// <remarks>
/// A domain palette that disagrees wins, because the chip asks it first. Jobs colour <c>Running</c> as a warning, for instance, while the default here treats it as
/// informational.
/// </remarks>
public sealed class LyoDefaultStatusPalette : ILyoStatusPalette
{
    /// <summary>Name reserved for this palette. Domain palettes must not reuse it.</summary>
    public const string PaletteName = "default";

    /// <summary>Shared instance, used by <see cref="LyoStatusPaletteResolver.Default" /> and safe to register on its own.</summary>
    public static readonly LyoDefaultStatusPalette Instance = new();

    /// <inheritdoc />
    public string Name => PaletteName;

    /// <inheritdoc />
    public LyoChipSpec? Resolve(string? status)
    {
        var key = LyoStatusText.Normalize(status);
        if (key.Length == 0)
            return null;

        var label = LyoStatusText.Humanize(key);
        return key switch {
            "success" or "succeeded" or "ok" or "complete" or "completed" or "done" or "passed" or "valid" or "delivered" or "sent" or "read" or "healthy" or "active"
                or "enabled" or "online" or "connected" or "up" => new LyoChipSpec(label, Color.Success, Icons.Material.Filled.CheckCircle),
            "warning" or "warn" or "degraded" or "partial" or "partial success" or "partially delivered" or "succeeded with warnings" or "success with warnings" or "stale"
                => new LyoChipSpec(label, Color.Warning, Icons.Material.Filled.Warning),
            "failed" or "failure" or "error" or "faulted" or "invalid" or "unhealthy" or "undelivered" or "rejected" or "down" or "disconnected"
                => new LyoChipSpec(label, Color.Error, Icons.Material.Filled.Error),
            "timeout" or "timed out" or "expired" => new LyoChipSpec(label, Color.Error, Icons.Material.Filled.Timer),
            "running" or "in progress" or "inprogress" or "processing" or "started" or "sending" or "receiving" or "draining"
                => new LyoChipSpec(label, Color.Info, Icons.Material.Filled.PlayArrow),
            "queued" or "pending" or "scheduled" or "waiting" or "accepted" or "new" or "created" => new LyoChipSpec(label, Color.Info, Icons.Material.Filled.Schedule),
            "cancelled" or "canceled" or "aborted" or "stopped" => new LyoChipSpec(label, Color.Default, Icons.Material.Filled.Cancel),
            "skipped" or "ignored" or "noop" => new LyoChipSpec(label, Color.Default, Icons.Material.Filled.SkipNext),
            "disabled" or "inactive" or "offline" or "paused" or "suspended" => new LyoChipSpec(label, Color.Default, Icons.Material.Filled.PauseCircle),
            "unknown" or "none" => new LyoChipSpec(label, Color.Default, Icons.Material.Filled.Help),
            var _ => null
        };
    }
}
