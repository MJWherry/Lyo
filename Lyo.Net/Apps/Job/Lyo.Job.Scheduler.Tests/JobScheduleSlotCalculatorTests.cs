using Lyo.Common.Core.Enums;
using Lyo.Job.Models;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Response;
using Lyo.Schedule.Models;

namespace Lyo.Job.Scheduler.Tests;

/// <summary>
/// Slot math regressions: the misfire scan must return the newest missed slot even for a schedule dense enough to blow the old 10,000-iteration cap, due-slot evaluation must
/// respect the schedule window and blackout deferral, and a per-schedule time zone must actually move the UTC instant across a DST boundary.
/// </summary>
public class JobScheduleSlotCalculatorTests
{
    private static readonly DateTime Now = new(2026, 7, 7, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void GetDueSlot_WhenNextSlotIsInTheFuture_ReturnsNull()
    {
        var schedule = CronSchedule("0 * * * *");
        var due = JobScheduleSlotCalculator.GetDueSlot(schedule.ToScheduleDefinition(), schedule, null, TimeZoneInfo.Utc, Now, Now.AddMinutes(30));
        Assert.Null(due);
    }

    [Fact]
    public void GetDueSlot_WhenSlotHasPassed_ReturnsIt()
    {
        var schedule = CronSchedule("0 * * * *");
        var due = JobScheduleSlotCalculator.GetDueSlot(schedule.ToScheduleDefinition(), schedule, null, TimeZoneInfo.Utc, Now.AddHours(-1), Now.AddMinutes(5));
        Assert.Equal(Now, due);
    }

    [Fact]
    public void GetDueSlot_WhenSlotFallsOutsideTheScheduleWindow_ReturnsNull()
    {
        var schedule = CronSchedule("0 * * * *") with { EndDateUtc = Now.AddHours(-1) };
        var due = JobScheduleSlotCalculator.GetDueSlot(schedule.ToScheduleDefinition(), schedule, null, TimeZoneInfo.Utc, Now.AddHours(-1), Now.AddMinutes(5));
        Assert.Null(due);
    }

    [Fact]
    public void GetDueSlot_WhenBlackoutDefersTheSlotIntoTheFuture_ReturnsNull()
    {
        var schedule = CronSchedule("0 * * * *");
        var calendar = DeferCalendar("11:30", "23:00");

        // Deferring to 23:00 cannot create the run now: the slot is no longer due, so a later check picks it up.
        var due = JobScheduleSlotCalculator.GetDueSlot(schedule.ToScheduleDefinition(), schedule, calendar, TimeZoneInfo.Utc, Now.AddHours(-1), Now.AddMinutes(5));
        Assert.Null(due);
    }

    [Fact]
    public void FindMostRecentMissedSlot_ForAOneMinuteSchedule_ReturnsTheNewestMissedSlot()
    {
        // A day of minute slots is ~1,440 candidates; a five-second schedule over the same lookback exceeded the old 10,000-iteration cap and returned a stale slot or none.
        var schedule = CronSchedule("* * * * *");
        var lookbackStart = Now.AddDays(-1);
        var missed = JobScheduleSlotCalculator.FindMostRecentMissedSlot(schedule.ToScheduleDefinition(), schedule, null, TimeZoneInfo.Utc, lookbackStart, lookbackStart, Now);
        Assert.Equal(Now, missed);
    }

    [Fact]
    public void FindMostRecentMissedSlot_ForASparseSchedule_WidensTheWindowUntilItFindsTheSlot()
    {
        // 03:00 daily: the first one-minute probe window holds nothing, so the doubling search has to reach back nine hours.
        var schedule = CronSchedule("0 3 * * *");
        var lookbackStart = Now.AddDays(-1);
        var missed = JobScheduleSlotCalculator.FindMostRecentMissedSlot(schedule.ToScheduleDefinition(), schedule, null, TimeZoneInfo.Utc, lookbackStart, lookbackStart, Now);
        Assert.Equal(new DateTime(2026, 7, 7, 3, 0, 0, DateTimeKind.Utc), missed);
    }

    [Fact]
    public void FindMostRecentMissedSlot_WhenTheReferenceIsAlreadyCurrent_ReturnsNull()
    {
        var schedule = CronSchedule("* * * * *");
        var missed = JobScheduleSlotCalculator.FindMostRecentMissedSlot(schedule.ToScheduleDefinition(), schedule, null, TimeZoneInfo.Utc, Now, Now.AddDays(-1), Now);
        Assert.Null(missed);
    }

    [Fact]
    public void FindMostRecentMissedSlot_IgnoresSlotsOlderThanTheLookback()
    {
        var schedule = CronSchedule("0 3 * * *");
        var lookbackStart = Now.AddHours(-2);
        var missed = JobScheduleSlotCalculator.FindMostRecentMissedSlot(
            schedule.ToScheduleDefinition(), schedule, null, TimeZoneInfo.Utc, Now.AddDays(-1), lookbackStart, Now);

        Assert.Null(missed);
    }

    [Fact]
    public void FindMostRecentMissedSlot_SkipsABlackedOutSlotAndReturnsAnEarlierOne()
    {
        var schedule = CronSchedule("0 * * * *");
        var calendar = SkipCalendar("10:00", "12:00");
        var now = new DateTime(2026, 7, 7, 11, 30, 0, DateTimeKind.Utc);
        var lookbackStart = now.AddHours(-6);
        var missed = JobScheduleSlotCalculator.FindMostRecentMissedSlot(schedule.ToScheduleDefinition(), schedule, calendar, TimeZoneInfo.Utc, lookbackStart, lookbackStart, now);

        // 10:00 and 11:00 are blacked out, so the newest usable slot is 09:00.
        Assert.Equal(new DateTime(2026, 7, 7, 9, 0, 0, DateTimeKind.Utc), missed);
    }

    /// <summary>
    /// The same wall-clock cron resolves to a different UTC instant either side of a DST transition. This is what the per-schedule <c>TimeZoneId</c> buys, and what the
    /// scheduler's old <c>with { TimeZone = _options.TimeZone }</c> overwrite discarded.
    /// </summary>
    [Fact]
    public void GetDueSlot_WithAScheduleTimeZone_ShiftsTheUtcInstantAcrossDst()
    {
        var schedule = CronSchedule("0 12 * * *") with { TimeZoneId = "America/New_York" };
        var definition = schedule.ToScheduleDefinition();

        var summer = JobScheduleSlotCalculator.GetDueSlot(definition, schedule, null, null, new(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc), new(2026, 7, 8, 0, 0, 0, DateTimeKind.Utc));
        var winter = JobScheduleSlotCalculator.GetDueSlot(definition, schedule, null, null, new(2026, 1, 7, 0, 0, 0, DateTimeKind.Utc), new(2026, 1, 8, 0, 0, 0, DateTimeKind.Utc));

        Assert.Equal(new DateTime(2026, 7, 7, 16, 0, 0, DateTimeKind.Utc), summer); // EDT, UTC-4
        Assert.Equal(new DateTime(2026, 1, 7, 17, 0, 0, DateTimeKind.Utc), winter); // EST, UTC-5
    }

    /// <summary>Spring forward skips 02:00-03:00 local, so a 02:30 daily cron has no valid instant that day and cannot throw or return a nonexistent local time.</summary>
    [Fact]
    public void GetDueSlot_OnASpringForwardGap_DoesNotProduceANonexistentLocalTime()
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
        var schedule = CronSchedule("30 2 * * *") with { TimeZoneId = "America/New_York" };

        // 2026-03-08 is the US spring-forward date.
        var due = JobScheduleSlotCalculator.GetDueSlot(
            schedule.ToScheduleDefinition(), schedule, null, null, new(2026, 3, 8, 5, 0, 0, DateTimeKind.Utc), new(2026, 3, 9, 12, 0, 0, DateTimeKind.Utc));

        Assert.NotNull(due);
        Assert.False(zone.IsInvalidTime(TimeZoneInfo.ConvertTimeFromUtc(due!.Value, zone)));
    }

