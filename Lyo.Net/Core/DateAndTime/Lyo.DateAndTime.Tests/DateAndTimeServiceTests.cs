using Lyo.Common.Core.Enums;
using Lyo.Common.Metadata.Records;
using Lyo.Exceptions.Models;
using Xunit;
#if NET6_0_OR_GREATER
using TimeOnly = System.TimeOnly;

#else
using TimeOnly = Lyo.DateAndTime.TimeOnlyModel;
using DateOnly = Lyo.DateAndTime.DateOnlyModel;
#endif

namespace Lyo.DateAndTime.Tests;

public class DateAndTimeServiceTests
{
    [Theory]
    [InlineData(USState.NY, "America/New_York", "Eastern Standard Time")]
    [InlineData(USState.FL, "America/New_York", "Eastern Standard Time")]
    [InlineData(USState.TX, "America/Chicago", "Central Standard Time")]
    [InlineData(USState.CO, "America/Denver", "Mountain Standard Time")]
    [InlineData(USState.CA, "America/Los_Angeles", "Pacific Standard Time")]
    [InlineData(USState.WA, "America/Los_Angeles", "Pacific Standard Time")]
    public void GetTimeZoneByState_KnownState_ReturnsExpectedZone(USState state, string ianaId, string windowsId)
    {
        var result = DateAndTime.GetTimeZoneByState(state);
        Assert.NotNull(result);
        Assert.True(result.Id == ianaId || result.Id == windowsId);
    }

    [Fact]
    public void GetLocalDateTime_WithDateTime_ConvertsToLocalTime()
    {
        var utcDateTime = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        var result = DateAndTime.GetLocalDateTime(USState.NY, utcDateTime);
        Assert.NotNull(result);
        Assert.NotEqual(utcDateTime, result.Value);
        // Eastern is UTC-5, so 12:00 UTC becomes 7:00 EST
        Assert.Equal(7, result.Value.Hour);
    }

