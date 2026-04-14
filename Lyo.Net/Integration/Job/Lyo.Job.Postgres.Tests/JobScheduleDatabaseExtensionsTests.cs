using Lyo.Common.Enums;
using Lyo.Job.Postgres.Database;
using Lyo.Schedule.Models;

namespace Lyo.Job.Postgres.Tests;

public class JobScheduleDatabaseExtensionsTests
{
    [Fact]
    public void ToScheduleDefinition_WithValidSchedule_ReturnsCorrectScheduleDefinition()
    {
        var jobSchedule = new JobSchedule {
            Id = Guid.NewGuid(),
            JobDefinitionId = Guid.NewGuid(),
            Type = nameof(ScheduleType.SetTimes),
            DayFlags = nameof(DayFlags.Weekdays),
            MonthFlags = nameof(MonthFlags.EveryMonth),
            Times = ["09:00", "14:00", "18:00"],
            Enabled = true,
            Description = "Test schedule"
        };

        var result = jobSchedule.ToScheduleDefinition();
        Assert.Equal(ScheduleType.SetTimes, result.Type);
        Assert.Equal(DayFlags.Weekdays, result.DayFlags);
        Assert.Equal(MonthFlags.EveryMonth, result.MonthFlags);
        Assert.NotNull(result.Times);
        Assert.Equal(3, result.Times!.Count);
        Assert.Equal(new(9, 0), result.Times[0]);
        Assert.Equal(new(14, 0), result.Times[1]);
        Assert.Equal(new(18, 0), result.Times[2]);
        Assert.True(result.Enabled);
        Assert.Equal("Test schedule", result.Description);
    }

    [Fact]
    public void ToScheduleDefinition_WithIntervalSchedule_ReturnsCorrectStartAndEndTime()
    {
        var jobSchedule = new JobSchedule {
            Id = Guid.NewGuid(),
            JobDefinitionId = Guid.NewGuid(),
            Type = nameof(ScheduleType.Interval),
            DayFlags = nameof(DayFlags.EveryDay),
            MonthFlags = nameof(MonthFlags.EveryMonth),
            StartTime = "08:00",
            EndTime = "17:00",
            IntervalMinutes = 30,
            Enabled = true
        };

        var result = jobSchedule.ToScheduleDefinition();
        Assert.Equal(ScheduleType.Interval, result.Type);
        Assert.Equal(new TimeOnly(8, 0), result.StartTime);
        Assert.Equal(new TimeOnly(17, 0), result.EndTime);
        Assert.Equal(30, result.IntervalMinutes);
    }

    [Fact]
    public void ToScheduleDefinition_WithNullJobSchedule_ThrowsArgumentNullException()
    {
        JobSchedule? jobSchedule = null;
        var ex = Assert.Throws<ArgumentNullException>(() => jobSchedule!.ToScheduleDefinition());
        Assert.Equal("jobSchedule", ex.ParamName);
    }

    [Fact]
    public void ToScheduleDefinition_WithInvalidTypeString_FallsBackToSetTimes()
    {
        var jobSchedule = new JobSchedule {
            Id = Guid.NewGuid(),
            JobDefinitionId = Guid.NewGuid(),
            Type = "InvalidType",
            DayFlags = nameof(DayFlags.EveryDay),
            MonthFlags = nameof(MonthFlags.EveryMonth),
            Enabled = true
        };

        var result = jobSchedule.ToScheduleDefinition();
        Assert.Equal(ScheduleType.SetTimes, result.Type);
    }

    [Fact]
    public void ToScheduleDefinition_WithInvalidDayFlagsString_FallsBackToNone()
    {
        var jobSchedule = new JobSchedule {
            Id = Guid.NewGuid(),
            JobDefinitionId = Guid.NewGuid(),
            Type = nameof(ScheduleType.SetTimes),
            DayFlags = "InvalidDayFlags",
            MonthFlags = nameof(MonthFlags.EveryMonth),
            Times = ["09:00"],
            Enabled = true
        };

        var result = jobSchedule.ToScheduleDefinition();
        Assert.Equal(DayFlags.None, result.DayFlags);
    }

    [Fact]
    public void ToScheduleDefinition_WithInvalidMonthFlagsString_FallsBackToNone()
    {
        var jobSchedule = new JobSchedule {
            Id = Guid.NewGuid(),
            JobDefinitionId = Guid.NewGuid(),
            Type = nameof(ScheduleType.SetTimes),
            DayFlags = nameof(DayFlags.EveryDay),
            MonthFlags = "InvalidMonthFlags",
            Times = ["09:00"],
            Enabled = true
        };

        var result = jobSchedule.ToScheduleDefinition();
        Assert.Equal(MonthFlags.None, result.MonthFlags);
    }

    [Fact]
    public void ToScheduleDefinition_WithNullTimes_ReturnsNullTimes()
    {
        var jobSchedule = new JobSchedule {
            Id = Guid.NewGuid(),
            JobDefinitionId = Guid.NewGuid(),
            Type = nameof(ScheduleType.SetTimes),
            DayFlags = nameof(DayFlags.EveryDay),
            MonthFlags = nameof(MonthFlags.EveryMonth),
            Times = null,
            Enabled = true
        };

        var result = jobSchedule.ToScheduleDefinition();
        Assert.Null(result.Times);
    }

