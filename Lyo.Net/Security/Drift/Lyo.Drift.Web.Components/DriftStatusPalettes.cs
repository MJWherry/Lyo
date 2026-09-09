namespace Lyo.Drift.Web.Components;

/// <summary>Instance lifecycle states for <see cref="LyoStatusChip" />, under <c>Palette="drift.instance"</c>.</summary>
public sealed class DriftInstanceStatusPalette : ILyoStatusPalette
{
    /// <summary>Name to supply as <see cref="LyoStatusChip.Palette" />.</summary>
    public const string PaletteName = "drift.instance";

    /// <inheritdoc />
    public string Name => PaletteName;

    /// <inheritdoc />
    public LyoChipSpec? Resolve(string? status)
        => TypeConversion.EnumOrNull<DriftInstanceState>(status) is { } state
            ? new LyoChipSpec(LyoStatusText.Humanize(status), DriftColorHelper.ForInstanceState(state), DriftColorHelper.InstanceStateIcon(state))
            : null;
}

/// <summary>Snapshot kinds for <see cref="LyoStatusChip" />, under <c>Palette="drift.snapshot"</c>.</summary>
public sealed class DriftSnapshotKindPalette : ILyoStatusPalette
{
    /// <summary>Name to supply as <see cref="LyoStatusChip.Palette" />.</summary>
    public const string PaletteName = "drift.snapshot";

    /// <inheritdoc />
    public string Name => PaletteName;

    /// <inheritdoc />
    public LyoChipSpec? Resolve(string? status)
        => TypeConversion.EnumOrNull<DriftSnapshotKind>(status) is { } kind
            ? new LyoChipSpec(LyoStatusText.Humanize(status), DriftColorHelper.ForSnapshotKind(kind))
            : null;
}

/// <summary>Diff sources for <see cref="LyoStatusChip" />, under <c>Palette="drift.diff"</c>.</summary>
public sealed class DriftDiffSourcePalette : ILyoStatusPalette
{
    /// <summary>Name to supply as <see cref="LyoStatusChip.Palette" />.</summary>
    public const string PaletteName = "drift.diff";

    /// <inheritdoc />
    public string Name => PaletteName;

    /// <inheritdoc />
    public LyoChipSpec? Resolve(string? status)
        => TypeConversion.EnumOrNull<DriftDiffSource>(status) is { } source
            ? new LyoChipSpec(LyoStatusText.Humanize(status), DriftColorHelper.ForDiffSource(source))
            : null;
}

/// <summary>File-system change kinds for <see cref="LyoStatusChip" />, under <c>Palette="drift.change"</c>.</summary>
public sealed class DriftChangeKindPalette : ILyoStatusPalette
{
    /// <summary>Name to supply as <see cref="LyoStatusChip.Palette" />.</summary>
    public const string PaletteName = "drift.change";

    /// <inheritdoc />
    public string Name => PaletteName;

    /// <inheritdoc />
    public LyoChipSpec? Resolve(string? status)
        => TypeConversion.EnumOrNull<FileSystemChangeKind>(status) is { } kind
            ? new LyoChipSpec(LyoStatusText.Humanize(status), DriftColorHelper.ForChangeKind(kind))
            : null;
}
