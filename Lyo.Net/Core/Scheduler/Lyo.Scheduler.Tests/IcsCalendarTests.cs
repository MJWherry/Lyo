using Lyo.Schedule.Models;

namespace Lyo.Scheduler.Tests;

public class IcsCalendarTests
{
    private readonly IcsCalendar _ics = new();

    [Fact]
    public void RoundTrip_TimedEvent_PreservesUtcStartEndAndKey()
    {
        var entry = new CalendarEntry(new(2020, 1, 3, 15, 0, 0, DateTimeKind.Utc), new(2020, 1, 3, 16, 0, 0, DateTimeKind.Utc), "meet-1", "Meet", "Notes");
        var again = _ics.Read(_ics.Write(entry));
        Assert.Equal(entry.Key, again.Key);
        Assert.Equal(entry.Start, again.Start);
        Assert.Equal(entry.End, again.End);
        Assert.Equal(DateTimeKind.Utc, again.Start.Kind);
        Assert.Equal("Meet", again.Title);
        Assert.Equal("Notes", again.Description);
    }

    [Fact]
    public void Read_AllDay_IsUtcMidnightSpan()
    {
        const string text = """
                            BEGIN:VCALENDAR
                            BEGIN:VEVENT
                            UID:day-1
                            SUMMARY:Holiday
                            DTSTART;VALUE=DATE:20200103
                            END:VEVENT
                            END:VCALENDAR
                            """;

        var entry = _ics.Read(text);
        Assert.Equal(new(2020, 1, 3, 0, 0, 0, DateTimeKind.Utc), entry.Start);
        Assert.Equal(new(2020, 1, 4, 0, 0, 0, DateTimeKind.Utc), entry.End);
    }

    [Fact]
    public void Expand_Weekly_RespectsCount()
    {
        const string text = """
                            BEGIN:VCALENDAR
                            BEGIN:VEVENT
                            UID:week-1
                            SUMMARY:Standup
                            DTSTART:20200106T150000Z
                            DTEND:20200106T160000Z
                            RRULE:FREQ=WEEKLY
                            END:VEVENT
                            END:VCALENDAR
                            """;

        var items = _ics.Expand(text, null, 3);
        Assert.Equal(3, items.Count);
        Assert.Equal(new(2020, 1, 6, 15, 0, 0, DateTimeKind.Utc), items[0].Start);
        Assert.Equal(new(2020, 1, 13, 15, 0, 0, DateTimeKind.Utc), items[1].Start);
        Assert.Equal(new(2020, 1, 20, 15, 0, 0, DateTimeKind.Utc), items[2].Start);
    }

    [Fact]
    public void Expand_OverCap_IsError()
    {
        const string text = """
                            BEGIN:VCALENDAR
                            BEGIN:VEVENT
                            UID:day-2
                            SUMMARY:Daily
                            DTSTART:20200101T000000Z
                            DTEND:20200101T010000Z
                            RRULE:FREQ=DAILY
                            END:VEVENT
                            END:VCALENDAR
                            """;

        var error = Record.Exception(() => _ics.Expand(text, null, 367));
        Assert.IsType<IcsCalendarException>(error);
    }
}