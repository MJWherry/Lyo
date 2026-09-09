using Lyo.Common.Core.Enums;
using Lyo.DateAndTime;
using Lyo.Job.Models;
using Lyo.Job.Models.Builders;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Response;

namespace Lyo.Job.Scheduler.Tests;

public class JobBlackoutCalendarEvaluatorTests
{
    private static readonly TimeZoneInfo Utc = TimeZoneInfo.Utc;

    [Fact]
    public void AdjustSlotForBlackout_WhenNoCalendar_ReturnsOriginalSlot()
    {
        var slot = new DateTime(2026, 7, 7, 10, 0, 0, DateTimeKind.Utc);
        var result = JobBlackoutCalendarEvaluator.AdjustSlotForBlackout(slot, null, Utc);
        Assert.Equal(slot, result);
    }

    [Fact]
    public void AdjustSlotForBlackout_WhenSlotOutsideWindow_ReturnsOriginalSlot()
    {
        var slot = new DateTime(2026, 7, 7, 14, 0, 0, DateTimeKind.Utc);
        var calendar = CreateCalendar(JobBlackoutPolicy.Skip, "09:00", "12:00");
        var result = JobBlackoutCalendarEvaluator.AdjustSlotForBlackout(slot, calendar, Utc);
        Assert.Equal(slot, result);
    }

    [Fact]
    public void AdjustSlotForBlackout_WhenInsideWindowWithSkipPolicy_ReturnsNull()
    {
        var slot = new DateTime(2026, 7, 7, 10, 30, 0, DateTimeKind.Utc);
        var calendar = CreateCalendar(JobBlackoutPolicy.Skip, "09:00", "12:00");
        var result = JobBlackoutCalendarEvaluator.AdjustSlotForBlackout(slot, calendar, Utc);
        Assert.Null(result);
    }

    [Fact]
    public void AdjustSlotForBlackout_WhenInsideWindowWithDeferPolicy_ReturnsWindowEndUtc()
    {
        var slot = new DateTime(2026, 7, 7, 10, 30, 0, DateTimeKind.Utc);
        var calendar = CreateCalendar(JobBlackoutPolicy.Defer, "09:00", "12:00");
        var result = JobBlackoutCalendarEvaluator.AdjustSlotForBlackout(slot, calendar, Utc);
        Assert.NotNull(result);
        Assert.Equal(new DateTime(2026, 7, 7, 12, 0, 0, DateTimeKind.Utc), result);
    }

    [Fact]
    public void AdjustSlotForBlackout_WhenWindowSpansMidnight_DeferUsesNextDayEnd()
    {
        var slot = new DateTime(2026, 7, 7, 23, 30, 0, DateTimeKind.Utc);
        var calendar = CreateCalendar(JobBlackoutPolicy.Defer, "22:00", "06:00");
        var result = JobBlackoutCalendarEvaluator.AdjustSlotForBlackout(slot, calendar, Utc);
        Assert.NotNull(result);
        Assert.Equal(new DateTime(2026, 7, 8, 6, 0, 0, DateTimeKind.Utc), result);
    }

    [Fact]
    public void AdjustSlotForBlackout_WhenDatedWindowMatches_SkipsSlot()
    {
        var slot = new DateTime(2026, 12, 25, 10, 0, 0, DateTimeKind.Utc);
        var calendar = CreateDatedCalendar(JobBlackoutPolicy.Skip, new(2026, 12, 25, 0, 0, 0, DateTimeKind.Utc));
        var result = JobBlackoutCalendarEvaluator.AdjustSlotForBlackout(slot, calendar, Utc);
        Assert.Null(result);
    }

    [Fact]
    public void AdjustSlotForBlackout_WhenDatedWindowDoesNotMatch_ReturnsOriginalSlot()
    {
        var slot = new DateTime(2026, 7, 7, 10, 0, 0, DateTimeKind.Utc);
        var calendar = CreateDatedCalendar(JobBlackoutPolicy.Skip, new(2026, 12, 25, 0, 0, 0, DateTimeKind.Utc));
        var result = JobBlackoutCalendarEvaluator.AdjustSlotForBlackout(slot, calendar, Utc);
        Assert.Equal(slot, result);
    }

