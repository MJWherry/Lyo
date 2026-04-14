#if NET6_0_OR_GREATER
using TimeOnly = System.TimeOnly;
#else
using TimeOnly = Lyo.DateAndTime.TimeOnlyModel;
#endif
using Lyo.Common.Enums;
using Lyo.Exceptions;

namespace Lyo.DateAndTime;

/// <summary>Static utility class for date and time operations including timezone conversions and scheduling.</summary>
public static class DateAndTime
{
    private const int DefaultMaxDaysLookAhead = 7;

    private const int DefaultMaxDaysPastDueCheck = 7;

    /// <summary>Gets the TimeZoneInfo for a given US state abbreviation.</summary>
    /// <param name="usStateAbbreviation">The US state abbreviation</param>
    /// <returns>The TimeZoneInfo for the state, or null if the state is not mapped</returns>
    public static TimeZoneInfo? GetTimeZoneByState(USState usStateAbbreviation)
    {
        var geographicInfo = GeographicInfo.FromState(usStateAbbreviation);
        return geographicInfo.TimeZone;
    }

    /// <summary>Gets the local DateTime for a given US state, optionally converting from UTC.</summary>
    /// <param name="usStateAbbreviation">The US state abbreviation</param>
    /// <param name="dateTime">The UTC DateTime to convert. If null, uses current UTC time.</param>
    /// <returns>The local DateTime for the state, or null if the state is not mapped</returns>
    public static DateTime? GetLocalDateTime(USState usStateAbbreviation, DateTime? dateTime = null)
    {
        var timeZone = GetTimeZoneByState(usStateAbbreviation);
        if (timeZone == null)
            return null;

        var utcTime = dateTime ?? DateTime.UtcNow;
        return TimeZoneInfo.ConvertTimeFromUtc(utcTime, timeZone);
    }

    /// <summary>Gets the current local DateTime for a given US state.</summary>
    /// <param name="usStateAbbreviation">The US state abbreviation</param>
    /// <returns>The current local DateTime for the state, or null if the state is not mapped</returns>
    public static DateTime? GetCurrentLocalDateTime(USState usStateAbbreviation) => GetLocalDateTime(usStateAbbreviation, DateTime.UtcNow);

    /// <summary>Converts a UTC DateTime to local time for a given US state.</summary>
    /// <param name="usStateAbbreviation">The US state abbreviation</param>
    /// <param name="utcDateTime">The UTC DateTime to convert</param>
    /// <returns>The local DateTime for the state, or null if the state is not mapped</returns>
    public static DateTime? ConvertToLocalTime(USState usStateAbbreviation, DateTime utcDateTime) => GetLocalDateTime(usStateAbbreviation, utcDateTime);

    /// <summary>Converts a local DateTime to UTC for a given US state.</summary>
    /// <param name="usStateAbbreviation">The US state abbreviation</param>
    /// <param name="localDateTime">The local DateTime to convert</param>
    /// <returns>The UTC DateTime, or null if the state is not mapped</returns>
    public static DateTime? ConvertToUtc(USState usStateAbbreviation, DateTime localDateTime)
    {
        var timeZone = GetTimeZoneByState(usStateAbbreviation);
        if (timeZone == null)
            return null;

        return TimeZoneInfo.ConvertTimeToUtc(localDateTime, timeZone);
    }

