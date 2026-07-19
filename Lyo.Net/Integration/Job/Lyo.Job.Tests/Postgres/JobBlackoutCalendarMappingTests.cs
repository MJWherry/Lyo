using System.Text.Json;
using System.Text.Json.Serialization;
using Lyo.Common.Enums;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Request;
using Lyo.Job.Postgres.Database;
using Lyo.Job.Postgres.Mapping;
using Lyo.Schedule.Models;

namespace Lyo.Job.Tests.Postgres;

public class JobBlackoutCalendarMappingTests
{
    private static readonly JobLyoMapper Mapper = new();

    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public void JobDefinitionReq_WithSharedInlineBlackoutCalendar_DeduplicatesEntityAcrossSchedules()
    {
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

        var definition = Mapper.Map<JobDefinition>(req);
        var schedules = definition.JobSchedules.ToList();

        Assert.Equal(2, schedules.Count);
        Assert.NotNull(schedules[0].JobBlackoutCalendar);
        Assert.Same(schedules[0].JobBlackoutCalendar, schedules[1].JobBlackoutCalendar);
        Assert.Equal(schedules[0].JobBlackoutCalendarId, schedules[1].JobBlackoutCalendarId);
        Assert.Single(schedules[0].JobBlackoutCalendar!.JobBlackoutWindows);
        Assert.NotEqual(default, schedules[0].JobBlackoutCalendar!.CreatedTimestamp);
        Assert.All(schedules[0].JobBlackoutCalendar.JobBlackoutWindows, w => Assert.NotEqual(default, w.CreatedTimestamp));
    }

    [Fact]
    public void JobDefinitionReq_WithDefinitionOnlyInlineBlackoutCalendar_AppliesToSchedulesWithoutCascade()
    {
        var req = new JobDefinitionReq("Test Job") {
            CreateBlackoutCalendar = new() {
                Name = "Maintenance",
                CreateBlackoutWindows = [
                    new() {
                        Name = "Nightly", DayFlags = DayFlags.Weekdays, StartTime = TimeOnly.Parse("02:00"), EndTime = TimeOnly.Parse("04:00"),
                        Policy = JobBlackoutPolicy.Defer
                    }
                ]
            },
            CreateSchedules = [
                new() { MonthFlags = MonthFlags.EveryMonth, DayFlags = DayFlags.EveryDay, Type = ScheduleType.Cron, CronExpression = "0 * * * *" },
                new() { MonthFlags = MonthFlags.EveryMonth, DayFlags = DayFlags.Weekdays, Type = ScheduleType.Cron, CronExpression = "0 9 * * *" }
            ]
        };

        var definition = Mapper.Map<JobDefinition>(req);
        var schedules = definition.JobSchedules.ToList();

        Assert.Equal(2, schedules.Count);
        Assert.NotNull(schedules[0].JobBlackoutCalendar);
        Assert.Same(schedules[0].JobBlackoutCalendar, schedules[1].JobBlackoutCalendar);
        Assert.Equal("Maintenance", schedules[0].JobBlackoutCalendar!.Name);
        Assert.True(schedules[0].JobBlackoutCalendar.Enabled);

        var calendar = schedules[0].JobBlackoutCalendar!;
        var window = Assert.Single(calendar.JobBlackoutWindows);
        Assert.Equal("Nightly", window.Name);
        Assert.Equal(nameof(DayFlags.Weekdays), window.DayFlags);
        Assert.Equal(nameof(JobBlackoutPolicy.Defer), window.Policy);
        Assert.True(window.Enabled);
        Assert.NotEqual(default, calendar.CreatedTimestamp);
        Assert.NotEqual(default, window.CreatedTimestamp);
    }

    [Fact]
    public void JobDefinitionReq_AfterJsonRoundTrip_StructurallySharedCalendars_Deduplicate()
    {
        var sharedCalendar = new JobBlackoutCalendarReq {
            Name = "Holidays",
            CreateBlackoutWindows = [
                new() {
                    Name = "Christmas", DayFlags = DayFlags.EveryDay, StartTime = TimeOnly.Parse("00:00"), EndTime = TimeOnly.Parse("23:59"),
                    Policy = JobBlackoutPolicy.Skip, StartDateUtc = new DateTime(2026, 12, 25, 0, 0, 0, DateTimeKind.Utc),
                    EndDateUtc = new DateTime(2026, 12, 25, 0, 0, 0, DateTimeKind.Utc)
                }
            ]
        };

        var req = new JobDefinitionReq("JSON Job") {
            Type = "Test",
            WorkerType = "cs",
            CreateBlackoutCalendar = sharedCalendar,
            CreateSchedules = [
                new() { MonthFlags = MonthFlags.EveryMonth, DayFlags = DayFlags.EveryDay, Type = ScheduleType.Cron, CronExpression = "0 * * * *", CreateBlackoutCalendar = sharedCalendar },
                new() { MonthFlags = MonthFlags.EveryMonth, DayFlags = DayFlags.Weekdays, Type = ScheduleType.Cron, CronExpression = "0 9 * * *", CreateBlackoutCalendar = sharedCalendar }
            ]
        };

        var json = JsonSerializer.Serialize(req, JsonOptions);
        var roundTripped = JsonSerializer.Deserialize<JobDefinitionReq>(json, JsonOptions)!;

        Assert.NotSame(roundTripped.CreateBlackoutCalendar, roundTripped.CreateSchedules[0].CreateBlackoutCalendar);

        var definition = Mapper.Map<JobDefinition>(roundTripped);
        var schedules = definition.JobSchedules.ToList();
        Assert.Same(schedules[0].JobBlackoutCalendar, schedules[1].JobBlackoutCalendar);
    }
}
