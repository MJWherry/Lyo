using Lyo.Common.Core.Conversion;
using Lyo.Common.Core.Enums;
using Lyo.Exceptions;
using Lyo.Job.Models;
using Lyo.Job.Postgres.Database;
using Lyo.Job.Postgres.Mapping;
using Lyo.Schedule.Models;
#if NET6_0_OR_GREATER
using TimeOnly = System.TimeOnly;

#else
using TimeOnly = Lyo.DateAndTime.TimeOnlyModel;
#endif

namespace Lyo.Job.Postgres;

/// <summary>Helpers that convert database JobSchedule entities to ScheduleDefinition for Lyo.Scheduler.</summary>
public static class JobScheduleDatabaseExtensions
{
    extension(JobSchedule jobSchedule)
    {
        /// <summary>Converts a stored JobSchedule entity to ScheduleDefinition. Parses stored string values for Type, DayFlags, and MonthFlags.</summary>
        public ScheduleDefinition ToScheduleDefinition()
        {
            ArgumentHelpers.ThrowIfNull(jobSchedule);
            var type = TypeConversion.EnumOrDefault(jobSchedule.Type, ScheduleType.SetTimes);
            var dayFlags = TypeConversion.EnumOrDefault(jobSchedule.DayFlags, DayFlags.None);
            var monthFlags = TypeConversion.EnumOrDefault(jobSchedule.MonthFlags, MonthFlags.None);
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

        /// <summary>Returns whether <paramref name="utcTime" /> sits inside the schedule's active date bounds.</summary>
        public bool IsWithinScheduleWindow(DateTime utcTime)
        {
            if (jobSchedule.StartDateUtc.HasValue && utcTime < jobSchedule.StartDateUtc.Value)
                return false;

            if (jobSchedule.EndDateUtc.HasValue && utcTime > jobSchedule.EndDateUtc.Value)
                return false;

            return true;
        }

        /// <summary>
        /// Applies the schedule's optional blackout calendar to <paramref name="utcTime" />. Returns null when the slot should be skipped, the original instant when it may
        /// fire, or a deferred UTC instant when a matching window uses Defer. When no calendar is attached, returns <paramref name="utcTime" />.
        /// </summary>
        public DateTime? AdjustSlotForBlackout(DateTime utcTime)
        {
            if (jobSchedule.JobBlackoutCalendar is not { Enabled: true })
                return utcTime;

            var calendar = JobLyoMapper.ToRes(jobSchedule.JobBlackoutCalendar);
            var timeZone = JobScheduleExtensions.ResolveTimeZone(jobSchedule.TimeZoneId) ?? TimeZoneInfo.Utc;
            return JobBlackoutCalendarEvaluator.AdjustSlotForBlackout(utcTime, calendar, timeZone);
        }

        /// <summary>
        /// Returns whether <paramref name="utcTime" /> is allowed by the schedule's optional blackout calendar windows. Skip windows return false; Defer windows still
        /// count as allowed (the fire time is shifted). When no blackout calendar is attached, every time is allowed.
        /// </summary>
        public bool IsAllowedByBlackoutCalendar(DateTime utcTime) => jobSchedule.AdjustSlotForBlackout(utcTime).HasValue;
    }
}
