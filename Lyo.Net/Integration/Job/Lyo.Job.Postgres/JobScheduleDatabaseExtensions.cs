using Lyo.Common.Enums;
using Lyo.Exceptions;
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
        ArgumentHelpers.ThrowIfNull(jobSchedule, nameof(jobSchedule));
        var type = Enum.TryParse<ScheduleType>(jobSchedule.Type, out var t) ? t : ScheduleType.SetTimes;
        var dayFlags = Enum.TryParse<DayFlags>(jobSchedule.DayFlags, out var d) ? d : DayFlags.None;
        var monthFlags = Enum.TryParse<MonthFlags>(jobSchedule.MonthFlags, out var m) ? m : MonthFlags.None;
        IReadOnlyList<TimeOnly>? times = null;
        if (jobSchedule.Times != null && jobSchedule.Times.Count > 0)
            times = jobSchedule.Times.Select(s => TimeOnly.Parse(s)).ToList();

        TimeOnly? startTime = null;
        if (!string.IsNullOrEmpty(jobSchedule.StartTime))
            startTime = TimeOnly.Parse(jobSchedule.StartTime);

        TimeOnly? endTime = null;
        if (!string.IsNullOrEmpty(jobSchedule.EndTime))
            endTime = TimeOnly.Parse(jobSchedule.EndTime);

        return new(type, dayFlags, monthFlags, times, startTime, endTime, jobSchedule.IntervalMinutes, null, null, jobSchedule.Enabled, jobSchedule.Description);
    }
}