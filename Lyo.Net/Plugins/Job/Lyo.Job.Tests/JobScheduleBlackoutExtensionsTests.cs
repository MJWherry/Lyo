using Lyo.Common.Core.Enums;
using Lyo.Job.Models.Enums;
using Lyo.Job.Postgres;
using Lyo.Job.Postgres.Database;

namespace Lyo.Job.Tests;

public class JobScheduleBlackoutExtensionsTests
{
    [Fact]
    public void IsAllowedByBlackoutCalendar_WhenDatedWindowMatches_ReturnsFalse()
    {
        var date = new DateTime(2026, 12, 25, 0, 0, 0, DateTimeKind.Utc);
        var schedule = ScheduleWithWindow(
            new() {
                Name = "Christmas",
                DayFlags = nameof(DayFlags.EveryDay),
                StartDateUtc = date,
                EndDateUtc = date,
                StartTime = "00:00",
                EndTime = "23:59",
                Policy = nameof(JobBlackoutPolicy.Skip),
                Enabled = true
            });

        Assert.False(schedule.IsAllowedByBlackoutCalendar(new(2026, 12, 25, 10, 0, 0, DateTimeKind.Utc)));
        Assert.True(schedule.IsAllowedByBlackoutCalendar(new(2026, 12, 24, 10, 0, 0, DateTimeKind.Utc)));
    }

    [Fact]
    public void AdjustSlotForBlackout_WhenDefer_ReturnsWindowEnd()
    {
        var schedule = ScheduleWithWindow(
            new() {
                Name = "Morning",
                DayFlags = nameof(DayFlags.EveryDay),
                StartTime = "09:00",
                EndTime = "12:00",
                Policy = nameof(JobBlackoutPolicy.Defer),
                Enabled = true
            });

        var slot = new DateTime(2026, 7, 7, 10, 30, 0, DateTimeKind.Utc);
        Assert.Equal(new DateTime(2026, 7, 7, 12, 0, 0, DateTimeKind.Utc), schedule.AdjustSlotForBlackout(slot));
        Assert.True(schedule.IsAllowedByBlackoutCalendar(slot));
    }

    private static JobSchedule ScheduleWithWindow(JobBlackoutWindow window)
        => new() {
            TimeZoneId = "UTC",
            JobBlackoutCalendar = new() {
                Name = "Test",
                Enabled = true,
                JobBlackoutWindows = [window]
            }
        };
}
