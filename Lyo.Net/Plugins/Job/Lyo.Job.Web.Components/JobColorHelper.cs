using Lyo.Common.Core.Conversion;
using Lyo.Web.Components.DataGrid;

namespace Lyo.Job.Web.Components;

public static class JobColorHelper
{
    public static Color ForState(JobState state)
        => state switch {
            JobState.Queued => Color.Info,
            JobState.Running => Color.Warning,
            JobState.Finished => Color.Success,
            JobState.Cancelled => Color.Default,
            var _ => Color.Default
        };

    public static Color ForState(string? text)
        => TypeConversion.EnumOrNull<JobState>(text) is { } state ? ForState(state) : Color.Default;

    /// <summary>A heartbeat older than this is treated as stale even if the instance row still exists.</summary>
    public static readonly TimeSpan WorkerHeartbeatStaleAfter = TimeSpan.FromSeconds(90);

    /// <summary>MudBlazor color for a live worker-instance state.</summary>
    public static Color ForWorkerState(JobWorkerInstanceState state)
        => state switch {
            JobWorkerInstanceState.Running => Color.Success,
            JobWorkerInstanceState.Draining => Color.Warning,
            JobWorkerInstanceState.Stopped => Color.Default,
            var _ => Color.Default
        };

    public static Color ForWorkerState(string? text)
        => TypeConversion.EnumOrNull<JobWorkerInstanceState>(text) is { } state ? ForWorkerState(state) : Color.Default;

    public static string WorkerStateIcon(JobWorkerInstanceState state)
        => state switch {
            JobWorkerInstanceState.Running => Icons.Material.Filled.PlayArrow,
            JobWorkerInstanceState.Draining => Icons.Material.Filled.HourglassTop,
            JobWorkerInstanceState.Stopped => Icons.Material.Filled.Stop,
            var _ => Icons.Material.Filled.Help
        };

    public static string WorkerStateIcon(string? text)
        => TypeConversion.EnumOrNull<JobWorkerInstanceState>(text) is { } state ? WorkerStateIcon(state) : Icons.Material.Filled.Help;

    /// <summary>True when the last heartbeat is older than <paramref name="threshold" /> (90s by default).</summary>
    public static bool IsHeartbeatStale(DateTime? lastHeartbeatUtc, TimeSpan? threshold = null)
        => lastHeartbeatUtc is { } at && DateTime.UtcNow - at > (threshold ?? WorkerHeartbeatStaleAfter);

    public static Color ForResult(JobRunResult result) => ForResult((JobRunResult?)result);

    public static Color ForResult(JobRunResult? result)
        => result switch {
            JobRunResult.Success => Color.Success,
            JobRunResult.SuccessWithWarnings => Color.Warning,
            JobRunResult.PartialSuccess => Color.Warning,
            JobRunResult.Failure => Color.Error,
            JobRunResult.Cancelled => Color.Default,
            JobRunResult.Skipped => Color.Default,
            JobRunResult.Timeout => Color.Error,
            var _ => Color.Default
        };

    public static Color ForResult(string? text)
        => TypeConversion.EnumOrNull<JobRunResult>(text) is { } result ? ForResult(result) : Color.Default;

    public static Color ForDuration(DateTime? started, DateTime? finished) => LyoDurationDisplay.ForDuration(started, finished);

    public static Color ForLogLevel(JobLogLevel level)
        => level switch {
            JobLogLevel.Trace => Color.Default,
            JobLogLevel.Debug => Color.Default,
            JobLogLevel.Information => Color.Info,
            JobLogLevel.Warning => Color.Warning,
            JobLogLevel.Error => Color.Error,
            JobLogLevel.Critical => Color.Error,
            var _ => Color.Default
        };

    public static string StateIcon(JobState state)
        => state switch {
            JobState.Queued => Icons.Material.Filled.Schedule,
            JobState.Running => Icons.Material.Filled.PlayArrow,
            JobState.Finished => Icons.Material.Filled.CheckCircle,
            JobState.Cancelled => Icons.Material.Filled.Cancel,
            var _ => Icons.Material.Filled.Help
        };

    public static string ResultIcon(JobRunResult result) => ResultIcon((JobRunResult?)result);

    public static string ResultIcon(JobRunResult? result)
        => result switch {
            JobRunResult.Success => Icons.Material.Filled.CheckCircle,
            JobRunResult.SuccessWithWarnings => Icons.Material.Filled.Warning,
            JobRunResult.PartialSuccess => Icons.Material.Filled.OfflineBolt,
            JobRunResult.Failure => Icons.Material.Filled.Error,
            JobRunResult.Cancelled => Icons.Material.Filled.Cancel,
            JobRunResult.Skipped => Icons.Material.Filled.SkipNext,
            JobRunResult.Timeout => Icons.Material.Filled.Timer,
            var _ => Icons.Material.Filled.Help
        };

    public static string FormatDuration(double? ms) => LyoDurationDisplay.Format(ms);

    public static string FormatDurationFromDates(DateTime? started, DateTime? finished) => LyoDurationDisplay.FormatFromDates(started, finished);
}