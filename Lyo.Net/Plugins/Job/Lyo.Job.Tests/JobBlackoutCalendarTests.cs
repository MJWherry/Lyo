using Lyo.Common.Core.Enums;
using Lyo.Common.Core.Identifiers;
using Lyo.DateAndTime;
using Lyo.Job.Models;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Response;
using Lyo.Schedule.Models;

namespace Lyo.Job.Tests;

public class JobBlackoutCalendarTests
{
    [Fact]
    public void ToCalendar_WeekdayWindow_PlacesMatchingDays()
    {
        var placed = JobBlackoutCalendar.ToCalendar(WeekdayCalendar(), 2026, 9, TimeZoneInfo.Utc);
        var tuesday = placed.Days.Single(day => day.Date == new DateOnlyModel(2026, 9, 1));
        Assert.Contains(
            tuesday.Entries,
            entry => entry.Title == "Office" && entry.Start == new DateTime(2026, 9, 1, 9, 0, 0, DateTimeKind.Utc) &&
                entry.End == new DateTime(2026, 9, 1, 17, 0, 0, DateTimeKind.Utc));

        Assert.Empty(placed.Days.Single(day => day.Date == new DateOnlyModel(2026, 9, 5)).Entries);
        Assert.Empty(placed.Days.Single(day => day.Date == new DateOnlyModel(2026, 9, 6)).Entries);
        Assert.Contains(placed.Days.Single(day => day.Date == new DateOnlyModel(2026, 9, 7)).Entries, entry => entry.Title == "Office");
    }

    [Fact]
    public void ToCalendar_DatedRange_ClipsToBounds()
    {
        var placed = JobBlackoutCalendar.ToCalendar(DatedCalendar(), 2026, 9, TimeZoneInfo.Utc);
        Assert.Contains(placed.Days.Single(day => day.Date == new DateOnlyModel(2026, 9, 10)).Entries, entry => entry.Title == "Freeze");
        Assert.Contains(placed.Days.Single(day => day.Date == new DateOnlyModel(2026, 9, 11)).Entries, entry => entry.Title == "Freeze");
        Assert.Contains(placed.Days.Single(day => day.Date == new DateOnlyModel(2026, 9, 12)).Entries, entry => entry.Title == "Freeze");
        Assert.Empty(placed.Days.Single(day => day.Date == new DateOnlyModel(2026, 9, 9)).Entries);
        Assert.Empty(placed.Days.Single(day => day.Date == new DateOnlyModel(2026, 9, 13)).Entries);
    }

    [Fact]
    public void ToCalendar_NewYearsDay_PlacesJanuary1()
    {
        var placed = JobBlackoutCalendar.ToCalendar(NewYearCalendar(), 2026, 1, TimeZoneInfo.Utc);
        var newYear = placed.Days.Single(day => day.Date == new DateOnlyModel(2026, 1, 1));
        Assert.Contains(
            newYear.Entries,
            entry => entry.Title == "New Year" && entry.Start == new DateTime(2026, 1, 1, 9, 0, 0, DateTimeKind.Utc) &&
                entry.End == new DateTime(2026, 1, 1, 17, 0, 0, DateTimeKind.Utc));

        Assert.Empty(placed.Days.Single(day => day.Date == new DateOnlyModel(2026, 1, 2)).Entries);
    }

    [Fact]
    public void Suppress_SkipDatedDay_MutesFiresInsideTheWindow()
    {
        var calendar = Calendar.FromTimeFrames(
            new(2026, 9, 1), new(2026, 9, 30), TimeZoneInfo.Utc,
            [Frame("labor", "Klein", new(2026, 9, 7, 12, 0, 0, DateTimeKind.Utc)), Frame("next", "Klein", new(2026, 9, 8, 12, 0, 0, DateTimeKind.Utc))]);

        var suppressed = JobBlackoutCalendar.Suppress(calendar, FullDay(new(2026, 9, 7)), TimeZoneInfo.Utc);
        var labor = Assert.Single(suppressed.Entries, entry => entry.Key == "labor");
        Assert.True(labor.Muted);
        var next = Assert.Single(suppressed.Entries, entry => entry.Key == "next");
        Assert.False(next.Muted);
    }

    [Fact]
    public void Suppress_Defer_MovesTheFireToTheWindowEnd()
    {
        var calendar = Calendar.FromTimeFrames(new(2026, 9, 7), new(2026, 9, 7), TimeZoneInfo.Utc, [Frame("slot", "Klein", new(2026, 9, 7, 10, 0, 0, DateTimeKind.Utc))]);
        var calendarId = LyoGuid.CreateV7();
        var window = Window(calendarId, "Hold", DayFlags.EveryDay, "09:00", "12:00") with {
            Policy = JobBlackoutPolicy.Defer, StartDateUtc = new DateTime(2026, 9, 7, 0, 0, 0, DateTimeKind.Utc), EndDateUtc = new DateTime(2026, 9, 7, 0, 0, 0, DateTimeKind.Utc)
        };

        var suppressed = JobBlackoutCalendar.Suppress(calendar, new(calendarId, "Hold", null, true, [window]), TimeZoneInfo.Utc);
        var fire = Assert.Single(suppressed.Entries);
        Assert.Equal(new(2026, 9, 7, 12, 0, 0, DateTimeKind.Utc), fire.Start);
    }

    private static TimeFrame Frame(string key, string title, DateTime at) => new(at, at, key, title);

    private static JobBlackoutCalendarRes FullDay(DateTime day)
    {
        var calendarId = LyoGuid.CreateV7();
        var window = Window(calendarId, "Labor Day (2026)", DayFlags.EveryDay, "00:00", "23:59") with { StartDateUtc = day, EndDateUtc = day };
        return new(calendarId, "Holidays", null, true, [window]);
    }

    private static JobBlackoutCalendarRes WeekdayCalendar()
    {
        var calendarId = LyoGuid.CreateV7();
        return new(calendarId, "Hours", null, true, [Window(calendarId, "Office", DayFlags.Weekdays, "09:00", "17:00")]);
    }

    private static JobBlackoutCalendarRes DatedCalendar()
    {
        var calendarId = LyoGuid.CreateV7();
        var window = Window(calendarId, "Freeze", DayFlags.EveryDay, "09:00", "12:00") with {
            StartDateUtc = new DateTime(2026, 9, 10, 0, 0, 0, DateTimeKind.Utc), EndDateUtc = new DateTime(2026, 9, 12, 0, 0, 0, DateTimeKind.Utc)
        };

        return new(calendarId, "Freeze", null, true, [window]);
    }

    private static JobBlackoutCalendarRes NewYearCalendar()
    {
        var calendarId = LyoGuid.CreateV7();
        var window = Window(calendarId, "New Year", DayFlags.None, "09:00", "17:00") with { HolidaySlug = "new-years-day" };
        return new(calendarId, "Holidays", null, true, [window]);
    }

    private static JobBlackoutWindowRes Window(Guid calendarId, string name, DayFlags days, string start, string end)
        => new(LyoGuid.CreateV7(), calendarId, name, days, TimeOnly.Parse(start), TimeOnly.Parse(end), JobBlackoutPolicy.Skip, true);
}