using Lyo.Common.Core.Enums;
using Lyo.DateAndTime;
using Lyo.Schedule.Models;

namespace Lyo.Scheduler.Tests;

public class ScheduleCalendarTests
{
    [Fact]
    public void FromSchedule_SetTimes_PlacesClockTimesOnFlaggedWeekdays()
    {
        var definition = ScheduleDefinition.Create().WithDescription("Standup").WithTimeZone(TimeZoneInfo.Utc).SetTimes("09:00").SetDays(DayFlags.Tue).SetMonths(MonthFlags.EveryMonth).Build();

        var calendar = ScheduleCalendar.From(definition, 2026, 9, TimeZoneInfo.Utc);
        var points = calendar.Entries.Where(e => e.IsPoint).ToArray();

        Assert.Equal(5, points.Length);
        Assert.All(points, entry => {
            Assert.Equal("Standup", entry.Title);
            Assert.Equal(9, entry.Start.Hour);
            Assert.Equal(DayOfWeek.Tuesday, entry.Start.DayOfWeek);
        });
        Assert.Contains(points, entry => entry.Start == new DateTime(2026, 9, 1, 9, 0, 0, DateTimeKind.Utc));
        Assert.Empty(calendar.Days.Single(day => day.Date == new DateOnlyModel(2026, 9, 5)).Entries);
    }

    [Fact]
    public void FromSchedule_Disabled_EmitsNothing()
    {
        var definition = ScheduleDefinition.Create().WithDescription("Standup").WithTimeZone(TimeZoneInfo.Utc).Enabled(false).SetTimes("09:00").SetDays(DayFlags.Tue).SetMonths(MonthFlags.EveryMonth).Build();

        var calendar = ScheduleCalendar.From(definition, 2026, 9, TimeZoneInfo.Utc);

        Assert.Empty(calendar.Entries);
        Assert.Equal(35, calendar.Days.Count);
    }

    [Fact]
    public void FromSchedule_KeyScope_KeepsSameInstantFiresDistinct()
    {
        var definition = ScheduleDefinition.Create().WithDescription("Standup").WithTimeZone(TimeZoneInfo.Utc).SetTimes("09:00").SetDays(DayFlags.Tue).SetMonths(MonthFlags.EveryMonth).Build();

        var first = ScheduleCalendar.From(definition, 2026, 9, TimeZoneInfo.Utc, keyScope: "a");
        var second = ScheduleCalendar.From(definition, 2026, 9, TimeZoneInfo.Utc, keyScope: "b");
        var atNine = first.Merge(second).Entries.Where(entry => entry.Start == new DateTime(2026, 9, 1, 9, 0, 0, DateTimeKind.Utc)).ToArray();

        Assert.Equal(2, atNine.Length);
        Assert.NotEqual(atNine[0].Key, atNine[1].Key);
    }

    [Fact]
    public void FromSchedule_Interval_PlacesWindowAndTicks()
    {
        var definition = ScheduleDefinition.Create().WithDescription("Office").WithTimeZone(TimeZoneInfo.Utc).SetInterval("09:00", "17:00", 60).SetDays(DayFlags.Weekdays).SetMonths(MonthFlags.EveryMonth).Build();

        var calendar = ScheduleCalendar.From(definition, 2026, 9, TimeZoneInfo.Utc);
        var tuesday = calendar.Days.Single(day => day.Date == new DateOnlyModel(2026, 9, 1));

        Assert.Contains(tuesday.Entries, entry => !entry.IsPoint && entry.Title == "Office" && entry.Start == new DateTime(2026, 9, 1, 9, 0, 0, DateTimeKind.Utc) && entry.End == new DateTime(2026, 9, 1, 17, 0, 0, DateTimeKind.Utc));
        Assert.Contains(tuesday.Entries, entry => entry.IsPoint && entry.Start == new DateTime(2026, 9, 1, 9, 0, 0, DateTimeKind.Utc));
        Assert.Contains(tuesday.Entries, entry => entry.IsPoint && entry.Start == new DateTime(2026, 9, 1, 17, 0, 0, DateTimeKind.Utc));
        Assert.Empty(calendar.Days.Single(day => day.Date == new DateOnlyModel(2026, 9, 5)).Entries);
    }
}
