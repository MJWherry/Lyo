using Lyo.Common.Enums;
using Lyo.Job.Models.Request;
using Lyo.Job.Postgres;
using Lyo.Job.Postgres.Database;
using Lyo.Schedule.Models;
using Mapster;

namespace Lyo.Job.Tests.Postgres;

public class JobBlackoutCalendarMappingTests
{
    [Fact]
    public void JobDefinitionReq_WithSharedInlineBlackoutCalendar_DeduplicatesEntityAcrossSchedules()
    {
        var config = new TypeAdapterConfig();
        config.ConfigureJobMappings();

        var sharedCalendar = new JobBlackoutCalendarReq {
            Name = "Maintenance",
            CreateBlackoutWindows = [new() { Name = "Nightly", DayFlags = DayFlags.Weekdays, StartTime = TimeOnly.Parse("02:00"), EndTime = TimeOnly.Parse("04:00") }]
        };

        var req = new JobDefinitionReq("Test Job") {
            CreateBlackoutCalendar = sharedCalendar,
            CreateSchedules = [
                new() { MonthFlags = MonthFlags.EveryMonth, DayFlags = DayFlags.EveryDay, Type = ScheduleType.Cron, CronExpression = "0 * * * *", CreateBlackoutCalendar = sharedCalendar },
                new() { MonthFlags = MonthFlags.EveryMonth, DayFlags = DayFlags.Weekdays, Type = ScheduleType.Cron, CronExpression = "0 9 * * *", CreateBlackoutCalendar = sharedCalendar }
            ]
        };

        var definition = req.Adapt<JobDefinition>(config);
        var schedules = definition.JobSchedules.ToList();

        Assert.Equal(2, schedules.Count);
        Assert.NotNull(schedules[0].JobBlackoutCalendar);
        Assert.Same(schedules[0].JobBlackoutCalendar, schedules[1].JobBlackoutCalendar);
        Assert.Equal(schedules[0].JobBlackoutCalendarId, schedules[1].JobBlackoutCalendarId);
    }
}