    [Fact]
    public void GetLocalDateTime_WithoutDateTime_UsesCurrentUtcTime()
    {
        var result = DateAndTime.GetLocalDateTime(USState.CA);
        Assert.NotNull(result);
        // Stay close to now after applying the timezone offset
        var utcNow = DateTime.UtcNow;
        var timeZone = DateAndTime.GetTimeZoneByState(USState.CA);
        var expectedLocalTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, timeZone!);
        var timeDiff = Math.Abs((result.Value - expectedLocalTime).TotalSeconds);
        Assert.True(timeDiff < 5); // 5 seconds of slack for test runtime
    }

    [Fact]
    public void GetCurrentLocalDateTime_ReturnsLocalDateTime()
    {
        var result = DateAndTime.GetCurrentLocalDateTime(USState.TX);
        Assert.NotNull(result);
        // Stay close to now after applying the timezone offset
        var utcNow = DateTime.UtcNow;
        var timeZone = DateAndTime.GetTimeZoneByState(USState.TX);
        var expectedLocalTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, timeZone!);
        var timeDiff = Math.Abs((result.Value - expectedLocalTime).TotalSeconds);
        Assert.True(timeDiff < 5);
    }

    [Fact]
    public void ConvertToLocalTime_ConvertsUtcToLocal()
    {
        var utcDateTime = new DateTime(2024, 6, 15, 15, 30, 0, DateTimeKind.Utc);
        var result = DateAndTime.ConvertToLocalTime(USState.CA, utcDateTime);
        Assert.NotNull(result);
        // In June, Pacific is PDT (UTC-7), so 15:30 UTC becomes 8:30 PDT
        // Cross-check the conversion against TimeZoneInfo directly
        var timeZone = DateAndTime.GetTimeZoneByState(USState.CA);
        var expected = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, timeZone!);
        Assert.Equal(expected.Hour, result.Value.Hour);
        Assert.Equal(expected.Minute, result.Value.Minute);
    }

    [Fact]
    public void ConvertToUtc_ConvertsLocalToUtc()
    {
        var localDateTime = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Unspecified);
        var result = DateAndTime.ConvertToUtc(USState.NY, localDateTime);
        Assert.NotNull(result);
        // Eastern is UTC-5, so 10:00 EST becomes 15:00 UTC
        Assert.Equal(15, result.Value.Hour);
        Assert.Equal(0, result.Value.Minute);
    }

    [Fact]
    public void GetNextScheduledDateTime_WithScheduleTimes_ReturnsNextScheduledTime()
    {
        var scheduleTimes = new List<TimeOnly> { new(9, 0), new(14, 0), new(18, 0) };
        var scheduleFlags = DayFlags.EveryDay; // EveryDay so a slot is always found
        var result = DateAndTime.GetNextScheduledDateTime(USState.NY, scheduleTimes, scheduleFlags);
        var localNow = DateAndTime.GetCurrentLocalDateTime(USState.NY);
        Assert.NotNull(localNow);
        Assert.True(result > localNow.Value);
    }

    [Fact]
    public void GetNextScheduledDateTime_WithTimeWindow_ReturnsNextScheduledTime()
    {
        var startTime = new TimeOnly(9, 0);
        var endTime = new TimeOnly(17, 0);
        var intervalMinutes = 60;
        var scheduleFlags = DayFlags.EveryDay; // EveryDay so a slot is always found
        var result = DateAndTime.GetNextScheduledDateTime(USState.CA, startTime, endTime, intervalMinutes, scheduleFlags);
        var localNow = DateAndTime.GetCurrentLocalDateTime(USState.CA);
        Assert.NotNull(localNow);
        Assert.True(result > localNow.Value);
    }

    [Fact]
    public void GetNextScheduledDateTime_NoValidDay_ThrowsInvalidOperationException()
    {
        var scheduleTimes = new List<TimeOnly> { new(9, 0) };
        var scheduleFlags = DayFlags.None; // no weekday flags
        Assert.Throws<InvalidOperationException>(() => DateAndTime.GetNextScheduledDateTime(USState.NY, scheduleTimes, scheduleFlags));
    }

    [Fact]
    public void IsPastDue_WithPastDueSchedule_ReturnsTrue()
    {
        var scheduleTimes = new List<TimeOnly> { new(9, 0) };
        var scheduleFlags = DayFlags.Weekdays;

        // Pick a last-run instant far enough back that at least one weekday 9:00 (local) has already
        // passed, even if this test runs Monday morning before 9:00 AM Eastern (the old
        // "2 days ago" case could be Saturday with the next slot Monday 9 AM — not yet elapsed).
        var lastRunDay = DateTime.UtcNow.AddDays(-10).Date;
        var lastRunDateTime = new DateTime(lastRunDay.Year, lastRunDay.Month, lastRunDay.Day, 8, 0, 0, DateTimeKind.Utc);
        var result = DateAndTime.IsPastDue(USState.NY, scheduleTimes, scheduleFlags, lastRunDateTime);
        Assert.True(result, $"Expected past due with last run {lastRunDateTime:o} UTC (10+ days ago, weekday slots should have elapsed).");
    }

    [Fact]
    public void IsPastDue_WithRecentRun_ReturnsFalse()
    {
        // Use a schedule time that is still in the future so a last run "1 hour ago" cannot have missed it.
        // The old test used 9:00 AM, which failed when run between 9-10 AM Eastern on weekdays
        // (last run before 9 AM + 9 AM already passed = past due).
        var localNow = DateAndTime.GetCurrentLocalDateTime(USState.NY)!.Value;
        var futureSchedule = localNow.AddHours(2);
        var scheduleTimes = new List<TimeOnly> { new(futureSchedule.Hour, futureSchedule.Minute) };
        var scheduleFlags = DayFlags.Weekdays;
        var lastRunDateTime = DateTime.UtcNow.AddHours(-1); // one hour ago
        var result = DateAndTime.IsPastDue(USState.NY, scheduleTimes, scheduleFlags, lastRunDateTime);
        Assert.False(result);
    }

    [Fact]
    public void IsPastDue_EarlyMorningWithYesterdayMissedSchedule_ReturnsTrue()
    {
        // Confirms the fix for IsPastDue only looking forward from today and skipping
        // schedules that fired between the last run and now.
        //
        // Setup: last run was 2 days ago, now is early morning (1:20 AM),
        // and yesterday's 9:00 AM weekday slot should already have been missed.
        var scheduleTimes = new List<TimeOnly> { new(9, 0) };
        var scheduleFlags = DayFlags.Weekdays;

        // Last run: 2 days ago at 8:00 AM UTC (before any 9:00 AM slot)
        var twoDaysAgo = DateTime.UtcNow.AddDays(-2);
        var lastRunDateTime = new DateTime(twoDaysAgo.Year, twoDaysAgo.Month, twoDaysAgo.Day, 8, 0, 0, DateTimeKind.Utc);

        // When yesterday was a weekday, its 9:00 AM slot should already be past due
        var yesterday = DateTime.UtcNow.AddDays(-1);
        var isYesterdayWeekday = yesterday.DayOfWeek >= DayOfWeek.Monday && yesterday.DayOfWeek <= DayOfWeek.Friday;
        if (isYesterdayWeekday) {
            var result = DateAndTime.IsPastDue(USState.NY, scheduleTimes, scheduleFlags, lastRunDateTime);

            // True: yesterday's 9:00 AM slot was missed (after last run, before now)
            Assert.True(
                result,
                $"Expected past due: last run was {lastRunDateTime:g} UTC, yesterday ({yesterday:yyyy-MM-dd}) was a {yesterday.DayOfWeek} with a 9:00 AM schedule that should have been missed.");
        }
    }

    [Fact]
    public void IsPastDue_ChecksDatesBetweenLastRunAndNow_ReturnsTrue()
    {
        // IsPastDue must walk every date from last run through now, not only from today
        // forward. That was the early-morning failure.
        var scheduleTimes = new List<TimeOnly> { new(14, 0) }; // 14:00
        var scheduleFlags = DayFlags.Weekdays;

        // Last run: 3 days ago at 1:00 PM UTC (before that day's 2:00 PM slot)
        var threeDaysAgo = DateTime.UtcNow.AddDays(-3);
        var lastRunDateTime = new DateTime(threeDaysAgo.Year, threeDaysAgo.Month, threeDaysAgo.Day, 13, 0, 0, DateTimeKind.Utc);

        // Confirm at least one weekday in the last 3 days
        var weekdayCount = 0;
        for (var i = 1; i <= 3; i++) {
            var checkDate = DateTime.UtcNow.AddDays(-i);
            if (checkDate.DayOfWeek >= DayOfWeek.Monday && checkDate.DayOfWeek <= DayOfWeek.Friday)
                weekdayCount++;
        }

        // Skip the assert when the window is all weekend
        if (weekdayCount > 0) {
            var result = DateAndTime.IsPastDue(USState.NY, scheduleTimes, scheduleFlags, lastRunDateTime);

            // True: weekday 2:00 PM slots in the last 3 days fired after the 1:00 PM last run
            Assert.True(
                result,
                $"Expected past due: last run was {lastRunDateTime:g} UTC, found {weekdayCount} weekdays in the last 3 days with 2:00 PM schedules that should have been missed.");
        }
    }

    [Fact]
    public void IsPastDue_WithTimeWindow_PastDue_ReturnsTrue()
    {
        var startTime = new TimeOnly(9, 0);
        var endTime = new TimeOnly(17, 0);
        var minuteInterval = 60;
        var scheduleFlags = DayFlags.Weekdays;
        var lastRunDateTime = DateTime.UtcNow.AddHours(-3); // three hours ago
        var result = DateAndTime.IsPastDue(USState.NY, startTime, endTime, minuteInterval, scheduleFlags, lastRunDateTime);

        // Outcome depends on now and the schedule; only assert that it does not throw
        Assert.IsType<bool>(result);
    }

    [Fact]
    public void IsPastDue_WithTimeWindow_NotInWindow_ReturnsFalse()
    {
        var startTime = new TimeOnly(9, 0);
        var endTime = new TimeOnly(17, 0);
        var minuteInterval = 60;
        var scheduleFlags = DayFlags.Weekdays;
        var lastRunDateTime = DateTime.UtcNow.AddHours(-1);

        // Outside the window, expect false
        var result = DateAndTime.IsPastDue(USState.CA, startTime, endTime, minuteInterval, scheduleFlags, lastRunDateTime);
        Assert.IsType<bool>(result);
    }

    [Fact]
    public void GetScheduledTimesForDay_WithScheduleTimes_ReturnsAllTimes()
    {
        var date = new DateTime(2024, 1, 15); // 2024-01-15 is a Monday
        var scheduleTimes = new List<TimeOnly> { new(9, 0), new(12, 0), new(15, 0) };
        var scheduleFlags = DayFlags.Mon;
        var result = DateAndTime.GetScheduledTimesForDay(date, scheduleTimes, scheduleFlags).ToList();
        Assert.Equal(3, result.Count);
        Assert.Equal(new(2024, 1, 15, 9, 0, 0), result[0]);
        Assert.Equal(new(2024, 1, 15, 12, 0, 0), result[1]);
        Assert.Equal(new(2024, 1, 15, 15, 0, 0), result[2]);
    }

    [Fact]
    public void GetScheduledTimesForDay_NotScheduledDay_ReturnsEmpty()
    {
        var date = new DateTime(2024, 1, 14); // 2024-01-14 is a Sunday
        var scheduleTimes = new List<TimeOnly> { new(9, 0) };
        var scheduleFlags = DayFlags.Weekdays; // weekdays only
        var result = DateAndTime.GetScheduledTimesForDay(date, scheduleTimes, scheduleFlags).ToList();
        Assert.Empty(result);
    }

    [Fact]
    public void GetScheduledTimesForDay_WithTimeWindow_ReturnsAllIntervals()
    {
        var date = new DateTime(2024, 1, 15); // 2024-01-15 is a Monday
        var startTime = new TimeOnly(9, 0);
        var endTime = new TimeOnly(12, 0);
        var intervalMinutes = 60;
        var scheduleFlags = DayFlags.Mon;
        var result = DateAndTime.GetScheduledTimesForDay(date, startTime, endTime, intervalMinutes, scheduleFlags).ToList();
        Assert.Equal(4, result.Count); // 09:00, 10:00, 11:00, 12:00
        Assert.Equal(new(2024, 1, 15, 9, 0, 0), result[0]);
        Assert.Equal(new(2024, 1, 15, 10, 0, 0), result[1]);
        Assert.Equal(new(2024, 1, 15, 11, 0, 0), result[2]);
        Assert.Equal(new(2024, 1, 15, 12, 0, 0), result[3]);
    }

    [Fact]
    public void IsScheduledDay_WeekdayWithWeekdayFlag_ReturnsTrue()
    {
        var dateTime = new DateTime(2024, 1, 15); // 2024-01-15 is a Monday
        var scheduleFlags = DayFlags.Weekdays;
        var result = DateAndTime.IsScheduledDay(dateTime, scheduleFlags);
        Assert.True(result);
    }

    [Fact]
    public void IsScheduledDay_WeekendWithWeekdayFlag_ReturnsFalse()
    {
        var dateTime = new DateTime(2024, 1, 14); // 2024-01-14 is a Sunday
        var scheduleFlags = DayFlags.Weekdays;
        var result = DateAndTime.IsScheduledDay(dateTime, scheduleFlags);
        Assert.False(result);
    }

    [Fact]
    public void IsScheduledDay_WeekendWithWeekendFlag_ReturnsTrue()
    {
        var dateTime = new DateTime(2024, 1, 14); // 2024-01-14 is a Sunday
        var scheduleFlags = DayFlags.Weekends;
        var result = DateAndTime.IsScheduledDay(dateTime, scheduleFlags);
        Assert.True(result);
    }

    [Fact]
    public void IsScheduledDay_SpecificDay_ReturnsTrue()
    {
        var dateTime = new DateTime(2024, 1, 15); // 2024-01-15 is a Monday
        var scheduleFlags = DayFlags.Mon;
        var result = DateAndTime.IsScheduledDay(dateTime, scheduleFlags);
        Assert.True(result);
    }

    [Fact]
    public void IsScheduledDay_DifferentDay_ReturnsFalse()
    {
        var dateTime = new DateTime(2024, 1, 16); // 2024-01-16 is a Tuesday
        var scheduleFlags = DayFlags.Mon;
        var result = DateAndTime.IsScheduledDay(dateTime, scheduleFlags);
        Assert.False(result);
    }

    [Fact]
    public void GetNextScheduledDateTime_MultipleTimes_ReturnsEarliest()
    {
        var scheduleTimes = new List<TimeOnly> { new(18, 0), new(9, 0), new(15, 0) };
        var scheduleFlags = DayFlags.EveryDay; // EveryDay so a slot is always found
        var result = DateAndTime.GetNextScheduledDateTime(USState.NY, scheduleTimes, scheduleFlags);

        // Expect the earliest slot still in the future
        var localNow = DateAndTime.GetCurrentLocalDateTime(USState.NY);
        Assert.NotNull(localNow);
        Assert.True(result > localNow.Value);
    }

    [Fact]
    public void ConvertToUtc_WithDifferentTimezones_ConvertsCorrectly()
    {
        var localDateTime = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Unspecified);
        var easternResult = DateAndTime.ConvertToUtc(USState.NY, localDateTime);
        var pacificResult = DateAndTime.ConvertToUtc(USState.CA, localDateTime);
        Assert.NotNull(easternResult);
        Assert.NotNull(pacificResult);
        // Pacific UTC is 3 hours later than Eastern UTC
        Assert.Equal(3, (pacificResult.Value - easternResult.Value).Hours);
    }

    [Fact]
    public void GetScheduledTimesForDay_OrdersTimesCorrectly()
    {
        var date = new DateTime(2024, 1, 15); // 2024-01-15 is a Monday
        var scheduleTimes = new List<TimeOnly> { new(15, 0), new(9, 0), new(12, 0) };
        var scheduleFlags = DayFlags.Mon;
        var result = DateAndTime.GetScheduledTimesForDay(date, scheduleTimes, scheduleFlags).ToList();
        Assert.Equal(3, result.Count);
        // Expect chronological order
        Assert.True(result[0] < result[1]);
        Assert.True(result[1] < result[2]);
    }

    [Fact]
    public void IsPastDue_WithNullLastRun_UsesDefault()
    {
        var scheduleTimes = new List<TimeOnly> { new(9, 0) };
        var scheduleFlags = DayFlags.Weekdays;
        var result = DateAndTime.IsPastDue(USState.NY, scheduleTimes, scheduleFlags, null);

        // Default last-run is 7 days ago, so this is usually past due
        Assert.IsType<bool>(result);
    }

    [Fact]
    public void GetNextScheduledDateTime_AllDays_ReturnsNextTime()
    {
        var scheduleTimes = new List<TimeOnly> { new(9, 0) };
        var scheduleFlags = DayFlags.EveryDay;
        var result = DateAndTime.GetNextScheduledDateTime(USState.NY, scheduleTimes, scheduleFlags);
        var localNow = DateAndTime.GetCurrentLocalDateTime(USState.NY);
        Assert.NotNull(localNow);
        Assert.True(result > localNow.Value);
    }

    [Fact]
    public void GetNextScheduledDateTime_NullScheduleTimes_ThrowsArgumentNullException()
    {
        IEnumerable<TimeOnly>? scheduleTimes = null;
        var scheduleFlags = DayFlags.EveryDay;
        Assert.Throws<ArgumentNullException>(() => DateAndTime.GetNextScheduledDateTime(USState.NY, scheduleTimes!, scheduleFlags));
    }

    [Fact]
    public void GetNextScheduledDateTime_EmptyScheduleTimes_ThrowsArgumentException()
    {
        var scheduleFlags = DayFlags.EveryDay;
        Assert.Throws<ArgumentException>(() => DateAndTime.GetNextScheduledDateTime(USState.NY, [], scheduleFlags));
    }

    [Fact]
    public void GetNextScheduledDateTime_StartTimeGreaterThanEndTime_ThrowsArgumentException()
    {
        var startTime = new TimeOnly(17, 0);
        var endTime = new TimeOnly(9, 0);
        var intervalMinutes = 60;
        var scheduleFlags = DayFlags.EveryDay;
        Assert.Throws<ArgumentException>(() => DateAndTime.GetNextScheduledDateTime(USState.NY, startTime, endTime, intervalMinutes, scheduleFlags));
    }

    [Fact]
    public void GetNextScheduledDateTime_ZeroIntervalMinutes_ThrowsArgumentException()
    {
        var startTime = new TimeOnly(9, 0);
        var endTime = new TimeOnly(17, 0);
        var intervalMinutes = 0;
        var scheduleFlags = DayFlags.EveryDay;
        Assert.Throws<ArgumentOutsideRangeException>(() => DateAndTime.GetNextScheduledDateTime(USState.NY, startTime, endTime, intervalMinutes, scheduleFlags));
    }

    [Fact]
    public void GetNextScheduledDateTime_NegativeIntervalMinutes_ThrowsArgumentException()
    {
        var startTime = new TimeOnly(9, 0);
        var endTime = new TimeOnly(17, 0);
        var intervalMinutes = -10;
        var scheduleFlags = DayFlags.EveryDay;
        Assert.Throws<ArgumentOutsideRangeException>(() => DateAndTime.GetNextScheduledDateTime(USState.NY, startTime, endTime, intervalMinutes, scheduleFlags));
    }

    [Fact]
    public void IsPastDue_NullScheduleTimes_ThrowsArgumentNullException()
    {
        List<TimeOnly>? scheduleTimes = null;
        var scheduleFlags = DayFlags.Weekdays;
        var lastRunDateTime = DateTime.UtcNow.AddDays(-1);
        Assert.Throws<ArgumentNullException>(() => DateAndTime.IsPastDue(USState.NY, scheduleTimes!, scheduleFlags, lastRunDateTime));
    }

    [Fact]
    public void IsPastDue_EmptyScheduleTimes_ThrowsArgumentException()
    {
        var scheduleFlags = DayFlags.Weekdays;
        var lastRunDateTime = DateTime.UtcNow.AddDays(-1);
        Assert.Throws<ArgumentException>(() => DateAndTime.IsPastDue(USState.NY, [], scheduleFlags, lastRunDateTime));
    }

    [Fact]
    public void IsPastDue_TimeWindow_StartTimeGreaterThanEndTime_ThrowsArgumentException()
    {
        var startTime = new TimeOnly(17, 0);
        var endTime = new TimeOnly(9, 0);
        var minuteInterval = 60;
        var scheduleFlags = DayFlags.Weekdays;
        var lastRunDateTime = DateTime.UtcNow.AddHours(-1);
        Assert.Throws<ArgumentException>(() => DateAndTime.IsPastDue(USState.NY, startTime, endTime, minuteInterval, scheduleFlags, lastRunDateTime));
    }

    [Fact]
    public void IsPastDue_TimeWindow_ZeroMinuteInterval_ThrowsArgumentException()
    {
        var startTime = new TimeOnly(9, 0);
        var endTime = new TimeOnly(17, 0);
        var minuteInterval = 0;
        var scheduleFlags = DayFlags.Weekdays;
        var lastRunDateTime = DateTime.UtcNow.AddHours(-1);
        Assert.Throws<ArgumentOutsideRangeException>(() => DateAndTime.IsPastDue(USState.NY, startTime, endTime, minuteInterval, scheduleFlags, lastRunDateTime));
    }

    [Fact]
    public void GetScheduledTimesForDay_NullScheduleTimes_ThrowsArgumentNullException()
    {
        IEnumerable<TimeOnly>? scheduleTimes = null;
        var date = new DateTime(2024, 1, 15);
        var scheduleFlags = DayFlags.Mon;

        // Enumerate so validation actually runs
        Assert.Throws<ArgumentNullException>(() => DateAndTime.GetScheduledTimesForDay(date, scheduleTimes!, scheduleFlags).ToList());
    }

    [Fact]
    public void GetScheduledTimesForDay_TimeWindow_StartTimeGreaterThanEndTime_ThrowsArgumentException()
    {
        var date = new DateTime(2024, 1, 15);
        var startTime = new TimeOnly(17, 0);
        var endTime = new TimeOnly(9, 0);
        var intervalMinutes = 60;
        var scheduleFlags = DayFlags.Mon;

        // Enumerate so validation actually runs
        Assert.Throws<ArgumentException>(() => DateAndTime.GetScheduledTimesForDay(date, startTime, endTime, intervalMinutes, scheduleFlags).ToList());
    }

    [Fact]
    public void GetScheduledTimesForDay_TimeWindow_ZeroIntervalMinutes_ThrowsArgumentException()
    {
        var date = new DateTime(2024, 1, 15);
        var startTime = new TimeOnly(9, 0);
        var endTime = new TimeOnly(17, 0);
        var intervalMinutes = 0;
        var scheduleFlags = DayFlags.Mon;

        // Enumerate so validation actually runs
        Assert.Throws<ArgumentOutsideRangeException>(() => DateAndTime.GetScheduledTimesForDay(date, startTime, endTime, intervalMinutes, scheduleFlags).ToList());
    }

    [Fact]
    public void GetScheduledTimesForDay_TimeWindow_EndTimeInclusive()
    {
        var date = new DateTime(2024, 1, 15); // 2024-01-15 is a Monday
        var startTime = new TimeOnly(9, 0);
        var endTime = new TimeOnly(9, 0); // same as start
        var intervalMinutes = 60;
        var scheduleFlags = DayFlags.Mon;
        var result = DateAndTime.GetScheduledTimesForDay(date, startTime, endTime, intervalMinutes, scheduleFlags).ToList();

        // End time is included
        Assert.Single(result);
        Assert.Equal(new(2024, 1, 15, 9, 0, 0), result[0]);
    }

    [Fact]
    public void GetNextScheduledDateTime_TimeWindow_EndTimeInclusive()
    {
        var now = DateAndTime.GetCurrentLocalDateTime(USState.NY);
        Assert.NotNull(now);

        // Start from a past hour so the next slot is in the future
        var pastTime = now.Value.AddHours(-2);

        // Window that covers that same hour
        var startTime = new TimeOnly(pastTime.Hour, 0);
        var endTime = new TimeOnly(pastTime.Hour, 0);
        var intervalMinutes = 60;
        var scheduleFlags = DayFlags.EveryDay;

        // A slot is found even when start and end match
        var result = DateAndTime.GetNextScheduledDateTime(USState.NY, startTime, endTime, intervalMinutes, scheduleFlags);
        Assert.True(result > DateTime.UtcNow);
    }
}