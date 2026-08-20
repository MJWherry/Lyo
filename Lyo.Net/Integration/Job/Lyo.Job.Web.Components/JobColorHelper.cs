using System.ComponentModel;
using System.Reflection;
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
        => Enum.TryParse<JobState>(text, out var state) ? ForState(state) : Color.Default;

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
        => Enum.TryParse<JobRunResult>(text, out var result) ? ForResult(result) : Color.Default;

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

    public static string GetEnumDescription<T>(T value)
        where T : Enum
    {
        var field = typeof(T).GetField(value.ToString());
        var attr = field?.GetCustomAttribute<DescriptionAttribute>();
        return attr?.Description ?? value.ToString();
    }
}