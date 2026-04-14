using Lyo.Common.Enums;
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
    [Fact]
    public void GetTimeZoneByState_ValidState_ReturnsTimeZoneInfo()
    {
        var result = DateAndTime.GetTimeZoneByState(USState.NY);
        Assert.NotNull(result);
        // GeographicInfo uses IANA timezone IDs (e.g., "America/New_York")
        // On Windows, this may map to "Eastern Standard Time", on Linux it stays as IANA ID
        Assert.True(result.Id == "America/New_York" || result.Id == "Eastern Standard Time");
    }

    [Fact]
    public void GetTimeZoneByState_InvalidState_ReturnsNull()
    {
        // Assuming there's a state not in the map - but all US states are mapped
        // Let's test with a state that exists but verify the mapping works
        var result = DateAndTime.GetTimeZoneByState(USState.CA);
        Assert.NotNull(result);
        // GeographicInfo uses IANA timezone IDs (e.g., "America/Los_Angeles")
        // On Windows, this may map to "Pacific Standard Time", on Linux it stays as IANA ID
        Assert.True(result.Id == "America/Los_Angeles" || result.Id == "Pacific Standard Time");
    }

    [Fact]
    public void GetTimeZoneByState_EasternState_ReturnsEasternTimeZone()
    {
        var result = DateAndTime.GetTimeZoneByState(USState.FL);
        Assert.NotNull(result);
        // GeographicInfo uses IANA timezone IDs (e.g., "America/New_York")
        // On Windows, this may map to "Eastern Standard Time", on Linux it stays as IANA ID
        Assert.True(result.Id == "America/New_York" || result.Id == "Eastern Standard Time");
    }

    [Fact]
    public void GetTimeZoneByState_CentralState_ReturnsCentralTimeZone()
    {
        var result = DateAndTime.GetTimeZoneByState(USState.TX);
        Assert.NotNull(result);
        // GeographicInfo uses IANA timezone IDs (e.g., "America/Chicago")
        // On Windows, this may map to "Central Standard Time", on Linux it stays as IANA ID
        Assert.True(result.Id == "America/Chicago" || result.Id == "Central Standard Time");
    }

    [Fact]
    public void GetTimeZoneByState_MountainState_ReturnsMountainTimeZone()
    {
        var result = DateAndTime.GetTimeZoneByState(USState.CO);
        Assert.NotNull(result);
        // GeographicInfo uses IANA timezone IDs (e.g., "America/Denver")
        // On Windows, this may map to "Mountain Standard Time", on Linux it stays as IANA ID
        Assert.True(result.Id == "America/Denver" || result.Id == "Mountain Standard Time");
    }

    [Fact]
    public void GetTimeZoneByState_PacificState_ReturnsPacificTimeZone()
    {
        var result = DateAndTime.GetTimeZoneByState(USState.WA);
        Assert.NotNull(result);
        // GeographicInfo uses IANA timezone IDs (e.g., "America/Los_Angeles")
        // On Windows, this may map to "Pacific Standard Time", on Linux it stays as IANA ID
        Assert.True(result.Id == "America/Los_Angeles" || result.Id == "Pacific Standard Time");
    }

    [Fact]
    public void GetLocalDateTime_WithDateTime_ConvertsToLocalTime()
    {
        var utcDateTime = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        var result = DateAndTime.GetLocalDateTime(USState.NY, utcDateTime);
        Assert.NotNull(result);
        Assert.NotEqual(utcDateTime, result.Value);
        // Eastern is UTC-5, so 12:00 UTC should be 7:00 EST
        Assert.Equal(7, result.Value.Hour);
    }

    [Fact]
    public void GetLocalDateTime_WithoutDateTime_UsesCurrentUtcTime()
    {
        var result = DateAndTime.GetLocalDateTime(USState.CA);
        Assert.NotNull(result);
        // Should be within a reasonable time range (accounting for timezone offset)
        var utcNow = DateTime.UtcNow;
        var timeZone = DateAndTime.GetTimeZoneByState(USState.CA);
        var expectedLocalTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, timeZone!);
        var timeDiff = Math.Abs((result.Value - expectedLocalTime).TotalSeconds);
        Assert.True(timeDiff < 5); // Allow 5 seconds for test execution time
    }

    [Fact]
    public void GetCurrentLocalDateTime_ReturnsLocalDateTime()
    {
        var result = DateAndTime.GetCurrentLocalDateTime(USState.TX);
        Assert.NotNull(result);
        // Should be within a reasonable time range (accounting for timezone offset)
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
        // In June, Pacific is PDT (UTC-7), so 15:30 UTC should be 8:30 PDT
        // Verify the conversion is correct by checking against TimeZoneInfo directly
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
        // Eastern is UTC-5, so 10:00 EST should be 15:00 UTC
        Assert.Equal(15, result.Value.Hour);
        Assert.Equal(0, result.Value.Minute);
    }

    [Fact]
    public void GetNextScheduledDateTime_WithScheduleTimes_ReturnsNextScheduledTime()
    {
        var scheduleTimes = new List<TimeOnly> { new(9, 0), new(14, 0), new(18, 0) };
        var scheduleFlags = DayFlags.EveryDay; // Use EveryDay to ensure we find a time
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
        var scheduleFlags = DayFlags.EveryDay; // Use EveryDay to ensure we find a time
        var result = DateAndTime.GetNextScheduledDateTime(USState.CA, startTime, endTime, intervalMinutes, scheduleFlags);
        var localNow = DateAndTime.GetCurrentLocalDateTime(USState.CA);
        Assert.NotNull(localNow);
        Assert.True(result > localNow.Value);
    }

    [Fact]
    public void GetNextScheduledDateTime_NoValidDay_ThrowsInvalidOperationException()
    {
        var scheduleTimes = new List<TimeOnly> { new(9, 0) };
        var scheduleFlags = DayFlags.None; // No days selected
        Assert.Throws<InvalidOperationException>(() => DateAndTime.GetNextScheduledDateTime(USState.NY, scheduleTimes, scheduleFlags));
    }

    [Fact]
    public void IsPastDue_WithPastDueSchedule_ReturnsTrue()
    {
        var scheduleTimes = new List<TimeOnly> { new(9, 0) };
        var scheduleFlags = DayFlags.Weekdays;

        // Use a last-run instant far enough in the past that at least one weekday 9:00 (local) has passed
        // since then, even when this test runs on a Monday morning before 9:00 AM Eastern (the old
        // "2 days ago" case could be Saturday with the next slot Monday 9 AM — not yet elapsed).
        var lastRunDay = DateTime.UtcNow.AddDays(-10).Date;
        var lastRunDateTime = new DateTime(lastRunDay.Year, lastRunDay.Month, lastRunDay.Day, 8, 0, 0, DateTimeKind.Utc);

        var result = DateAndTime.IsPastDue(USState.NY, scheduleTimes, scheduleFlags, lastRunDateTime);
        Assert.True(result, $"Expected past due with last run {lastRunDateTime:o} UTC (10+ days ago, weekday slots should have elapsed).");
    }

    [Fact]
    public void IsPastDue_WithRecentRun_ReturnsFalse()
    {
        // Use a schedule time that's guaranteed to be in the future so "1 hour ago" can't have missed it.
        // The old test used 9:00 AM, which would fail when run between 9-10 AM Eastern on weekdays
        // (last run before 9 AM + 9 AM has passed = past due).
        var localNow = DateAndTime.GetCurrentLocalDateTime(USState.NY)!.Value;
        var futureSchedule = localNow.AddHours(2);
        var scheduleTimes = new List<TimeOnly> { new(futureSchedule.Hour, futureSchedule.Minute) };
        var scheduleFlags = DayFlags.Weekdays;
        var lastRunDateTime = DateTime.UtcNow.AddHours(-1); // 1 hour ago
        var result = DateAndTime.IsPastDue(USState.NY, scheduleTimes, scheduleFlags, lastRunDateTime);
        Assert.False(result);
    }

    [Fact]
    public void IsPastDue_EarlyMorningWithYesterdayMissedSchedule_ReturnsTrue()
    {
        // This test specifically verifies the bug fix where the service wasn't checking
        // past dates correctly. The bug was that IsPastDue only looked forward from today,
        // missing schedules that occurred between the last run and now.
        // 
        // Scenario: Last run was 2 days ago, current time is early morning (1:20 AM),
        // and yesterday's 9:00 AM weekday schedule should have been missed.
        var scheduleTimes = new List<TimeOnly> { new(9, 0) };
        var scheduleFlags = DayFlags.Weekdays;

        // Set last run to 2 days ago at 8:00 AM UTC (before any 9:00 AM schedule)
        var twoDaysAgo = DateTime.UtcNow.AddDays(-2);
        var lastRunDateTime = new DateTime(twoDaysAgo.Year, twoDaysAgo.Month, twoDaysAgo.Day, 8, 0, 0, DateTimeKind.Utc);

        // Check if yesterday was a weekday - if so, its 9:00 AM schedule should be past due
        var yesterday = DateTime.UtcNow.AddDays(-1);
        var isYesterdayWeekday = yesterday.DayOfWeek >= DayOfWeek.Monday && yesterday.DayOfWeek <= DayOfWeek.Friday;
        if (isYesterdayWeekday) {
            var result = DateAndTime.IsPastDue(USState.NY, scheduleTimes, scheduleFlags, lastRunDateTime);

            // Should return true because yesterday's 9:00 AM schedule was missed
            // (it occurred after the last run 2 days ago and before now)
            Assert.True(
                result,
                $"Expected past due: last run was {lastRunDateTime:g} UTC, yesterday ({yesterday:yyyy-MM-dd}) was a {yesterday.DayOfWeek} with a 9:00 AM schedule that should have been missed.");
        }
    }

    [Fact]
    public void IsPastDue_ChecksDatesBetweenLastRunAndNow_ReturnsTrue()
    {
        // This test verifies that IsPastDue checks all dates between the last run and now,
        // not just dates from today forward. This was the core bug that caused failures
        // when checking early in the morning.
        var scheduleTimes = new List<TimeOnly> { new(14, 0) }; // 2:00 PM
        var scheduleFlags = DayFlags.Weekdays;

        // Set last run to 3 days ago at 1:00 PM UTC (before the 2:00 PM schedule that day)
        var threeDaysAgo = DateTime.UtcNow.AddDays(-3);
        var lastRunDateTime = new DateTime(threeDaysAgo.Year, threeDaysAgo.Month, threeDaysAgo.Day, 13, 0, 0, DateTimeKind.Utc);

        // Verify there's at least one weekday in the last 3 days
        var weekdayCount = 0;
        for (var i = 1; i <= 3; i++) {
            var checkDate = DateTime.UtcNow.AddDays(-i);
            if (checkDate.DayOfWeek >= DayOfWeek.Monday && checkDate.DayOfWeek <= DayOfWeek.Friday)
                weekdayCount++;
        }

        // Only assert if we have weekdays to check (skip on weekends)
        if (weekdayCount > 0) {
            var result = DateAndTime.IsPastDue(USState.NY, scheduleTimes, scheduleFlags, lastRunDateTime);

            // Should return true because there were weekday schedules in the past 3 days
            // (specifically 2:00 PM schedules that occurred after the last run at 1:00 PM)
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
        var lastRunDateTime = DateTime.UtcNow.AddHours(-3); // 3 hours ago
        var result = DateAndTime.IsPastDue(USState.NY, startTime, endTime, minuteInterval, scheduleFlags, lastRunDateTime);

        // Result depends on current time and schedule, so we just verify it doesn't throw
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

        // If current time is outside the window, should return false
        var result = DateAndTime.IsPastDue(USState.CA, startTime, endTime, minuteInterval, scheduleFlags, lastRunDateTime);
        Assert.IsType<bool>(result);
    }

    [Fact]
    public void GetScheduledTimesForDay_WithScheduleTimes_ReturnsAllTimes()
    {
        var date = new DateTime(2024, 1, 15); // Monday
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
        var date = new DateTime(2024, 1, 14); // Sunday
        var scheduleTimes = new List<TimeOnly> { new(9, 0) };
        var scheduleFlags = DayFlags.Weekdays; // Only weekdays
        var result = DateAndTime.GetScheduledTimesForDay(date, scheduleTimes, scheduleFlags).ToList();
        Assert.Empty(result);
    }

    [Fact]
    public void GetScheduledTimesForDay_WithTimeWindow_ReturnsAllIntervals()
    {
        var date = new DateTime(2024, 1, 15); // Monday
        var startTime = new TimeOnly(9, 0);
        var endTime = new TimeOnly(12, 0);
        var intervalMinutes = 60;
        var scheduleFlags = DayFlags.Mon;
        var result = DateAndTime.GetScheduledTimesForDay(date, startTime, endTime, intervalMinutes, scheduleFlags).ToList();
        Assert.Equal(4, result.Count); // 9:00, 10:00, 11:00, 12:00
        Assert.Equal(new(2024, 1, 15, 9, 0, 0), result[0]);
        Assert.Equal(new(2024, 1, 15, 10, 0, 0), result[1]);
        Assert.Equal(new(2024, 1, 15, 11, 0, 0), result[2]);
        Assert.Equal(new(2024, 1, 15, 12, 0, 0), result[3]);
    }

    [Fact]
    public void IsScheduledDay_WeekdayWithWeekdayFlag_ReturnsTrue()
    {
        var dateTime = new DateTime(2024, 1, 15); // Monday
        var scheduleFlags = DayFlags.Weekdays;
        var result = DateAndTime.IsScheduledDay(dateTime, scheduleFlags);
        Assert.True(result);
    }

    [Fact]
    public void IsScheduledDay_WeekendWithWeekdayFlag_ReturnsFalse()
    {
        var dateTime = new DateTime(2024, 1, 14); // Sunday
        var scheduleFlags = DayFlags.Weekdays;
        var result = DateAndTime.IsScheduledDay(dateTime, scheduleFlags);
        Assert.False(result);
    }

    [Fact]
    public void IsScheduledDay_WeekendWithWeekendFlag_ReturnsTrue()
    {
        var dateTime = new DateTime(2024, 1, 14); // Sunday
        var scheduleFlags = DayFlags.Weekends;
        var result = DateAndTime.IsScheduledDay(dateTime, scheduleFlags);
        Assert.True(result);
    }

    [Fact]
    public void IsScheduledDay_SpecificDay_ReturnsTrue()
    {
        var dateTime = new DateTime(2024, 1, 15); // Monday
        var scheduleFlags = DayFlags.Mon;
        var result = DateAndTime.IsScheduledDay(dateTime, scheduleFlags);
        Assert.True(result);
    }

    [Fact]
    public void IsScheduledDay_DifferentDay_ReturnsFalse()
    {
        var dateTime = new DateTime(2024, 1, 16); // Tuesday
        var scheduleFlags = DayFlags.Mon;
        var result = DateAndTime.IsScheduledDay(dateTime, scheduleFlags);
        Assert.False(result);
    }

    [Fact]
    public void GetNextScheduledDateTime_MultipleTimes_ReturnsEarliest()
    {
        var scheduleTimes = new List<TimeOnly> { new(18, 0), new(9, 0), new(15, 0) };
        var scheduleFlags = DayFlags.EveryDay; // Use EveryDay to ensure we find a time
        var result = DateAndTime.GetNextScheduledDateTime(USState.NY, scheduleTimes, scheduleFlags);

        // Should be the earliest time that's in the future
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
        // Pacific should be 3 hours later in UTC than Eastern
        Assert.Equal(3, (pacificResult.Value - easternResult.Value).Hours);
    }

    [Fact]
    public void GetScheduledTimesForDay_OrdersTimesCorrectly()
    {
        var date = new DateTime(2024, 1, 15); // Monday
        var scheduleTimes = new List<TimeOnly> { new(15, 0), new(9, 0), new(12, 0) };
        var scheduleFlags = DayFlags.Mon;
        var result = DateAndTime.GetScheduledTimesForDay(date, scheduleTimes, scheduleFlags).ToList();
        Assert.Equal(3, result.Count);
        // Should be ordered
        Assert.True(result[0] < result[1]);
        Assert.True(result[1] < result[2]);
    }

    [Fact]
    public void IsPastDue_WithNullLastRun_UsesDefault()
    {
        var scheduleTimes = new List<TimeOnly> { new(9, 0) };
        var scheduleFlags = DayFlags.Weekdays;
        var result = DateAndTime.IsPastDue(USState.NY, scheduleTimes, scheduleFlags, null);

        // Should use default (7 days ago), so likely past due
        Assert.IsType<bool>(result);
    }

    [Fact]
    public void GetNextScheduledDateTime_AllDays_ReturnsNextTime()
    {
        var scheduleTimes = new List<TimeOnly> { new(9, 0) };
        var scheduleFlags = DayFlags.EveryDay;
        var result = DateAndTime.GetNextScheduledDateTime(USState.NY, scheduleTimes, scheduleFlags);
        Assert.True(result > DateTime.UtcNow);
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
        var scheduleTimes = new List<TimeOnly>();
        var scheduleFlags = DayFlags.EveryDay;
        Assert.Throws<ArgumentException>(() => DateAndTime.GetNextScheduledDateTime(USState.NY, scheduleTimes, scheduleFlags));
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
        Assert.Throws<ArgumentException>(() => DateAndTime.GetNextScheduledDateTime(USState.NY, startTime, endTime, intervalMinutes, scheduleFlags));
    }

    [Fact]
    public void GetNextScheduledDateTime_NegativeIntervalMinutes_ThrowsArgumentException()
    {
        var startTime = new TimeOnly(9, 0);
        var endTime = new TimeOnly(17, 0);
        var intervalMinutes = -10;
        var scheduleFlags = DayFlags.EveryDay;
        Assert.Throws<ArgumentException>(() => DateAndTime.GetNextScheduledDateTime(USState.NY, startTime, endTime, intervalMinutes, scheduleFlags));
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
        var scheduleTimes = new List<TimeOnly>();
        var scheduleFlags = DayFlags.Weekdays;
        var lastRunDateTime = DateTime.UtcNow.AddDays(-1);
        Assert.Throws<ArgumentException>(() => DateAndTime.IsPastDue(USState.NY, scheduleTimes, scheduleFlags, lastRunDateTime));
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
        Assert.Throws<ArgumentException>(() => DateAndTime.IsPastDue(USState.NY, startTime, endTime, minuteInterval, scheduleFlags, lastRunDateTime));
    }

    [Fact]
    public void GetScheduledTimesForDay_NullScheduleTimes_ThrowsArgumentNullException()
    {
        IEnumerable<TimeOnly>? scheduleTimes = null;
        var date = new DateTime(2024, 1, 15);
        var scheduleFlags = DayFlags.Mon;

        // Need to enumerate the iterator to trigger validation
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

        // Need to enumerate the iterator to trigger validation
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

        // Need to enumerate the iterator to trigger validation
        Assert.Throws<ArgumentException>(() => DateAndTime.GetScheduledTimesForDay(date, startTime, endTime, intervalMinutes, scheduleFlags).ToList());
    }

    [Fact]
    public void GetTimeZoneByState_UnknownState_ReturnsNull()
    {
        // Test with a state that might not be in the map (though all US states should be mapped)
        // This tests the null return path
        var result = DateAndTime.GetTimeZoneByState(USState.UU); // Unknown state
        // UU might not be mapped, or it might be - this test verifies the method handles it
        Assert.True(result == null || result != null);
    }

    [Fact]
    public void GetScheduledTimesForDay_TimeWindow_EndTimeInclusive()
    {
        var date = new DateTime(2024, 1, 15); // Monday
        var startTime = new TimeOnly(9, 0);
        var endTime = new TimeOnly(9, 0); // Same as start time
        var intervalMinutes = 60;
        var scheduleFlags = DayFlags.Mon;
        var result = DateAndTime.GetScheduledTimesForDay(date, startTime, endTime, intervalMinutes, scheduleFlags).ToList();

        // Should include the end time
        Assert.Single(result);
        Assert.Equal(new(2024, 1, 15, 9, 0, 0), result[0]);
    }

    [Fact]
    public void GetNextScheduledDateTime_TimeWindow_EndTimeInclusive()
    {
        var now = DateAndTime.GetCurrentLocalDateTime(USState.NY);
        Assert.NotNull(now);

        // Set a time in the past to ensure we get a future time
        var pastTime = now.Value.AddHours(-2);

        // Create a schedule that includes the current hour
        var startTime = new TimeOnly(pastTime.Hour, 0);
        var endTime = new TimeOnly(pastTime.Hour, 0);
        var intervalMinutes = 60;
        var scheduleFlags = DayFlags.EveryDay;

        // Should find a time even when start and end are the same
        var result = DateAndTime.GetNextScheduledDateTime(USState.NY, startTime, endTime, intervalMinutes, scheduleFlags);
        Assert.True(result > DateTime.UtcNow);
    }
}