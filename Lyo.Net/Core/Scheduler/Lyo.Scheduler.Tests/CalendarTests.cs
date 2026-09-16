using Lyo.DateAndTime;
using Lyo.EntityReference.Models;
using Lyo.Schedule.Models;

namespace Lyo.Scheduler.Tests;

public class CalendarTests
{
    private static readonly DateTime MidnightSpanStart = new(2026, 9, 15, 23, 0, 0, DateTimeKind.Utc);

    private static readonly DateTime MidnightSpanEnd = new(2026, 9, 16, 1, 0, 0, DateTimeKind.Utc);

    private static readonly DateTime NewYorkInstant = new(2026, 9, 30, 2, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ForMonth_September2026_SundayStart_PadsAugust30ThroughOctober3()
    {
        var calendar = Calendar.ForMonth(2026, 9, TimeZoneInfo.Utc);

        Assert.Equal(new DateOnlyModel(2026, 8, 30), calendar.Start);
        Assert.Equal(new DateOnlyModel(2026, 10, 3), calendar.End);
        Assert.Equal(35, calendar.Days.Count);
        Assert.Equal(new DateOnlyModel(2026, 8, 30), calendar.Days[0].Date);
        Assert.Equal(new DateOnlyModel(2026, 10, 3), calendar.Days[^1].Date);
    }

    [Fact]
    public void FromTimeFrames_CrossesLocalMidnight_ListedOnBothDays()
    {
        var frame = new TimeFrame(MidnightSpanStart, MidnightSpanEnd, "night", "Night");
        var calendar = Calendar.FromTimeFrames(new(2026, 9, 15), new(2026, 9, 16), TimeZoneInfo.Utc, [frame]);

        Assert.Single(calendar.Entries);
        Assert.Contains(calendar.Days.Single(d => d.Date == new DateOnlyModel(2026, 9, 15)).Entries, e => e.Key == "night");
        Assert.Contains(calendar.Days.Single(d => d.Date == new DateOnlyModel(2026, 9, 16)).Entries, e => e.Key == "night");
    }

    [Fact]
    public void FromTimeFrames_OutsideRange_Dropped()
    {
        var frame = new TimeFrame(new(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc), new(2026, 8, 1, 13, 0, 0, DateTimeKind.Utc), "aug", "August");
        var calendar = Calendar.FromTimeFrames(new(2026, 9, 1), new(2026, 9, 30), TimeZoneInfo.Utc, [frame]);

        Assert.Empty(calendar.Entries);
        Assert.All(calendar.Days, day => Assert.Empty(day.Entries));
    }

    [Fact]
    public void WithTimeZone_UtcMorning_LandsOnPreviousEveningInNewYork()
    {
        var frame = new TimeFrame(NewYorkInstant, NewYorkInstant, "call", "Call");
        var utc = Calendar.FromTimeFrames(new(2026, 9, 1), new(2026, 9, 30), TimeZoneInfo.Utc, [frame]);
        var eastern = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

        var local = utc.WithTimeZone(eastern);
        var day = Assert.Single(local.Days, d => d.Entries.Count > 0);

        Assert.Equal(new DateOnlyModel(2026, 9, 29), day.Date);
        Assert.Equal(22, TimeZoneInfo.ConvertTimeFromUtc(NewYorkInstant, eastern).Hour);
    }

    [Fact]
    public void Format_SameInstant_SwitchesBetween24HourAnd12Hour()
    {
        var eastern = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

        Assert.Equal("22:00", CalendarClock.Format(NewYorkInstant, eastern, use24Hour: true));
        Assert.Equal("10:00 PM", CalendarClock.Format(NewYorkInstant, eastern, use24Hour: false));
    }

    [Fact]
    public void FromTimeFrames_CopiesTitleDescriptionHueAndEntityRefs()
    {
        var person = new EntityRef("Person", "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var frame = new TimeFrame(
            new(2026, 9, 15, 15, 0, 0, DateTimeKind.Utc),
            new(2026, 9, 15, 16, 0, 0, DateTimeKind.Utc),
            "meet",
            "Standup",
            "Daily sync",
            228,
            "meeting",
            [person]);

        var calendar = Calendar.FromTimeFrames(new(2026, 9, 15), new(2026, 9, 15), TimeZoneInfo.Utc, [frame]);
        var entry = Assert.Single(calendar.Entries);

        Assert.Equal("Standup", entry.Title);
        Assert.Equal("Daily sync", entry.Description);
        Assert.Equal(228, entry.Hue);
        Assert.Equal("meeting", entry.Category);
        var entity = Assert.Single(entry.Refs);
        Assert.Equal("Person", entity.EntityType);
        Assert.Equal(person.EntityId, entity.EntityId);
    }
}
