#if NET6_0_OR_GREATER
using TimeOnly = System.TimeOnly;
#else
using TimeOnly = Lyo.DateAndTime.TimeOnlyModel;
#endif
using Lyo.Common.Core.Enums;
using Lyo.Common.Metadata.Records;
using Lyo.Exceptions;

namespace Lyo.DateAndTime;

/// <summary>US-state time-zone conversions and weekday scheduling helpers, all anchored in each state's local civil time.</summary>
/// <remarks>
/// <para>
/// Time zones resolve through <see cref="Lyo.Common.Core.Enums.GeographicInfo.FromState" /> (IANA ids such as <c>America/New_York</c>). When the OS cannot resolve an id, conversion
/// members return <see langword="null" /> instead of throwing.
/// </para>
/// <para>
/// Scheduling reads <see cref="DayFlags" /> as one bit per weekday. Next-run search looks at most seven local midnights ahead; discrete <c>IsPastDue</c> scans cap
/// backward history the same way. The interval-based <c>IsPastDue</c> overload only looks at <em>today's</em> local window — the discrete-times overload walks multiple
/// days.
/// </para>
/// <para>On .NET 6+, APIs use <see cref="TimeOnly" />; on .NET Standard 2.0 use <see cref="TimeOnlyModel" /> instead (this library aliases the type internally).</para>
/// </remarks>
public static class DateAndTime
{
    private const int DefaultMaxDaysLookAhead = 7;

    private const int DefaultMaxDaysPastDueCheck = 7;

    /// <summary>Looks up <see cref="TimeZoneInfo" /> for a US state abbreviation.</summary>
    /// <param name="usStateAbbreviation">US state abbreviation</param>
    /// <returns>The state's <see cref="TimeZoneInfo" />, or null when the state is not mapped</returns>
    public static TimeZoneInfo? GetTimeZoneByState(USState usStateAbbreviation)
    {
        var geographicInfo = GeographicInfo.FromState(usStateAbbreviation);
        return geographicInfo.TimeZone;
    }

    /// <summary>Returns local <see cref="DateTime" /> for a US state, optionally converting from UTC.</summary>
    /// <param name="usStateAbbreviation">US state abbreviation</param>
    /// <param name="dateTime">UTC <see cref="DateTime" /> to convert. Null means current UTC.</param>
    /// <returns>Local <see cref="DateTime" /> for the state, or null when the state is not mapped</returns>
    public static DateTime? GetLocalDateTime(USState usStateAbbreviation, DateTime? dateTime = null)
    {
        var timeZone = GetTimeZoneByState(usStateAbbreviation);
        if (timeZone == null)
            return null;

        var utcTime = dateTime ?? DateTime.UtcNow;
        return TimeZoneInfo.ConvertTimeFromUtc(utcTime, timeZone);
    }

    /// <summary>Returns the current local <see cref="DateTime" /> for a US state.</summary>
    /// <param name="usStateAbbreviation">US state abbreviation</param>
    /// <returns>Current local <see cref="DateTime" /> for the state, or null when the state is not mapped</returns>
    public static DateTime? GetCurrentLocalDateTime(USState usStateAbbreviation) => GetLocalDateTime(usStateAbbreviation, DateTime.UtcNow);

    /// <summary>Converts a UTC <see cref="DateTime" /> to local time for a US state.</summary>
    /// <param name="usStateAbbreviation">US state abbreviation</param>
    /// <param name="utcDateTime">UTC <see cref="DateTime" /> to convert</param>
    /// <returns>Local <see cref="DateTime" /> for the state, or null when the state is not mapped</returns>
    public static DateTime? ConvertToLocalTime(USState usStateAbbreviation, DateTime utcDateTime) => GetLocalDateTime(usStateAbbreviation, utcDateTime);

    /// <summary>Converts a local <see cref="DateTime" /> to UTC for a US state.</summary>
    /// <param name="usStateAbbreviation">US state abbreviation</param>
    /// <param name="localDateTime">Local <see cref="DateTime" /> to convert</param>
    /// <returns>UTC <see cref="DateTime" />, or null when the state is not mapped</returns>
    public static DateTime? ConvertToUtc(USState usStateAbbreviation, DateTime localDateTime)
    {
        var timeZone = GetTimeZoneByState(usStateAbbreviation);
        if (timeZone == null)
            return null;

        return TimeZoneInfo.ConvertTimeToUtc(localDateTime, timeZone);
    }

    /// <summary>Finds the next scheduled <see cref="DateTime" /> from a list of times and day flags.</summary>
    /// <param name="usStateAbbreviation">US state abbreviation</param>
    /// <param name="scheduleTimes">Times of day to schedule</param>
    /// <param name="scheduleFlags">Day flags that mark which weekdays are scheduled</param>
    /// <returns>The next scheduled <see cref="DateTime" /></returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="scheduleTimes" /> is null</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="scheduleTimes" /> is empty</exception>
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