    /// <summary>Gets the next scheduled DateTime based on a list of times and day flags.</summary>
    /// <param name="usStateAbbreviation">The US state abbreviation</param>
    /// <param name="scheduleTimes">The list of times to schedule</param>
    /// <param name="scheduleFlags">The day flags indicating which days are scheduled</param>
    /// <returns>The next scheduled DateTime</returns>
    /// <exception cref="ArgumentNullException">Thrown when scheduleTimes is null</exception>
    /// <exception cref="ArgumentException">Thrown when scheduleTimes is empty</exception>
    /// <exception cref="InvalidOperationException">Thrown when the state is not mapped or no valid scheduled day is found</exception>
    public static DateTime GetNextScheduledDateTime(USState usStateAbbreviation, IEnumerable<TimeOnly> scheduleTimes, DayFlags scheduleFlags)
    {
        var scheduleTimesList = scheduleTimes.ToList();
        ArgumentHelpers.ThrowIfNullOrEmpty(scheduleTimesList, nameof(scheduleTimes));
        var now = GetCurrentLocalDateTime(usStateAbbreviation);
        OperationHelpers.ThrowIfNull(now, $"Couldn't get local datetime for {usStateAbbreviation}");
        DateTime? nextScheduledDate = null;
        for (var i = 0; i <= DefaultMaxDaysLookAhead; i++) {
            var targetDate = now.Value.Date.AddDays(i);
            var dayFlag = GetDayFlagForDate(targetDate);
            if (!scheduleFlags.HasFlag(dayFlag))
                continue;

            foreach (var scheduleTime in scheduleTimesList) {
                var scheduledDate = targetDate.Add(scheduleTime.ToTimeSpan());
                if (scheduledDate <= now.Value)
                    continue;

                if (!nextScheduledDate.HasValue || scheduledDate < nextScheduledDate.Value)
                    nextScheduledDate = scheduledDate;
            }
        }

        return nextScheduledDate ?? throw new InvalidOperationException("No valid scheduled day found.");
    }

    /// <summary>Gets the next scheduled DateTime within a time window with intervals.</summary>
    /// <param name="usStateAbbreviation">The US state abbreviation</param>
    /// <param name="startTime">The start time of the scheduling window</param>
    /// <param name="endTime">The end time of the scheduling window</param>
    /// <param name="intervalMinutes">The interval in minutes between scheduled times</param>
    /// <param name="scheduleFlags">The day flags indicating which days are scheduled</param>
    /// <returns>The next scheduled DateTime</returns>
    /// <exception cref="ArgumentException">Thrown when startTime is greater than endTime or intervalMinutes is less than or equal to zero</exception>
    /// <exception cref="InvalidOperationException">Thrown when the state is not mapped or no valid scheduled datetime is found</exception>
    public static DateTime GetNextScheduledDateTime(USState usStateAbbreviation, TimeOnly startTime, TimeOnly endTime, int intervalMinutes, DayFlags scheduleFlags)
    {
        ArgumentHelpers.ThrowIf(startTime > endTime, "Start time must be less than or equal to end time.", nameof(startTime));
        ArgumentHelpers.ThrowIfNegativeOrZero(intervalMinutes, nameof(intervalMinutes));
        var now = GetCurrentLocalDateTime(usStateAbbreviation);
        OperationHelpers.ThrowIfNull(now, $"Couldn't get local datetime for {usStateAbbreviation}");
        DateTime? nextScheduledDate = null;
        for (var i = 0; i <= DefaultMaxDaysLookAhead; i++) {
            var targetDate = now.Value.Date.AddDays(i);
            var dayFlag = GetDayFlagForDate(targetDate);
            if (!scheduleFlags.HasFlag(dayFlag))
                continue;

            var current = startTime;
            var maxIterations = 1440 / intervalMinutes + 1; // Max 24 hours worth of intervals + 1
            var iterations = 0;
            while (current <= endTime && iterations < maxIterations) {
                var scheduledDate = targetDate.Add(current.ToTimeSpan());
                if (scheduledDate > now.Value && (!nextScheduledDate.HasValue || scheduledDate < nextScheduledDate.Value))
                    nextScheduledDate = scheduledDate;

                var previousCurrent = current;
                current = current.AddMinutes(intervalMinutes);
                iterations++;

                // Safety check: if we've wrapped around past midnight or current didn't advance, break
                if (current < previousCurrent || current == previousCurrent)
                    break;
            }
        }

        return nextScheduledDate ?? throw new InvalidOperationException("No valid scheduled datetime found.");
    }

