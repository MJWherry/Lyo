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
    private static DayFlags GetDayFlagForDate(DateTime localDate)
        => localDate.DayOfWeek switch {
            DayOfWeek.Sunday => DayFlags.Sun,
            DayOfWeek.Monday => DayFlags.Mon,
            DayOfWeek.Tuesday => DayFlags.Tue,
            DayOfWeek.Wednesday => DayFlags.Wed,
            DayOfWeek.Thursday => DayFlags.Thu,
            DayOfWeek.Friday => DayFlags.Fri,
            DayOfWeek.Saturday => DayFlags.Sat,
            var _ => DayFlags.None
        };

    extension(JobSchedule jobSchedule)
    {
        /// <summary>Converts a database JobSchedule entity to ScheduleDefinition. Parses stored string values for Type, DayFlags, and MonthFlags.</summary>
        public ScheduleDefinition ToScheduleDefinition()
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
                type, dayFlags, monthFlags, times, startTime, endTime, jobSchedule.IntervalMinutes, null, JobScheduleExtensions.ResolveTimeZone(jobSchedule.TimeZoneId),
                jobSchedule.Enabled, jobSchedule.Description, jobSchedule.CronExpression);
        }

        /// <summary>Returns whether <paramref name="utcTime" /> falls within the schedule's active date bounds.</summary>
        public bool IsWithinScheduleWindow(DateTime utcTime)
        {
            if (jobSchedule.StartDateUtc.HasValue && utcTime < jobSchedule.StartDateUtc.Value)
                return false;

            if (jobSchedule.EndDateUtc.HasValue && utcTime > jobSchedule.EndDateUtc.Value)
                return false;

            return true;
        }

        /// <summary>Returns whether <paramref name="utcTime" /> is allowed by the schedule's optional blackout calendar windows. When no blackout calendar is attached, all times are allowed.</summary>
        public bool IsAllowedByBlackoutCalendar(DateTime utcTime)
        {
            if (jobSchedule.JobBlackoutCalendar is not { Enabled: true })
                return true;

            var windows = jobSchedule.JobBlackoutCalendar.JobBlackoutWindows.Where(w => w.Enabled).ToList();
            if (windows.Count == 0)
                return true;

            var timeZone = JobScheduleExtensions.ResolveTimeZone(jobSchedule.TimeZoneId) ?? TimeZoneInfo.Utc;
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
    }
}