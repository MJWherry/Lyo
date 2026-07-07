using Lyo.Common.Enums;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Response;
using Lyo.Job.Scheduler;

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

    private static JobBlackoutCalendarRes CreateCalendar(JobBlackoutPolicy policy, string start, string end)
    {
        var calendarId = Guid.NewGuid();
        return new JobBlackoutCalendarRes(
            calendarId,
            "Test calendar",
            null,
            true,
            [
                new JobBlackoutWindowRes(
                    Guid.NewGuid(),
                    calendarId,
                    "Blackout",
                    DayFlags.EveryDay,
                    TimeOnly.Parse(start),
                    TimeOnly.Parse(end),
                    policy,
                    true)
            ]);
    }
}