    /// <summary>Checks if a scheduled job is past due based on schedule times and last run time.</summary>
    /// <param name="usStateAbbreviation">The US state abbreviation</param>
    /// <param name="scheduleTimes">The list of scheduled times</param>
    /// <param name="scheduleFlags">The day flags indicating which days are scheduled</param>
    /// <param name="lastRunDateTime">The last run DateTime in UTC. If null, defaults to 7 days ago.</param>
    /// <returns>True if the schedule is past due, false otherwise</returns>
    /// <exception cref="ArgumentNullException">Thrown when scheduleTimes is null</exception>
    /// <exception cref="ArgumentException">Thrown when scheduleTimes is empty</exception>
    /// <exception cref="InvalidOperationException">Thrown when the state is not mapped</exception>
    public static bool IsPastDue(USState usStateAbbreviation, IEnumerable<TimeOnly> scheduleTimes, DayFlags scheduleFlags, DateTime? lastRunDateTime)
    {
        var times = scheduleTimes.ToList();
        ArgumentHelpers.ThrowIfNullOrEmpty(times, nameof(scheduleTimes));
        var now = GetCurrentLocalDateTime(usStateAbbreviation);
        OperationHelpers.ThrowIfNull(now, $"Couldn't get local datetime for {usStateAbbreviation}");
        var defaultLastRun = lastRunDateTime ?? DateTime.UtcNow.AddDays(-DefaultMaxDaysPastDueCheck);
        var localizedLastRunTimestamp = ConvertToLocalTime(usStateAbbreviation, defaultLastRun);
        OperationHelpers.ThrowIfNull(localizedLastRunTimestamp, $"Couldn't convert last run datetime to local time for {usStateAbbreviation}");

        // Start checking from the last run date and go forward to now (up to DefaultMaxDaysPastDueCheck days back if needed)
        var startDate = localizedLastRunTimestamp.Value.Date;
        var endDate = now.Value.Date;
        var daysBetween = (endDate - startDate).Days + 1;

        // If last run was more than DefaultMaxDaysPastDueCheck days ago, only check the last DefaultMaxDaysPastDueCheck days
        if (daysBetween > DefaultMaxDaysPastDueCheck) {
            startDate = endDate.AddDays(-(DefaultMaxDaysPastDueCheck - 1)); // Check last N days including today
            daysBetween = DefaultMaxDaysPastDueCheck;
        }

        var daysToCheck = daysBetween;
        for (var i = 0; i < daysToCheck; i++) {
            var targetDate = startDate.AddDays(i);
            var dayFlag = GetDayFlagForDate(targetDate);
            if (!scheduleFlags.HasFlag(dayFlag))
                continue;

            foreach (var scheduleTime in times.OrderBy(t => t)) {
                var scheduledDate = targetDate.Add(scheduleTime.ToTimeSpan());
                // Check if this scheduled time is after the last run and before or equal to now
                if (scheduledDate > localizedLastRunTimestamp.Value && scheduledDate <= now.Value)
                    return true;
            }
        }

        return false;
    }

    /// <summary>Checks if a scheduled job is past due within a time window with intervals.</summary>
    /// <param name="usStateAbbreviation">The US state abbreviation</param>
    /// <param name="startTime">The start time of the scheduling window</param>
    /// <param name="endTime">The end time of the scheduling window</param>
    /// <param name="minuteInterval">The interval in minutes between scheduled times</param>
    /// <param name="scheduleFlags">The day flags indicating which days are scheduled</param>
    /// <param name="lastRunDateTime">The last run DateTime in UTC. If null, defaults to 7 days ago.</param>
    /// <returns>True if the schedule is past due, false otherwise</returns>
    /// <exception cref="ArgumentException">Thrown when startTime is greater than endTime or minuteInterval is less than or equal to zero</exception>
    /// <exception cref="InvalidOperationException">Thrown when the state is not mapped</exception>
    public static bool IsPastDue(USState usStateAbbreviation, TimeOnly startTime, TimeOnly endTime, int minuteInterval, DayFlags scheduleFlags, DateTime? lastRunDateTime)
    {
        ArgumentHelpers.ThrowIf(startTime > endTime, "Start time must be less than or equal to end time.", nameof(startTime));
        ArgumentHelpers.ThrowIfNegativeOrZero(minuteInterval, nameof(minuteInterval));
        var now = GetCurrentLocalDateTime(usStateAbbreviation);
        OperationHelpers.ThrowIfNull(now, $"Couldn't get local datetime for {usStateAbbreviation}");
        var defaultLastRun = lastRunDateTime ?? DateTime.UtcNow.AddDays(-DefaultMaxDaysPastDueCheck);
        var lastRun = ConvertToLocalTime(usStateAbbreviation, defaultLastRun);
        OperationHelpers.ThrowIfNull(lastRun, $"Couldn't convert last run datetime to local time for {usStateAbbreviation}");

        // Check only today
        var today = now.Value.Date;
        var dayFlag = GetDayFlagForDate(today);
        if (!scheduleFlags.HasFlag(dayFlag))
            return false;

        var windowStart = today.Add(startTime.ToTimeSpan());
        var windowEnd = today.Add(endTime.ToTimeSpan());
        if (now.Value < windowStart || now.Value > windowEnd)
            return false;

        var minutesSinceStart = (int)(now.Value - windowStart).TotalMinutes;
        var intervalsSinceStart = minutesSinceStart / minuteInterval;
        var lastScheduledTime = windowStart.AddMinutes(intervalsSinceStart * minuteInterval);
        return lastScheduledTime > lastRun.Value;
    }

