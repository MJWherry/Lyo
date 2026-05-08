using Lyo.Common.Enums;
using Lyo.Exceptions;
#if NET6_0_OR_GREATER
using TimeOnly = System.TimeOnly;
#else
using TimeOnly = Lyo.DateAndTime.TimeOnlyModel;
#endif

namespace Lyo.DateAndTime;

/// <summary>US-state timezone conversions and day-of-week scheduling helpers anchored in each state’s local civil time.</summary>
/// <remarks>
/// <para>
/// Time zones resolve through <see cref="Lyo.Common.Enums.GeographicInfo.FromState" /> (IANA ids such as <c>America/New_York</c>). When the OS cannot resolve an id,
/// conversion members return <see langword="null" /> rather than throwing.
/// </para>
/// <para>
/// Scheduling interprets <see cref="DayFlags" /> with one bit per weekday. Next-run search scans at most seven local midnights ahead; discrete <c>IsPastDue</c> scans cap
/// backward history similarly. The interval-based <c>IsPastDue</c> overload only evaluates <em>today’s</em> local window — unlike the discrete-times overload which walks
/// multiple days.
/// </para>
/// <para>
/// On .NET 6+, APIs use <see cref="TimeOnly" />; on .NET Standard 2.0 use <see cref="TimeOnlyModel" /> instead (this library aliases the type internally).
/// </para>
/// </remarks>
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

    /// <summary>Interval-based past-due check that only inspects <em>today’s</em> local window in the given state.</summary>
    /// <remarks>
    /// Unlike the discrete-time overload, this method does not walk historical days: it returns <see langword="false" /> unless the current local day matches
    /// <paramref name="scheduleFlags" />, the clock is inside <c>[startTime, endTime]</c>, and the latest completed tick strictly after
    /// <paramref name="lastRunDateTime" /> (localized) is still in the past relative to “now”.
    /// </remarks>
    /// <param name="usStateAbbreviation">State whose local civil time defines “today” and the window.</param>
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

    /// <summary>Enumerates concrete local <see cref="DateTime" /> instants on a calendar day for discrete schedule times.</summary>
    /// <remarks>
    /// Uses <paramref name="date" />.<see cref="DateTime.Date" /> only — caller must supply the intended civil calendar day (this overload does not take a
    /// <see cref="Lyo.Common.Enums.USState" />). Combine with <see cref="ConvertToLocalTime" /> first if you need another zone’s calendar.
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

    /// <summary>Enumerates interval-based schedule ticks on a calendar day inside <c>[startTime, endTime]</c>.</summary>
    /// <remarks>Uses <paramref name="date" />.<see cref="DateTime.Date" /> only; caller supplies the intended civil day (no <see cref="Lyo.Common.Enums.USState" /> parameter).</remarks>
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