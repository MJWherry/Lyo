#if NET6_0_OR_GREATER
using TimeOnly = System.TimeOnly;
#else
using TimeOnly = Lyo.DateAndTime.TimeOnlyModel;
#endif
using Lyo.Common.Enums;
using Lyo.Exceptions;

namespace Lyo.Schedule.Models;

/// <summary>Data-only schedule definition describing when something should run.</summary>
/// <param name="Type">The type of schedule (SetTimes, Interval, or OneShot).</param>
/// <param name="DayFlags">Which days of the week the schedule applies to. Ignored for OneShot.</param>
/// <param name="MonthFlags">Which months the schedule applies to. Ignored for OneShot.</param>
/// <param name="Times">Specific times to run (SetTimes only).</param>
/// <param name="StartTime">Window start time (Interval only).</param>
/// <param name="EndTime">Window end time (Interval only).</param>
/// <param name="IntervalMinutes">Minutes between runs within the window (Interval only).</param>
/// <param name="ExecuteAt">Exact date/time to run (OneShot only).</param>
/// <param name="TimeZone">Time zone for interpreting times. Uses local time if null.</param>
/// <param name="Enabled">Whether the schedule is active.</param>
/// <param name="Description">Optional description.</param>
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
    string? Description = null)
{
    /// <summary>Creates a new schedule definition using the builder pattern.</summary>
    public static ScheduleDefinitionBuilder Create() => new();

    /// <summary>Validates the schedule definition. Throws if invalid.</summary>
    public void Validate()
    {
        switch (Type) {
            case ScheduleType.SetTimes:
                ArgumentHelpers.ThrowIfNullOrEmpty(Times, nameof(Times));
                ArgumentHelpers.ThrowIf(DayFlags == DayFlags.None, "DayFlags must be set for SetTimes schedule.", nameof(DayFlags));
                ArgumentHelpers.ThrowIf(MonthFlags == MonthFlags.None, "MonthFlags must be set for SetTimes schedule.", nameof(MonthFlags));
                break;
            case ScheduleType.Interval:
                ArgumentHelpers.ThrowIfNull(StartTime, nameof(StartTime));
                ArgumentHelpers.ThrowIfNull(EndTime, nameof(EndTime));
                ArgumentHelpers.ThrowIf(StartTime > EndTime, "StartTime must be less than or equal to EndTime.", nameof(StartTime));
                ArgumentHelpers.ThrowIfNegativeOrZero(IntervalMinutes ?? 0, nameof(IntervalMinutes));
                ArgumentHelpers.ThrowIf(DayFlags == DayFlags.None, "DayFlags must be set for Interval schedule.", nameof(DayFlags));
                ArgumentHelpers.ThrowIf(MonthFlags == MonthFlags.None, "MonthFlags must be set for Interval schedule.", nameof(MonthFlags));
                break;
            case ScheduleType.OneShot:
                ArgumentHelpers.ThrowIfNull(ExecuteAt, nameof(ExecuteAt));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(Type), Type, "Unknown schedule type.");
        }
    }
}