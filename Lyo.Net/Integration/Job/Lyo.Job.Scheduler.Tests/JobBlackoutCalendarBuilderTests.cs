using Lyo.DateAndTime;
using Lyo.Job.Models.Builders;
using Lyo.Job.Models.Enums;

namespace Lyo.Job.Scheduler.Tests;

public class JobBlackoutCalendarBuilderTests
{
    [Fact]
    public void AddBlackoutHoliday_ExpandsToConcreteDatedWindows()
    {
        var calendar = JobBlackoutCalendarBuilder.New("Federal")
            .AddBlackoutHoliday(HolidayInfo.ChristmasDay, 2026, 2028)
            .Build();

        Assert.Equal(3, calendar.CreateBlackoutWindows.Count);
        Assert.All(calendar.CreateBlackoutWindows, w => {
            Assert.NotNull(w.StartDateUtc);
            Assert.Equal(w.StartDateUtc, w.EndDateUtc);
            Assert.Equal(TimeOnly.Parse("00:00"), w.StartTime);
            Assert.Equal(TimeOnly.Parse("23:59"), w.EndTime);
        });

        Assert.Equal(new DateTime(2026, 12, 25, 0, 0, 0, DateTimeKind.Utc), calendar.CreateBlackoutWindows[0].StartDateUtc);
        Assert.Equal("Christmas Day (2026)", calendar.CreateBlackoutWindows[0].Name);
    }

    [Fact]
    public void AddFederalHolidayBlackouts_ExpandsEveryFederalHolidayForDefaultYearSpan()
    {
        var fromYear = DateTime.UtcNow.Year;
        var calendar = JobBlackoutCalendarBuilder.New("Federal")
            .AddFederalHolidayBlackouts()
            .Build();

        Assert.Equal(HolidayInfo.FederalHolidays.Count * 10, calendar.CreateBlackoutWindows.Count);
        Assert.Contains(calendar.CreateBlackoutWindows, w => w.Name == $"Christmas Day ({fromYear})");
        Assert.All(calendar.CreateBlackoutWindows, w => Assert.NotNull(w.StartDateUtc));
    }

    [Fact]
    public void AddBlackoutHolidays_AcceptsMultipleHolidayRecords()
    {
        var calendar = JobBlackoutCalendarBuilder.New("Selected")
            .AddBlackoutHolidays([HolidayInfo.ChristmasDay, HolidayInfo.ThanksgivingDay], 2026, 2026)
            .Build();

        Assert.Equal(2, calendar.CreateBlackoutWindows.Count);
        Assert.Contains(calendar.CreateBlackoutWindows, w => w.Name == "Christmas Day (2026)");
        Assert.Contains(calendar.CreateBlackoutWindows, w => w.Name == "Thanksgiving Day (2026)");
    }

    [Fact]
    public void AddBlackoutHoliday_WithUnknownHoliday_Throws()
    {
        Assert.Throws<ArgumentException>(() => JobBlackoutCalendarBuilder.New("Bad").AddBlackoutHoliday(HolidayInfo.Unknown, 2026, 2026));
    }
}
