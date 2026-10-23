using Lyo.DateAndTime;

namespace Lyo.Web.Primitives.Tests;

public sealed class CalendarEventLayoutTests
{
    private static readonly DateOnlyModel Day = new(2026, 9, 15);

    [Fact]
    public void Place_SameInstant_UsesTwoLanes()
    {
        var at = new DateTime(2026, 9, 15, 9, 0, 0, DateTimeKind.Utc);
        var blocks = CalendarEventLayout.Place([(at, at), (at, at)], Day, TimeZoneInfo.Utc);
        Assert.Equal(2, blocks.Count);
        Assert.All(blocks, block => Assert.False(block.AllDay));
        Assert.Equal(0, blocks[0].Lane);
        Assert.Equal(1, blocks[1].Lane);
        Assert.Equal(2, blocks[0].LaneCount);
        Assert.Equal(2, blocks[1].LaneCount);
        Assert.Equal(blocks[0].TopPercent, blocks[1].TopPercent);
    }

    [Fact]
    public void Place_SeparatedSameDay_EachStaysInLaneZero()
    {
        var morning = new DateTime(2026, 9, 15, 9, 0, 0, DateTimeKind.Utc);
        var afternoon = new DateTime(2026, 9, 15, 15, 0, 0, DateTimeKind.Utc);
        var blocks = CalendarEventLayout.Place([(morning, morning), (afternoon, afternoon)], Day, TimeZoneInfo.Utc);
        Assert.All(blocks, block => Assert.Equal(0, block.Lane));
        Assert.All(blocks, block => Assert.Equal(1, block.LaneCount));
        Assert.True(blocks[1].TopPercent > blocks[0].TopPercent);
    }

    [Fact]
    public void Place_OverlappingSpans_SitSideBySide()
    {
        var firstStart = new DateTime(2026, 9, 15, 9, 0, 0, DateTimeKind.Utc);
        var firstEnd = new DateTime(2026, 9, 15, 11, 0, 0, DateTimeKind.Utc);
        var secondStart = new DateTime(2026, 9, 15, 10, 0, 0, DateTimeKind.Utc);
        var secondEnd = new DateTime(2026, 9, 15, 12, 0, 0, DateTimeKind.Utc);
        var blocks = CalendarEventLayout.Place([(firstStart, firstEnd), (secondStart, secondEnd)], Day, TimeZoneInfo.Utc);
        Assert.False(blocks[0].AllDay);
        Assert.Equal(0, blocks[0].Lane);
        Assert.Equal(1, blocks[1].Lane);
        Assert.Equal(2, blocks[0].LaneCount);
        Assert.Equal(2, blocks[1].LaneCount);
    }

    [Fact]
    public void Place_CoveringTheDay_IsAllDay()
    {
        var start = new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 9, 16, 0, 0, 0, DateTimeKind.Utc);
        var blocks = CalendarEventLayout.Place([(start, end)], Day, TimeZoneInfo.Utc);
        Assert.True(blocks[0].AllDay);
    }
}