using Lyo.DateAndTime;

namespace Lyo.Web.Primitives.Tests;

public sealed class CalendarMonthSummaryTests
{
    private static readonly DateOnlyModel Day = new(2026, 9, 15);

    [Fact]
    public void Build_SameHour_CollapsesToOneRow()
    {
        var seven = new DateTime(2026, 9, 15, 7, 0, 0, DateTimeKind.Utc);
        var result = CalendarMonthSummary.Build([(seven, seven), (seven, seven), (seven, seven)], Day, TimeZoneInfo.Utc);
        Assert.Single(result.Rows);
        Assert.Equal(0, result.MoreRows);
        Assert.False(result.Rows[0].AllDay);
        Assert.Equal(7, result.Rows[0].Hour);
        Assert.Equal(3, result.Rows[0].Indexes.Count);
    }

    [Fact]
    public void Build_MoreThanThreeHours_KeepsThreeAndCountsTheRest()
    {
        var result = CalendarMonthSummary.Build([(At(7), At(7)), (At(7), At(7)), (At(12), At(12)), (At(13), At(13)), (At(16), At(16))], Day, TimeZoneInfo.Utc);
        Assert.Equal(3, result.Rows.Count);
        Assert.Equal(1, result.MoreRows);
        Assert.Equal([7, 12, 13], result.Rows.Select(row => row.Hour).ToArray());
        Assert.Equal(2, result.Rows[0].Indexes.Count);
    }

    [Fact]
    public void Build_AllDay_LeadsTheRows()
    {
        var start = new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 9, 16, 0, 0, 0, DateTimeKind.Utc);
        var result = CalendarMonthSummary.Build([(start, end), (At(9), At(9))], Day, TimeZoneInfo.Utc);
        Assert.Equal(2, result.Rows.Count);
        Assert.Equal(0, result.MoreRows);
        Assert.True(result.Rows[0].AllDay);
        Assert.Equal(9, result.Rows[1].Hour);
    }

    private static DateTime At(int hour) => new(2026, 9, 15, hour, 0, 0, DateTimeKind.Utc);
}