    [Fact]
    public void ToScheduleDefinition_WithEmptyTimes_ReturnsNullTimes()
    {
        var jobSchedule = new JobSchedule {
            Id = Guid.NewGuid(),
            JobDefinitionId = Guid.NewGuid(),
            Type = nameof(ScheduleType.SetTimes),
            DayFlags = nameof(DayFlags.EveryDay),
            MonthFlags = nameof(MonthFlags.EveryMonth),
            Times = [],
            Enabled = true
        };

        var result = jobSchedule.ToScheduleDefinition();
        Assert.Null(result.Times);
    }

    [Fact]
    public void ToScheduleDefinition_WithEmptyStartTime_ReturnsNullStartTime()
    {
        var jobSchedule = new JobSchedule {
            Id = Guid.NewGuid(),
            JobDefinitionId = Guid.NewGuid(),
            Type = nameof(ScheduleType.Interval),
            DayFlags = nameof(DayFlags.EveryDay),
            MonthFlags = nameof(MonthFlags.EveryMonth),
            StartTime = "",
            EndTime = "17:00",
            IntervalMinutes = 60,
            Enabled = true
        };

        var result = jobSchedule.ToScheduleDefinition();
        Assert.Null(result.StartTime);
    }

    [Fact]
    public void ToScheduleDefinition_WithNullStartTime_ReturnsNullStartTime()
    {
        var jobSchedule = new JobSchedule {
            Id = Guid.NewGuid(),
            JobDefinitionId = Guid.NewGuid(),
            Type = nameof(ScheduleType.Interval),
            DayFlags = nameof(DayFlags.EveryDay),
            MonthFlags = nameof(MonthFlags.EveryMonth),
            StartTime = null,
            EndTime = "17:00",
            IntervalMinutes = 60,
            Enabled = true
        };

        var result = jobSchedule.ToScheduleDefinition();
        Assert.Null(result.StartTime);
    }

    [Fact]
    public void ToScheduleDefinition_WithEmptyEndTime_ReturnsNullEndTime()
    {
        var jobSchedule = new JobSchedule {
            Id = Guid.NewGuid(),
            JobDefinitionId = Guid.NewGuid(),
            Type = nameof(ScheduleType.Interval),
            DayFlags = nameof(DayFlags.EveryDay),
            MonthFlags = nameof(MonthFlags.EveryMonth),
            StartTime = "08:00",
            EndTime = "",
            IntervalMinutes = 60,
            Enabled = true
        };

        var result = jobSchedule.ToScheduleDefinition();
        Assert.Null(result.EndTime);
    }

    [Fact]
    public void ToScheduleDefinition_WithCombinedDayFlags_ParsesCorrectly()
    {
        var jobSchedule = new JobSchedule {
            Id = Guid.NewGuid(),
            JobDefinitionId = Guid.NewGuid(),
            Type = nameof(ScheduleType.SetTimes),
            DayFlags = "Mon, Tue, Wed",
            MonthFlags = nameof(MonthFlags.EveryMonth),
            Times = ["09:00"],
            Enabled = true
        };

        var result = jobSchedule.ToScheduleDefinition();
        Assert.Equal(DayFlags.Mon | DayFlags.Tue | DayFlags.Wed, result.DayFlags);
    }

    [Fact]
    public void ToScheduleDefinition_WithOneShotType_ParsesCorrectly()
    {
        var jobSchedule = new JobSchedule {
            Id = Guid.NewGuid(),
            JobDefinitionId = Guid.NewGuid(),
            Type = nameof(ScheduleType.OneShot),
            DayFlags = nameof(DayFlags.None),
            MonthFlags = nameof(MonthFlags.None),
            Enabled = false,
            Description = "One-time job"
        };

        var result = jobSchedule.ToScheduleDefinition();
        Assert.Equal(ScheduleType.OneShot, result.Type);
        Assert.False(result.Enabled);
    }

    [Fact]
    public void ToScheduleDefinition_WithMidnightTime_ParsesCorrectly()
    {
        var jobSchedule = new JobSchedule {
            Id = Guid.NewGuid(),
            JobDefinitionId = Guid.NewGuid(),
            Type = nameof(ScheduleType.SetTimes),
            DayFlags = nameof(DayFlags.EveryDay),
            MonthFlags = nameof(MonthFlags.EveryMonth),
            Times = ["00:00"],
            Enabled = true
        };

        var result = jobSchedule.ToScheduleDefinition();
        Assert.NotNull(result.Times);
        Assert.Single(result.Times!);
        Assert.Equal(TimeOnly.MinValue, result.Times![0]);
    }

    [Fact]
    public void ToScheduleDefinition_WithEndOfDayTime_ParsesCorrectly()
    {
        var jobSchedule = new JobSchedule {
            Id = Guid.NewGuid(),
            JobDefinitionId = Guid.NewGuid(),
            Type = nameof(ScheduleType.SetTimes),
            DayFlags = nameof(DayFlags.EveryDay),
            MonthFlags = nameof(MonthFlags.EveryMonth),
            Times = ["23:59"],
            Enabled = true
        };

        var result = jobSchedule.ToScheduleDefinition();
        Assert.NotNull(result.Times);
        Assert.Single(result.Times!);
        Assert.Equal(new(23, 59), result.Times![0]);
    }
}