    [Fact]
    public void AdjustSlotForBlackout_WhenObservedHolidayDateMatches_SkipsSlot()
    {
        // July 4 2026 is a Saturday; observed Friday July 3.
        var slot = new DateTime(2026, 7, 3, 10, 0, 0, DateTimeKind.Utc);
        var calendar = CreateDatedCalendar(JobBlackoutPolicy.Skip, new(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc));
        var result = JobBlackoutCalendarEvaluator.AdjustSlotForBlackout(slot, calendar, Utc);
        Assert.Null(result);
    }

    [Fact]
    public void AdjustSlotForBlackout_WhenHolidaySlugMatchesCalendarDate_SkipsSlot()
    {
        var slot = new DateTime(2026, 12, 25, 10, 0, 0, DateTimeKind.Utc);
        var calendar = CreateHolidayCalendar(HolidayInfo.ChristmasDay);
        var result = JobBlackoutCalendarEvaluator.AdjustSlotForBlackout(slot, calendar, Utc);
        Assert.Null(result);
    }

    [Fact]
    public void AdjustSlotForBlackout_WhenHolidaySlugDoesNotMatch_ReturnsOriginalSlot()
    {
        var slot = new DateTime(2026, 12, 24, 10, 0, 0, DateTimeKind.Utc);
        var calendar = CreateHolidayCalendar(HolidayInfo.ChristmasDay);
        var result = JobBlackoutCalendarEvaluator.AdjustSlotForBlackout(slot, calendar, Utc);
        Assert.Equal(slot, result);
    }

    [Fact]
    public void AdjustSlotForBlackout_WhenIndependenceDayFallsOnWeekend_CalendarDateSkipsJuly4()
    {
        var slot = new DateTime(2026, 7, 4, 10, 0, 0, DateTimeKind.Utc);
        var calendar = CreateHolidayCalendar(HolidayInfo.IndependenceDay);
        var result = JobBlackoutCalendarEvaluator.AdjustSlotForBlackout(slot, calendar, Utc);
        Assert.Null(result);
    }

    [Fact]
    public void AdjustSlotForBlackout_WhenIndependenceDayObservedOff_DoesNotSkipJuly3()
    {
        var slot = new DateTime(2026, 7, 3, 10, 0, 0, DateTimeKind.Utc);
        var calendar = CreateHolidayCalendar(HolidayInfo.IndependenceDay);
        var result = JobBlackoutCalendarEvaluator.AdjustSlotForBlackout(slot, calendar, Utc);
        Assert.Equal(slot, result);
    }

    [Fact]
    public void AdjustSlotForBlackout_WhenIndependenceDayObservedOn_SkipsJuly3()
    {
        var slot = new DateTime(2026, 7, 3, 10, 0, 0, DateTimeKind.Utc);
        var calendar = CreateHolidayCalendar(HolidayInfo.IndependenceDay, includeObservedDate: true);
        var result = JobBlackoutCalendarEvaluator.AdjustSlotForBlackout(slot, calendar, Utc);
        Assert.Null(result);
    }

    [Fact]
    public void AdjustSlotForBlackout_WhenHolidaySlugIsUnknown_ReturnsOriginalSlot()
    {
        var slot = new DateTime(2026, 12, 25, 10, 0, 0, DateTimeKind.Utc);
        var calendarId = Guid.NewGuid();
        var calendar = new JobBlackoutCalendarRes(
            calendarId, "Bad", null, true,
            [new(Guid.NewGuid(), calendarId, "Unknown", DayFlags.None, TimeOnly.Parse("00:00"), TimeOnly.Parse("23:59"), JobBlackoutPolicy.Skip, true, HolidaySlug: "not-a-holiday")]);
        var result = JobBlackoutCalendarEvaluator.AdjustSlotForBlackout(slot, calendar, Utc);
        Assert.Equal(slot, result);
    }

