using Lyo.Common.Enums;
using Lyo.Exceptions;
using Lyo.Job.Models;
using Lyo.Job.Models.Enums;
using Lyo.Job.Postgres.Database;
using Lyo.Schedule.Models;
#if NET6_0_OR_GREATER
using TimeOnly = System.TimeOnly;

#else
using TimeOnly = Lyo.DateAndTime.TimeOnlyModel;
#endif

namespace Lyo.Job.Postgres;

/// <summary>Extensions for converting database JobSchedule entities to ScheduleDefinition for use with Lyo.Scheduler.</summary>
public static class JobScheduleDatabaseExtensions
{
    /// <summary>Converts a database JobSchedule entity to ScheduleDefinition. Parses stored string values for Type, DayFlags, and MonthFlags.</summary>
    public static ScheduleDefinition ToScheduleDefinition(this JobSchedule jobSchedule)
    {
        ArgumentHelpers.ThrowIfNull(jobSchedule);
        var type = Enum.TryParse<ScheduleType>(jobSchedule.Type, out var t) ? t : ScheduleType.SetTimes;
        var dayFlags = Enum.TryParse<DayFlags>(jobSchedule.DayFlags, out var d) ? d : DayFlags.None;
        var monthFlags = Enum.TryParse<MonthFlags>(jobSchedule.MonthFlags, out var m) ? m : MonthFlags.None;
        IReadOnlyList<TimeOnly>? times = null;
        if (jobSchedule.Times is { Count: > 0 })
            times = jobSchedule.Times.Select(TimeOnly.Parse).ToList();

        TimeOnly? startTime = null;
        if (!string.IsNullOrEmpty(jobSchedule.StartTime))
            startTime = TimeOnly.Parse(jobSchedule.StartTime);

        TimeOnly? endTime = null;
        if (!string.IsNullOrEmpty(jobSchedule.EndTime))
            endTime = TimeOnly.Parse(jobSchedule.EndTime);

        return new(
            type, dayFlags, monthFlags, times, startTime, endTime, jobSchedule.IntervalMinutes, null,
            JobScheduleExtensions.ResolveTimeZone(jobSchedule.TimeZoneId), jobSchedule.Enabled, jobSchedule.Description, jobSchedule.CronExpression);
    }

    /// <summary>Returns whether <paramref name="utcTime" /> falls within the schedule's active date bounds.</summary>
    public static bool IsWithinScheduleWindow(this JobSchedule schedule, DateTime utcTime)
    {
        if (schedule.StartDateUtc.HasValue && utcTime < schedule.StartDateUtc.Value)
            return false;

        if (schedule.EndDateUtc.HasValue && utcTime > schedule.EndDateUtc.Value)
            return false;

        return true;
    }

    /// <summary>
    /// Returns whether <paramref name="utcTime" /> is allowed by the schedule's optional calendar blackout windows. When no calendar is attached, all times are allowed.
    /// </summary>
    public static bool IsAllowedByCalendar(this JobSchedule schedule, DateTime utcTime)
    {
        if (schedule.JobCalendar is not { Enabled: true })
            return true;

        var windows = schedule.JobCalendar.JobCalendarWindows.Where(w => w.Enabled).ToList();
        if (windows.Count == 0)
            return true;

        var timeZone = JobScheduleExtensions.ResolveTimeZone(schedule.TimeZoneId) ?? TimeZoneInfo.Utc;
        var local = TimeZoneInfo.ConvertTimeFromUtc(utcTime, timeZone);
        var dayFlag = GetDayFlagForDate(local);

        foreach (var window in windows) {
            if (!Enum.TryParse<DayFlags>(window.DayFlags, out var windowDays) || !windowDays.HasFlag(dayFlag))
                continue;

            var start = TimeOnly.Parse(window.StartTime);
            var end = TimeOnly.Parse(window.EndTime);
            var localTime = TimeOnly.FromDateTime(local);
            if (localTime < start || localTime > end)
                continue;

            if (Enum.TryParse<JobBlackoutPolicy>(window.Policy, out var policy) && policy == JobBlackoutPolicy.Skip)
                return false;
        }

        return true;
    }

    private static DayFlags GetDayFlagForDate(DateTime localDate)
        => localDate.DayOfWeek switch {
            DayOfWeek.Sunday => DayFlags.Sun,
            DayOfWeek.Monday => DayFlags.Mon,
            DayOfWeek.Tuesday => DayFlags.Tue,
            DayOfWeek.Wednesday => DayFlags.Wed,
            DayOfWeek.Thursday => DayFlags.Thu,
            DayOfWeek.Friday => DayFlags.Fri,
            DayOfWeek.Saturday => DayFlags.Sat,
            _ => DayFlags.None
        };
}