        return nextScheduledDate.OrThrowInvalidOperation("No valid scheduled day found.");
    }

    /// <summary>Finds the next scheduled <see cref="DateTime" /> inside a window that ticks at a fixed interval.</summary>
    /// <param name="usStateAbbreviation">US state abbreviation</param>
    /// <param name="startTime">Start of the scheduling window</param>
    /// <param name="endTime">End of the scheduling window</param>
    /// <param name="intervalMinutes">Minutes between scheduled times</param>
    /// <param name="scheduleFlags">Day flags that mark which weekdays are scheduled</param>
    /// <returns>The next scheduled <see cref="DateTime" /></returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="startTime" /> is after <paramref name="endTime" />, or <paramref name="intervalMinutes" /> is not positive</exception>
    /// <exception cref="InvalidOperationException">Thrown when the state is not mapped or no valid scheduled datetime is found</exception>
    public static DateTime GetNextScheduledDateTime(USState usStateAbbreviation, TimeOnly startTime, TimeOnly endTime, int intervalMinutes, DayFlags scheduleFlags)
    {
        ArgumentHelpers.ThrowIf(startTime > endTime, "Start time must be less than or equal to end time.", nameof(startTime));
        ArgumentHelpers.ThrowIfNegativeOrZero(intervalMinutes);
        var now = GetCurrentLocalDateTime(usStateAbbreviation);
        OperationHelpers.ThrowIfNull(now, $"Couldn't get local datetime for {usStateAbbreviation}");
        DateTime? nextScheduledDate = null;
        for (var i = 0; i <= DefaultMaxDaysLookAhead; i++) {
            var targetDate = now.Value.Date.AddDays(i);
            var dayFlag = GetDayFlagForDate(targetDate);
            if (!scheduleFlags.HasFlag(dayFlag))
                continue;

            var current = startTime;
            var maxIterations = 1440 / intervalMinutes + 1; // At most 24 hours of intervals, plus one
            var iterations = 0;
            while (current <= endTime && iterations < maxIterations) {
                var scheduledDate = targetDate.Add(current.ToTimeSpan());
                if (scheduledDate > now.Value && (!nextScheduledDate.HasValue || scheduledDate < nextScheduledDate.Value))
                    nextScheduledDate = scheduledDate;

                var previousCurrent = current;
                current = current.AddMinutes(intervalMinutes);
                iterations++;

                // Stop if we wrapped past midnight or current did not move.
                if (current < previousCurrent || current == previousCurrent)
                    break;
            }
        }

        return nextScheduledDate ?? throw new InvalidOperationException("No valid scheduled datetime found.");
    }

    /// <summary>Returns whether a scheduled job is past due, given schedule times and last-run time.</summary>
    /// <param name="usStateAbbreviation">US state abbreviation</param>
    /// <param name="scheduleTimes">Scheduled times of day</param>
    /// <param name="scheduleFlags">Day flags that mark which weekdays are scheduled</param>
    /// <param name="lastRunDateTime">Last run in UTC. Null means 7 days ago.</param>
    /// <returns>True when the schedule is past due; otherwise false</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="scheduleTimes" /> is null</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="scheduleTimes" /> is empty</exception>
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

        // Walk from last-run date forward to now (capped at DefaultMaxDaysPastDueCheck days back).
        var startDate = localizedLastRunTimestamp.Value.Date;
        var endDate = now.Value.Date;
        var daysBetween = (endDate - startDate).Days + 1;

        // If last run is older than DefaultMaxDaysPastDueCheck days, only inspect the last DefaultMaxDaysPastDueCheck days.
        if (daysBetween > DefaultMaxDaysPastDueCheck) {
            startDate = endDate.AddDays(-(DefaultMaxDaysPastDueCheck - 1)); // Last N days, today included
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
                // Past due when this tick is after last run and at or before now.
                if (scheduledDate > localizedLastRunTimestamp.Value && scheduledDate <= now.Value)
                    return true;
            }
        }

        return false;
    }

    /// <summary>Interval-based past-due check that only looks at <em>today's</em> local window in the given state.</summary>
    /// <remarks>
    /// Unlike the discrete-time overload, this method does not walk historical days: it returns <see langword="false" /> unless the current local day matches
    /// <paramref name="scheduleFlags" />, the clock is inside <c>[startTime, endTime]</c>, and the latest completed tick strictly after <paramref name="lastRunDateTime" /> (localized) is
    /// still in the past relative to "now".
    /// </remarks>
    /// <param name="usStateAbbreviation">State whose local civil time defines "today" and the window.</param>
    /// <param name="startTime">Inclusive window start.</param>
    /// <param name="endTime">Inclusive window end.</param>
    /// <param name="minuteInterval">Positive minute stride between ticks.</param>
    /// <param name="scheduleFlags">Bitmask of weekdays that participate.</param>
    /// <param name="lastRunDateTime">Last successful run in UTC; defaults to seven days ago when <see langword="null" />.</param>
    /// <returns><see langword="true" /> when a tick should have fired after the localized last run but before now.</returns>
    /// <exception cref="ArgumentException"><paramref name="startTime" /> is after <paramref name="endTime" />, or <paramref name="minuteInterval" /> is not positive.</exception>
    /// <exception cref="InvalidOperationException">Local time or last-run localization failed for the state.</exception>
    public static bool IsPastDue(USState usStateAbbreviation, TimeOnly startTime, TimeOnly endTime, int minuteInterval, DayFlags scheduleFlags, DateTime? lastRunDateTime)
    {
        ArgumentHelpers.ThrowIf(startTime > endTime, "Start time must be less than or equal to end time.", nameof(startTime));
        ArgumentHelpers.ThrowIfNegativeOrZero(minuteInterval);
        var now = GetCurrentLocalDateTime(usStateAbbreviation);
        OperationHelpers.ThrowIfNull(now, $"Couldn't get local datetime for {usStateAbbreviation}");
        var defaultLastRun = lastRunDateTime ?? DateTime.UtcNow.AddDays(-DefaultMaxDaysPastDueCheck);
        var lastRun = ConvertToLocalTime(usStateAbbreviation, defaultLastRun);
        OperationHelpers.ThrowIfNull(lastRun, $"Couldn't convert last run datetime to local time for {usStateAbbreviation}");

        // Today only.
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

    /// <summary>Yields concrete local <see cref="DateTime" /> instants on a calendar day for discrete schedule times.</summary>
    /// <remarks>
    /// Uses <paramref name="date" />.<see cref="DateTime.Date" /> only — caller must supply the intended civil calendar day (this overload does not take a
    /// <see cref="Lyo.Common.Core.Enums.USState" />). Combine with <see cref="ConvertToLocalTime" /> first if you need another zone’s calendar.
    /// </remarks>
    /// <param name="date">Any instant on the target day; only its date component is used.</param>
    /// <param name="scheduleTimes">Clock times to materialize on that date.</param>
    /// <param name="scheduleFlags">Bitmask of weekdays that participate in the schedule.</param>
    /// <returns>Ordered local instants on <paramref name="date" /> when the weekday matches; otherwise empty.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="scheduleTimes" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="scheduleTimes" /> is empty.</exception>
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

    /// <summary>Yields interval-based schedule ticks on a calendar day inside <c>[startTime, endTime]</c>.</summary>
    /// <remarks>Uses <paramref name="date" />.<see cref="DateTime.Date" /> only; caller supplies the intended civil day (no <see cref="Lyo.Common.Core.Enums.USState" /> parameter).</remarks>
    /// <param name="date">Any instant on the target day; only its date component is used.</param>
    /// <param name="startTime">Inclusive window start (local time-of-day).</param>
    /// <param name="endTime">Inclusive window end (local time-of-day).</param>
    /// <param name="intervalMinutes">Positive minute stride between generated instants.</param>
    /// <param name="scheduleFlags">Bitmask of weekdays that participate in the schedule.</param>
    /// <returns>Local instants for every tick in the window when the weekday matches; otherwise empty.</returns>
    /// <exception cref="ArgumentException"><paramref name="startTime" /> is after <paramref name="endTime" />, or <paramref name="intervalMinutes" /> is not positive.</exception>
    public static IEnumerable<DateTime> GetScheduledTimesForDay(DateTime date, TimeOnly startTime, TimeOnly endTime, int intervalMinutes, DayFlags scheduleFlags)
    {
        ArgumentHelpers.ThrowIf(startTime > endTime, "Start time must be less than or equal to end time.", nameof(startTime));
        ArgumentHelpers.ThrowIfNegativeOrZero(intervalMinutes);
        var dayFlag = GetDayFlagForDate(date);
        if (!scheduleFlags.HasFlag(dayFlag))
            yield break;

        var current = startTime;
        while (current <= endTime) {
            yield return date.Date.Add(current.ToTimeSpan());

            current = current.AddMinutes(intervalMinutes);
        }
    }

    /// <summary>Returns whether a <see cref="DateTime" /> falls on a scheduled weekday per the day flags.</summary>
    /// <param name="dateTime"><see cref="DateTime" /> to test</param>
    /// <param name="scheduleFlags">Day flags that mark which weekdays are scheduled</param>
    /// <returns>True when the instant falls on a scheduled day; otherwise false</returns>
    public static bool IsScheduledDay(DateTime dateTime, DayFlags scheduleFlags)
    {
        var dayFlag = GetDayFlagForDate(dateTime);
        return scheduleFlags.HasFlag(dayFlag);
    }

    /// <summary>Maps a date to its <see cref="DayFlags" /> value.</summary>
    /// <param name="date">Date whose weekday is mapped</param>
    /// <returns><see cref="DayFlags" /> bit for that day of week</returns>
    private static DayFlags GetDayFlagForDate(DateTime date) => (DayFlags)(1 << (int)date.DayOfWeek);
}