    [Fact]
    public void AdjustSlotForBlackout_WhenCalendarDaysRepeat_SkipsMatchingDayEachYear()
    {
        var calendar = CreateCalendarDaysCalendar(MonthFlags.Dec, [25]);
        var result2026 = JobBlackoutCalendarEvaluator.AdjustSlotForBlackout(new(2026, 12, 25, 10, 0, 0, DateTimeKind.Utc), calendar, Utc);
        var result2027 = JobBlackoutCalendarEvaluator.AdjustSlotForBlackout(new(2027, 12, 25, 10, 0, 0, DateTimeKind.Utc), calendar, Utc);
        var resultOther = JobBlackoutCalendarEvaluator.AdjustSlotForBlackout(new(2026, 12, 24, 10, 0, 0, DateTimeKind.Utc), calendar, Utc);
        Assert.Null(result2026);
        Assert.Null(result2027);
        Assert.Equal(new DateTime(2026, 12, 24, 10, 0, 0, DateTimeKind.Utc), resultOther);
    }

    [Fact]
    public void AdjustSlotForBlackout_WhenCalendarDaysAreBounded_DoesNotSkipOutsideRange()
    {
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc);
        var calendar = CreateCalendarDaysCalendar(MonthFlags.Dec, [25], start, end);
        var inRange = JobBlackoutCalendarEvaluator.AdjustSlotForBlackout(new(2026, 12, 25, 10, 0, 0, DateTimeKind.Utc), calendar, Utc);
        var outOfRange = JobBlackoutCalendarEvaluator.AdjustSlotForBlackout(new(2027, 12, 25, 10, 0, 0, DateTimeKind.Utc), calendar, Utc);
        Assert.Null(inRange);
        Assert.Equal(new DateTime(2027, 12, 25, 10, 0, 0, DateTimeKind.Utc), outOfRange);
    }

    [Fact]
    public void AdjustSlotForBlackout_WhenFebruary31Window_DoesNotMatchFebruary28()
    {
        var slot = new DateTime(2026, 2, 28, 10, 0, 0, DateTimeKind.Utc);
        var calendar = CreateCalendarDaysCalendar(MonthFlags.Feb, [31]);
        var result = JobBlackoutCalendarEvaluator.AdjustSlotForBlackout(slot, calendar, Utc);
        Assert.Equal(slot, result);
    }

    private static JobBlackoutCalendarRes CreateHolidayCalendar(HolidayInfo holiday, bool includeObservedDate = false)
    {
        var calendarId = Guid.NewGuid();
        return new(
            calendarId, "Holiday calendar", null, true,
            [
                new(
                    Guid.NewGuid(), calendarId, holiday.Name, DayFlags.None, TimeOnly.Parse("00:00"), TimeOnly.Parse("23:59"), JobBlackoutPolicy.Skip, true, HolidaySlug: holiday.Slug,
                    IncludeObservedDate: includeObservedDate)
            ]);
    }

    private static JobBlackoutCalendarRes CreateCalendarDaysCalendar(MonthFlags months, IReadOnlyList<int> days, DateTime? start = null, DateTime? end = null)
    {
        var calendarId = Guid.NewGuid();
        return new(
            calendarId, "Calendar days", null, true,
            [
                new(
                    Guid.NewGuid(), calendarId, "Days", DayFlags.None, TimeOnly.Parse("00:00"), TimeOnly.Parse("23:59"), JobBlackoutPolicy.Skip, true, start, end, MonthFlags: months,
                    DaysOfMonth: days)
            ]);
    }

    private static JobBlackoutCalendarRes CreateDatedCalendar(JobBlackoutPolicy policy, DateTime dateUtc)
    {
        var calendarId = Guid.NewGuid();
        return new(
            calendarId, "Dated calendar", null, true,
            [new(Guid.NewGuid(), calendarId, "Holiday", DayFlags.EveryDay, TimeOnly.Parse("00:00"), TimeOnly.Parse("23:59"), policy, true, dateUtc, dateUtc)]);
    }

    private static JobBlackoutCalendarRes CreateCalendar(JobBlackoutPolicy policy, string start, string end)
    {
        var calendarId = Guid.NewGuid();
        return new(
            calendarId, "Test calendar", null, true, [new(Guid.NewGuid(), calendarId, "Blackout", DayFlags.EveryDay, TimeOnly.Parse(start), TimeOnly.Parse(end), policy, true)]);
    }
}
