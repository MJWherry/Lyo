using Lyo.Api.Services.Crud.Create;
using Lyo.Common.Enums;
using Lyo.Common.Identifiers;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Request;
using Lyo.Job.Models.Response;
using Lyo.Job.Postgres;
using Lyo.Job.Postgres.Database;
using Lyo.Schedule.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Job.Tests.Postgres;

public class JobBlackoutCalendarCreateTests(JobPostgresFixture fixture) : IClassFixture<JobPostgresFixture>
{
    [Fact]
    public async Task CreateDefinition_WithDefinitionLevelBlackout_PersistsSharedCalendarAndWindows()
    {
        await using var scope = fixture.ServiceProvider.CreateAsyncScope();
        var create = scope.ServiceProvider.GetRequiredService<ICreateService<JobContext>>();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<JobContext>>();
        var definitionId = LyoGuid.CreateCombPostgres();
        var created = await create.CreateAsync<JobDefinitionReq, JobDefinition, JobDefinitionRes>(
            new() {
                Name = $"BlackoutDef-{definitionId:N}",
                Type = "Test",
                WorkerType = "cs",
                Enabled = true,
                CreateBlackoutCalendar =
                    new() {
                        Name = "Maintenance",
                        Enabled = true,
                        CreateBlackoutWindows = [
                            new() {
                                Name = "Nightly",
                                DayFlags = DayFlags.Weekdays,
                                StartTime = TimeOnly.Parse("02:00"),
                                EndTime = TimeOnly.Parse("04:00"),
                                Policy = JobBlackoutPolicy.Skip,
                                Enabled = true
                            }
                        ]
                    },
                CreateSchedules = [
                    new() {
                        Type = ScheduleType.Cron,
                        MonthFlags = MonthFlags.EveryMonth,
                        DayFlags = DayFlags.EveryDay,
                        CronExpression = "0 * * * *",
                        Enabled = true,
                        Description = "hourly"
                    },
                    new() {
                        Type = ScheduleType.Cron,
                        MonthFlags = MonthFlags.EveryMonth,
                        DayFlags = DayFlags.Weekdays,
                        CronExpression = "0 9 * * *",
                        Enabled = true,
                        Description = "weekday-morning"
                    }
                ]
            }, ctx => {
                ctx.Entity.Id = definitionId;
                ctx.Entity.Type = "Test";
                ctx.Entity.WorkerType = "cs";
                foreach (var schedule in ctx.Entity.JobSchedules) {
                    if (schedule.Id == default)
                        schedule.Id = LyoGuid.CreateCombPostgres();

                    schedule.JobDefinitionId = definitionId;
                }

                JobBlackoutCalendarEntityHelper.AssignNestedBlackoutCalendarIds(ctx.Entity);
            }, ct: TestContext.Current.CancellationToken);

        Assert.True(created.IsSuccess, created.Error?.Detail ?? "create failed");
        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var schedules = await db.JobSchedules.AsNoTracking()
            .Include(s => s.JobBlackoutCalendar!)
            .ThenInclude(c => c.JobBlackoutWindows)
            .Where(s => s.JobDefinitionId == definitionId)
            .OrderBy(s => s.Description)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, schedules.Count);
        Assert.All(schedules, s => Assert.NotNull(s.JobBlackoutCalendarId));
        Assert.Equal(schedules[0].JobBlackoutCalendarId, schedules[1].JobBlackoutCalendarId);
        var calendar = schedules[0].JobBlackoutCalendar;
        Assert.NotNull(calendar);
        Assert.Equal("Maintenance", calendar.Name);
        Assert.True(calendar.Enabled);
        Assert.NotEqual(default, calendar.CreatedTimestamp);
        var window = Assert.Single(calendar.JobBlackoutWindows);
        Assert.Equal("Nightly", window.Name);
        Assert.Equal(nameof(DayFlags.Weekdays), window.DayFlags);
        Assert.Equal(nameof(JobBlackoutPolicy.Skip), window.Policy);
        Assert.True(window.Enabled);
        Assert.Equal(calendar.Id, window.JobBlackoutCalendarId);
        Assert.True(TimeOnly.TryParse(window.StartTime, out var start));
        Assert.True(TimeOnly.TryParse(window.EndTime, out var end));
        Assert.Equal(new(2, 0), start);
        Assert.Equal(new(4, 0), end);
        Assert.Equal(1, await db.JobBlackoutCalendars.CountAsync(c => c.Id == calendar.Id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateDefinition_WithScheduleLevelBlackout_PersistsCalendarOnThatSchedule()
    {
        await using var scope = fixture.ServiceProvider.CreateAsyncScope();
        var create = scope.ServiceProvider.GetRequiredService<ICreateService<JobContext>>();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<JobContext>>();
        var definitionId = LyoGuid.CreateCombPostgres();
        var created = await create.CreateAsync<JobDefinitionReq, JobDefinition, JobDefinitionRes>(
            new() {
                Name = $"BlackoutSched-{definitionId:N}",
                Type = "Test",
                WorkerType = "cs",
                Enabled = true,
                CreateSchedules = [
                    new() {
                        Type = ScheduleType.Cron,
                        MonthFlags = MonthFlags.EveryMonth,
                        DayFlags = DayFlags.EveryDay,
                        CronExpression = "0 * * * *",
                        Enabled = true,
                        CreateBlackoutCalendar = new() {
                            Name = "ScheduleBlackout",
                            Enabled = true,
                            CreateBlackoutWindows = [
                                new() {
                                    Name = "Lunch",
                                    DayFlags = DayFlags.EveryDay,
                                    StartTime = TimeOnly.Parse("12:00"),
                                    EndTime = TimeOnly.Parse("13:00"),
                                    Policy = JobBlackoutPolicy.Defer,
                                    Enabled = true
                                }
                            ]
                        }
                    }
                ]
            }, ctx => {
                ctx.Entity.Id = definitionId;
                ctx.Entity.Type = "Test";
                ctx.Entity.WorkerType = "cs";
                foreach (var schedule in ctx.Entity.JobSchedules) {
                    if (schedule.Id == default)
                        schedule.Id = LyoGuid.CreateCombPostgres();

                    schedule.JobDefinitionId = definitionId;
                }

                JobBlackoutCalendarEntityHelper.AssignNestedBlackoutCalendarIds(ctx.Entity);
            }, ct: TestContext.Current.CancellationToken);

        Assert.True(created.IsSuccess, created.Error?.Detail ?? "create failed");
        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var schedule = await db.JobSchedules.AsNoTracking()
            .Include(s => s.JobBlackoutCalendar!)
            .ThenInclude(c => c.JobBlackoutWindows)
            .SingleAsync(s => s.JobDefinitionId == definitionId, TestContext.Current.CancellationToken);

        Assert.NotNull(schedule.JobBlackoutCalendar);
        Assert.Equal("ScheduleBlackout", schedule.JobBlackoutCalendar!.Name);
        var window = Assert.Single(schedule.JobBlackoutCalendar.JobBlackoutWindows);
        Assert.Equal("Lunch", window.Name);
        Assert.Equal(nameof(JobBlackoutPolicy.Defer), window.Policy);
    }
}