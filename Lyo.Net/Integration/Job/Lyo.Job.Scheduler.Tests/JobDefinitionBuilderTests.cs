using Lyo.Common.Enums;
using Lyo.Job.Models.Builders;

namespace Lyo.Job.Scheduler.Tests;

public class JobDefinitionBuilderTests
{
    [Fact]
    public void WithBlackoutCalendar_OnDefinition_AppliesInlineCalendarToEverySchedule()
    {
        var definition = JobDefinitionBuilder.New("Test Job")
            .WithBlackoutCalendar("Maintenance", b => b.AddBlackoutWindow("Nightly", DayFlags.Weekdays, "02:00", "04:00"))
            .AddSchedule(s => s.EveryDay().SetCron("0 * * * *"))
            .AddSchedule(s => s.Weekdays().SetCron("0 9 * * *"))
            .Build();

        Assert.Equal(2, definition.CreateSchedules.Count);
        foreach (var schedule in definition.CreateSchedules) {
            Assert.Same(definition.CreateBlackoutCalendar, schedule.CreateBlackoutCalendar);
            Assert.Null(schedule.JobBlackoutCalendarId);
        }

        Assert.Equal("Maintenance", definition.CreateBlackoutCalendar!.Name);
        Assert.Single(definition.CreateBlackoutCalendar.CreateBlackoutWindows);
    }

    [Fact]
    public void WithBlackoutCalendar_OnDefinition_AppliesCalendarIdToEverySchedule()
    {
        var calendarId = Guid.NewGuid();
        var definition = JobDefinitionBuilder.New("Test Job")
            .AddSchedule(s => s.EveryDay().SetCron("0 * * * *"))
            .WithBlackoutCalendar(calendarId)
            .AddSchedule(s => s.Weekdays().SetCron("0 9 * * *"))
            .Build();

        Assert.Equal(2, definition.CreateSchedules.Count);
        foreach (var schedule in definition.CreateSchedules) {
            Assert.Equal(calendarId, schedule.JobBlackoutCalendarId);
            Assert.Null(schedule.CreateBlackoutCalendar);
        }
    }

    [Fact]
    public void WithBlackoutCalendar_ById_CascadesToSchedulesAddedBeforeAndAfter()
    {
        var calendarId = Guid.NewGuid();
        var definition = JobDefinitionBuilder.New("Test Job")
            .AddSchedule(s => s.EveryDay().SetCron("0 * * * *"))
            .WithBlackoutCalendar(calendarId)
            .AddSchedule(s => s.Weekdays().SetCron("0 9 * * *"))
            .Build();

        Assert.Equal(2, definition.CreateSchedules.Count);
        foreach (var schedule in definition.CreateSchedules) {
            Assert.Equal(calendarId, schedule.JobBlackoutCalendarId);
            Assert.Null(schedule.CreateBlackoutCalendar);
        }
    }

    [Fact]
    public void WithBlackoutCalendar_Inline_CascadesToSchedulesAddedBeforeAndAfter()
    {
        var definition = JobDefinitionBuilder.New("Test Job")
            .AddSchedule(s => s.EveryDay().SetCron("0 * * * *"))
            .WithBlackoutCalendar("Maintenance", b => b.AddBlackoutWindow("Nightly", DayFlags.Weekdays, "02:00", "04:00"))
            .AddSchedule(s => s.Weekdays().SetCron("0 9 * * *"))
            .Build();

        Assert.Equal(2, definition.CreateSchedules.Count);
        foreach (var schedule in definition.CreateSchedules) {
            Assert.Same(definition.CreateBlackoutCalendar, schedule.CreateBlackoutCalendar);
            Assert.Null(schedule.JobBlackoutCalendarId);
        }
    }

    [Fact]
    public void AddBlackoutWindow_OnDefinition_CascadesToSchedulesAddedBefore()
    {
        var definition = JobDefinitionBuilder.New("Test Job")
            .AddSchedule(s => s.EveryDay().SetCron("0 * * * *"))
            .AddBlackoutWindow("Nightly", DayFlags.Weekdays, "02:00", "04:00")
            .AddSchedule(s => s.Weekdays().SetCron("0 9 * * *"))
            .Build();

        Assert.NotNull(definition.CreateBlackoutCalendar);
        Assert.Single(definition.CreateBlackoutCalendar!.CreateBlackoutWindows);
        foreach (var schedule in definition.CreateSchedules)
            Assert.Same(definition.CreateBlackoutCalendar, schedule.CreateBlackoutCalendar);
    }

    [Fact]
    public void AddSchedule_WithInlineBlackoutCalendar_OverridesDefinitionDefault()
    {
        var definition = JobDefinitionBuilder.New("Test Job")
            .WithBlackoutCalendar("Default", b => b.AddBlackoutWindow("Default Window", DayFlags.EveryDay, "01:00", "02:00"))
            .AddSchedule(s => s.EveryDay().SetCron("0 * * * *").WithBlackoutCalendar("Override", b => b.AddBlackoutWindow("Override Window", DayFlags.Weekdays, "03:00", "04:00")))
            .AddSchedule(s => s.Weekdays().SetCron("0 9 * * *"))
            .Build();

        var overrideSchedule = definition.CreateSchedules[0];
        var inheritedSchedule = definition.CreateSchedules[1];
        Assert.Equal("Override", overrideSchedule.CreateBlackoutCalendar!.Name);
        Assert.NotSame(definition.CreateBlackoutCalendar, overrideSchedule.CreateBlackoutCalendar);
        Assert.Same(definition.CreateBlackoutCalendar, inheritedSchedule.CreateBlackoutCalendar);
    }

    [Fact]
    public void AddSchedule_WithBlackoutCalendarId_IsNotOverwrittenByDefinitionCascade()
    {
        var scheduleId = Guid.NewGuid();
        var definition = JobDefinitionBuilder.New("Test Job")
            .AddSchedule(s => s.EveryDay().SetCron("0 * * * *").WithBlackoutCalendar(scheduleId))
            .WithBlackoutCalendar("Maintenance", b => b.AddBlackoutWindow("Nightly", DayFlags.Weekdays, "02:00", "04:00"))
            .AddSchedule(s => s.Weekdays().SetCron("0 9 * * *"))
            .Build();

        Assert.Equal(scheduleId, definition.CreateSchedules[0].JobBlackoutCalendarId);
        Assert.Null(definition.CreateSchedules[0].CreateBlackoutCalendar);
        Assert.Same(definition.CreateBlackoutCalendar, definition.CreateSchedules[1].CreateBlackoutCalendar);
    }

    [Fact]
    public void AddSchedule_WithAddBlackoutWindow_EmbedsOnSchedule()
    {
        var definition = JobDefinitionBuilder.New("Test Job")
            .AddSchedule(s => s.EveryDay().SetCron("0 * * * *").AddBlackoutWindow("Nightly", DayFlags.EveryDay, "01:00", "02:00"))
            .Build();

        Assert.Single(definition.CreateSchedules);
        var schedule = definition.CreateSchedules[0];
        Assert.NotNull(schedule.CreateBlackoutCalendar);
        Assert.Equal("Blackout", schedule.CreateBlackoutCalendar!.Name);
        Assert.Single(schedule.CreateBlackoutCalendar.CreateBlackoutWindows);
        Assert.Null(schedule.JobBlackoutCalendarId);
    }
}