    /// <summary>Gets all scheduled times for a given day based on schedule times and day flags.</summary>
    /// <param name="date">The date to get scheduled times for</param>
    /// <param name="scheduleTimes">The list of scheduled times</param>
    /// <param name="scheduleFlags">The day flags indicating which days are scheduled</param>
    /// <returns>An enumerable of scheduled DateTime values for the day</returns>
    /// <exception cref="ArgumentNullException">Thrown when scheduleTimes is null</exception>
    public static IEnumerable<DateTime> GetScheduledTimesForDay(DateTime date, IEnumerable<TimeOnly> scheduleTimes, DayFlags scheduleFlags)
    {
        var times = scheduleTimes.ToList();
        ArgumentHelpers.ThrowIfNullOrEmpty(times, nameof(scheduleTimes));
        var dayFlag = GetDayFlagForDate(date);
        if (!scheduleFlags.HasFlag(dayFlag))
            yield break;

        foreach (var scheduleTime in times.OrderBy(t => t))
            yield return date.Date.Add(scheduleTime.ToTimeSpan());
    }

    /// <summary>Gets all scheduled times for a given day within a time window with intervals.</summary>
    /// <param name="date">The date to get scheduled times for</param>
    /// <param name="startTime">The start time of the scheduling window</param>
    /// <param name="endTime">The end time of the scheduling window</param>
    /// <param name="intervalMinutes">The interval in minutes between scheduled times</param>
    /// <param name="scheduleFlags">The day flags indicating which days are scheduled</param>
    /// <returns>An enumerable of scheduled DateTime values for the day</returns>
    /// <exception cref="ArgumentException">Thrown when startTime is greater than endTime or intervalMinutes is less than or equal to zero</exception>
    public static IEnumerable<DateTime> GetScheduledTimesForDay(DateTime date, TimeOnly startTime, TimeOnly endTime, int intervalMinutes, DayFlags scheduleFlags)
    {
        ArgumentHelpers.ThrowIf(startTime > endTime, "Start time must be less than or equal to end time.", nameof(startTime));
        ArgumentHelpers.ThrowIfNegativeOrZero(intervalMinutes, nameof(intervalMinutes));
        var dayFlag = GetDayFlagForDate(date);
        if (!scheduleFlags.HasFlag(dayFlag))
            yield break;

        var current = startTime;
        while (current <= endTime) {
            yield return date.Date.Add(current.ToTimeSpan());

            current = current.AddMinutes(intervalMinutes);
        }
    }

    /// <summary>Checks if a given DateTime falls on a scheduled day based on day flags.</summary>
    /// <param name="dateTime">The DateTime to check</param>
    /// <param name="scheduleFlags">The day flags indicating which days are scheduled</param>
    /// <returns>True if the DateTime falls on a scheduled day, false otherwise</returns>
    public static bool IsScheduledDay(DateTime dateTime, DayFlags scheduleFlags)
    {
        var dayFlag = GetDayFlagForDate(dateTime);
        return scheduleFlags.HasFlag(dayFlag);
    }

    /// <summary>Gets the DayFlags value for a given date.</summary>
    /// <param name="date">The date to get the day flag for</param>
    /// <returns>The DayFlags value corresponding to the day of week</returns>
    private static DayFlags GetDayFlagForDate(DateTime date) => (DayFlags)(1 << (int)date.DayOfWeek);
}