    [Fact]
    public void IsWithinWindow_HonoursBothBounds()
    {
        var schedule = CronSchedule("0 * * * *") with { StartDateUtc = Now.AddHours(-1), EndDateUtc = Now.AddHours(1) };
        Assert.False(JobScheduleSlotCalculator.IsWithinWindow(schedule, Now.AddHours(-2)));
        Assert.True(JobScheduleSlotCalculator.IsWithinWindow(schedule, Now));
        Assert.True(JobScheduleSlotCalculator.IsWithinWindow(schedule, Now.AddHours(1)));
        Assert.False(JobScheduleSlotCalculator.IsWithinWindow(schedule, Now.AddHours(2)));
    }

    private static JobScheduleRes CronSchedule(string cron)
        => new(
            Guid.NewGuid(), Guid.NewGuid(), MonthFlags.EveryMonth, DayFlags.EveryDay, ScheduleType.Cron, null, null, null, null, cron, true, null, cron);

    private static JobBlackoutCalendarRes DeferCalendar(string start, string end) => Calendar(JobBlackoutPolicy.Defer, start, end);

    private static JobBlackoutCalendarRes SkipCalendar(string start, string end) => Calendar(JobBlackoutPolicy.Skip, start, end);

    private static JobBlackoutCalendarRes Calendar(JobBlackoutPolicy policy, string start, string end)
    {
        var calendarId = Guid.NewGuid();
        return new(
            calendarId, "Test calendar", null, true, [new(Guid.NewGuid(), calendarId, "Blackout", DayFlags.EveryDay, TimeOnly.Parse(start), TimeOnly.Parse(end), policy, true)]);
    }
}
