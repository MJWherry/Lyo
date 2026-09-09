using Lyo.Common.Core.Enums;
using Lyo.DateAndTime;
using Lyo.Job.Models.Builders;
using Lyo.Job.Models.Enums;

namespace Lyo.Job.Scheduler.Tests;

public class JobBlackoutCalendarBuilderTests
{
    [Fact]
    public void AddBlackoutHoliday_StoresOneWindowWithSlug()
    {
        var calendar = JobBlackoutCalendarBuilder.New("Federal").AddBlackoutHoliday(HolidayInfo.ChristmasDay).Build();
        var window = Assert.Single(calendar.CreateBlackoutWindows);
        Assert.Equal(HolidayInfo.ChristmasDay.Slug, window.HolidaySlug);
        Assert.Equal("Christmas Day", window.Name);
        Assert.Equal(DayFlags.None, window.DayFlags);
        Assert.Null(window.StartDateUtc);
        Assert.False(window.IncludeObservedDate);
        Assert.Equal(TimeOnly.Parse("00:00"), window.StartTime);
        Assert.Equal(TimeOnly.Parse("23:59"), window.EndTime);
        Assert.Equal(JobBlackoutPolicy.Skip, window.Policy);
    }

    [Fact]
    public void AddFederalHolidayBlackouts_AddsOneWindowPerFederalHoliday()
    {
        var calendar = JobBlackoutCalendarBuilder.New("Federal").AddFederalHolidayBlackouts().Build();
        Assert.Equal(HolidayInfo.FederalHolidays.Count, calendar.CreateBlackoutWindows.Count);
        Assert.Contains(calendar.CreateBlackoutWindows, w => w.HolidaySlug == HolidayInfo.ChristmasDay.Slug);
        Assert.All(calendar.CreateBlackoutWindows, w => Assert.False(string.IsNullOrWhiteSpace(w.HolidaySlug)));
        Assert.All(calendar.CreateBlackoutWindows, w => Assert.Null(w.StartDateUtc));
    }

    [Fact]
    public void AddBlackoutHolidays_AcceptsMultipleHolidayRecords()
    {
        var calendar = JobBlackoutCalendarBuilder.New("Selected").AddBlackoutHolidays([HolidayInfo.ChristmasDay, HolidayInfo.ThanksgivingDay]).Build();
        Assert.Equal(2, calendar.CreateBlackoutWindows.Count);
        Assert.Contains(calendar.CreateBlackoutWindows, w => w.HolidaySlug == HolidayInfo.ChristmasDay.Slug);
        Assert.Contains(calendar.CreateBlackoutWindows, w => w.HolidaySlug == HolidayInfo.ThanksgivingDay.Slug);
    }

    [Fact]
    public void AddBlackoutHoliday_WithUnknownHoliday_Throws()
        => Assert.Throws<ArgumentException>(() => JobBlackoutCalendarBuilder.New("Bad").AddBlackoutHoliday(HolidayInfo.Unknown));

    [Fact]
    public void AddBlackoutCalendarDays_StoresMonthsAndDays()
    {
        var calendar = JobBlackoutCalendarBuilder.New("Inventory").AddBlackoutCalendarDays("Month end", MonthFlags.EveryMonth, [31]).Build();
        var window = Assert.Single(calendar.CreateBlackoutWindows);
        Assert.Equal(MonthFlags.EveryMonth, window.MonthFlags);
        Assert.Equal([31], window.DaysOfMonth);
        Assert.Null(window.HolidaySlug);
        Assert.Equal(DayFlags.None, window.DayFlags);
    }

    [Fact]
    public void AddBlackoutCalendarDays_WithDateBounds_StoresRange()
    {
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc);
        var calendar = JobBlackoutCalendarBuilder.New("Bounded").AddBlackoutCalendarDays("Dec 25", MonthFlags.Dec, [25], startDateUtc: start, endDateUtc: end).Build();
        var window = Assert.Single(calendar.CreateBlackoutWindows);
        Assert.Equal(start, window.StartDateUtc);
        Assert.Equal(end, window.EndDateUtc);
        Assert.Equal([25], window.DaysOfMonth);
    }

    [Fact]
    public void AddBlackoutCalendarDays_WithNoMonths_Throws()
        => Assert.Throws<ArgumentException>(() => JobBlackoutCalendarBuilder.New("Bad").AddBlackoutCalendarDays("X", MonthFlags.None, [1]));
}
