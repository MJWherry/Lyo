#if NET6_0_OR_GREATER
using TimeOnly = System.TimeOnly;
#else
using TimeOnly = Lyo.DateAndTime.TimeOnlyModel;
#endif
using System.Diagnostics;
using Lyo.Common.Core.Enums;
using Lyo.Exceptions;

namespace Lyo.Schedule.Models;

/// <summary>Data-only schedule that describes when something should run.</summary>
/// <param name="Type">Schedule kind (SetTimes, Interval, OneShot, or Cron).</param>
/// <param name="DayFlags">Days of the week the schedule applies to. Ignored for OneShot and Cron.</param>
/// <param name="MonthFlags">Months the schedule applies to. Ignored for OneShot and Cron.</param>
/// <param name="Times">Clock times to fire (SetTimes only).</param>
/// <param name="StartTime">Window start (Interval only).</param>
/// <param name="EndTime">Window end (Interval only).</param>
/// <param name="IntervalMinutes">Minutes between fires inside the window (Interval only).</param>
/// <param name="ExecuteAt">Exact date/time to fire (OneShot only).</param>
/// <param name="TimeZone">Time zone used to interpret times. Defaults to UTC when null.</param>
/// <param name="Enabled">True when the schedule is active.</param>
/// <param name="Description">Optional description.</param>
/// <param name="CronExpression">Cron string (Cron type only). Supports 5-field (minute) and 6-field (second) forms.</param>
[DebuggerDisplay("{ToString(),nq}")]
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
    /// <summary>Mints a builder for a schedule definition.</summary>
    public static ScheduleDefinitionBuilder Create() => new();

    /// <summary>Checks the schedule definition. Throws when it is invalid.</summary>
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

    public override string ToString() => $"ScheduleDefinition: type={Type}, enabled={Enabled}, cron={CronExpression ?? "(none)"}";
}