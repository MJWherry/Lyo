using Lyo.Common.Core.Conversion;

namespace Lyo.Job.Web.Components;

/// <summary>
/// Job run states and results for <see cref="LyoStatusChip" />. Register with <c>AddLyoJobStatusPalettes</c>, then set <c>Palette="job"</c> on the chip.
/// </summary>
/// <remarks>
/// A thin string-keyed view of <see cref="JobColorHelper" />, which keeps its strongly typed API for components that already hold a <see cref="JobState" /> or
/// <see cref="JobRunResult" />. Results are matched before states, which is safe because the two enums agree on the colour of every name they share.
/// </remarks>
public sealed class JobStatusPalette : ILyoStatusPalette
{
    /// <summary>Name to supply as <see cref="LyoStatusChip.Palette" />.</summary>
    public const string PaletteName = "job";

    /// <inheritdoc />
    public string Name => PaletteName;

    /// <inheritdoc />
    public LyoChipSpec? Resolve(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return null;

        if (TypeConversion.EnumOrNull<JobRunResult>(status) is { } result)
            return new LyoChipSpec(LyoStatusText.Humanize(status), JobColorHelper.ForResult(result), JobColorHelper.ResultIcon(result));

        if (TypeConversion.EnumOrNull<JobState>(status) is { } state)
            return new LyoChipSpec(LyoStatusText.Humanize(status), JobColorHelper.ForState(state), JobColorHelper.StateIcon(state));

        return null;
    }
}

/// <summary>Worker instance lifecycle states for <see cref="LyoStatusChip" />, under <c>Palette="job.worker"</c>.</summary>
/// <remarks>Kept apart from <see cref="JobStatusPalette" /> because <c>Stopped</c> and <c>Draining</c> mean a worker process, not a run outcome.</remarks>
public sealed class JobWorkerStatusPalette : ILyoStatusPalette
{
    /// <summary>Name to supply as <see cref="LyoStatusChip.Palette" />.</summary>
    public const string PaletteName = "job.worker";

    /// <inheritdoc />
    public string Name => PaletteName;

    /// <inheritdoc />
    public LyoChipSpec? Resolve(string? status)
        => TypeConversion.EnumOrNull<JobWorkerInstanceState>(status) is { } state
            ? new LyoChipSpec(LyoStatusText.Humanize(status), JobColorHelper.ForWorkerState(state), JobColorHelper.WorkerStateIcon(state))
            : null;
}
