using Lyo.Common.Enums;
using Lyo.Exceptions;
#if NET6_0_OR_GREATER
using TimeOnly = System.TimeOnly;
#else
using TimeOnly = Lyo.DateAndTime.TimeOnlyModel;
#endif

namespace Lyo.Schedule.Models;

/// <summary>Data-only schedule definition describing when something should run.</summary>
/// <param name="Type">The type of schedule (SetTimes, Interval, OneShot, or Cron).</param>
/// <param name="DayFlags">Which days of the week the schedule applies to. Ignored for OneShot and Cron.</param>
/// <param name="MonthFlags">Which months the schedule applies to. Ignored for OneShot and Cron.</param>
/// <param name="Times">Specific times to run (SetTimes only).</param>
/// <param name="StartTime">Window start time (Interval only).</param>
/// <param name="EndTime">Window end time (Interval only).</param>
/// <param name="IntervalMinutes">Minutes between runs within the window (Interval only).</param>
/// <param name="ExecuteAt">Exact date/time to run (OneShot only).</param>
/// <param name="TimeZone">Time zone for interpreting times. Defaults to UTC when null.</param>
/// <param name="Enabled">Whether the schedule is active.</param>
/// <param name="Description">Optional description.</param>
/// <param name="CronExpression">Standard cron expression string (Cron type only). Supports 5-field (minute precision) and 6-field (second precision) formats.</param>
public record ScheduleDefinition(
    ScheduleType Type,
    DayFlags DayFlags,
    MonthFlags MonthFlags,
    IReadOnlyList<TimeOnly>? Times,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    int? IntervalMinutes,
    DateTime? ExecuteAt,
    TimeZoneInfo? TimeZone,
    bool Enabled = true,
    string? Description = null,
    string? CronExpression = null)
{
    /// <summary>Creates a new schedule definition using the builder pattern.</summary>
    public static ScheduleDefinitionBuilder Create() => new();

    /// <summary>Validates the schedule definition. Throws if invalid.</summary>
    public void Validate()
    {
        switch (Type) {
            case ScheduleType.SetTimes:
                ArgumentHelpers.ThrowIfNullOrEmpty(Times);
                ArgumentHelpers.ThrowIf(DayFlags == DayFlags.None, "DayFlags must be set for SetTimes schedule.", nameof(DayFlags));
                ArgumentHelpers.ThrowIf(MonthFlags == MonthFlags.None, "MonthFlags must be set for SetTimes schedule.", nameof(MonthFlags));
                break;
            case ScheduleType.Interval:
                ArgumentHelpers.ThrowIfNull(StartTime);
                ArgumentHelpers.ThrowIfNull(EndTime);
                ArgumentHelpers.ThrowIf(StartTime > EndTime, "StartTime must be less than or equal to EndTime.", nameof(StartTime));
                ArgumentHelpers.ThrowIfNegativeOrZero(IntervalMinutes ?? 0, nameof(IntervalMinutes));
                ArgumentHelpers.ThrowIf(DayFlags == DayFlags.None, "DayFlags must be set for Interval schedule.", nameof(DayFlags));
                ArgumentHelpers.ThrowIf(MonthFlags == MonthFlags.None, "MonthFlags must be set for Interval schedule.", nameof(MonthFlags));
                break;
            case ScheduleType.OneShot:
                ArgumentHelpers.ThrowIfNull(ExecuteAt);
                break;
            case ScheduleType.Cron:
                ArgumentHelpers.ThrowIfNullOrWhiteSpace(CronExpression);
                try {
                    Models.CronExpression.Parse(CronExpression!, CronFormat.IncludeSeconds);
                }
                catch {
                    try {
                        Models.CronExpression.Parse(CronExpression!);
                    }
                    catch (Exception ex) {
                        throw new ArgumentException($"Invalid cron expression '{CronExpression}': {ex.Message}", nameof(CronExpression), ex);
                    }
                }

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(Type), Type, "Unknown schedule type.");
        }
    }
}