using Lyo.Api.ApiEndpoint.Config;
using Lyo.Api.Services.Crud.Create;
using Lyo.Api.Services.Crud.Delete;
using Lyo.Common.Enums;
using Lyo.Common.Identifiers;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Request;
using Lyo.Job.Models.Response;
using Lyo.Job.Postgres;
using Lyo.Job.Postgres.Database;
using Lyo.Query.Models.Enums;
using Lyo.Schedule.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Job.Tests.Postgres;

public class JobCascadeAndCreateTests(JobPostgresFixture fixture) : IClassFixture<JobPostgresFixture>
{
    [Fact]
    public async Task CreateDefinition_StampsCreatedTimestamps_OnNestedEntities()
    {
        await using var scope = fixture.ServiceProvider.CreateAsyncScope();
        var create = scope.ServiceProvider.GetRequiredService<ICreateService<JobContext>>();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<JobContext>>();
        var definitionId = LyoGuid.CreateCombPostgres();
        var created = await create.CreateAsync<JobDefinitionReq, JobDefinition, JobDefinitionRes>(
            BuildDefinitionReq(definitionId, true, false), ApplyDefinitionIds(definitionId), ct: TestContext.Current.CancellationToken);

        Assert.True(created.IsSuccess, created.Error?.Detail ?? "create failed");
        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var def = await db.JobDefinitions.AsNoTracking().SingleAsync(d => d.Id == definitionId, TestContext.Current.CancellationToken);
        Assert.NotEqual(default, def.CreatedTimestamp);
        var param = await db.JobParameters.AsNoTracking().SingleAsync(p => p.JobDefinitionId == definitionId, TestContext.Current.CancellationToken);
        Assert.NotEqual(default, param.CreatedTimestamp);
        var schedule = await db.JobSchedules.AsNoTracking()
            .Include(s => s.JobScheduleParameters)
            .Include(s => s.JobBlackoutCalendar!)
            .ThenInclude(c => c.JobBlackoutWindows)
            .SingleAsync(s => s.JobDefinitionId == definitionId, TestContext.Current.CancellationToken);

        Assert.NotEqual(default, schedule.CreatedTimestamp);
        Assert.All(schedule.JobScheduleParameters, p => Assert.NotEqual(default, p.CreatedTimestamp));
        Assert.NotNull(schedule.JobBlackoutCalendar);
        Assert.NotEqual(default, schedule.JobBlackoutCalendar!.CreatedTimestamp);
        Assert.All(schedule.JobBlackoutCalendar.JobBlackoutWindows, w => Assert.NotEqual(default, w.CreatedTimestamp));
    }

    [Fact]
    public async Task CreateDefinition_WithJsonStyleSharedBlackout_PersistsOneCalendarRow()
    {
        await using var scope = fixture.ServiceProvider.CreateAsyncScope();
        var create = scope.ServiceProvider.GetRequiredService<ICreateService<JobContext>>();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<JobContext>>();
        var definitionId = LyoGuid.CreateCombPostgres();
        var calendarA = new JobBlackoutCalendarReq {
            Name = "Shared",
            CreateBlackoutWindows = [
                new() {
                    Name = "W",
                    DayFlags = DayFlags.Weekdays,
                    StartTime = TimeOnly.Parse("01:00"),
                    EndTime = TimeOnly.Parse("02:00")
                }
            ]
        };

        // Simulate JSON round-trip: separate instances, same content.
        var calendarB = new JobBlackoutCalendarReq {
            Name = "Shared",
            CreateBlackoutWindows = [
                new() {
                    Name = "W",
                    DayFlags = DayFlags.Weekdays,
                    StartTime = TimeOnly.Parse("01:00"),
                    EndTime = TimeOnly.Parse("02:00")
                }
            ]
        };

        var created = await create.CreateAsync<JobDefinitionReq, JobDefinition, JobDefinitionRes>(
            new() {
                Name = $"JsonBlackout-{definitionId:N}".Length <= 100 ? $"JsonBlackout-{definitionId:N}" : $"JsonBlackout-{definitionId:N}"[..100],
                Type = "Test",
                WorkerType = "cs",
                Enabled = true,
                CreateBlackoutCalendar = calendarA,
                CreateSchedules = [
                    new() {
                        Type = ScheduleType.Cron,
                        MonthFlags = MonthFlags.EveryMonth,
                        DayFlags = DayFlags.EveryDay,
                        CronExpression = "0 * * * *",
                        Enabled = true,
                        CreateBlackoutCalendar = calendarA
                    },
                    new() {
                        Type = ScheduleType.Cron,
                        MonthFlags = MonthFlags.EveryMonth,
                        DayFlags = DayFlags.Weekdays,
                        CronExpression = "0 9 * * *",
                        Enabled = true,
                        CreateBlackoutCalendar = calendarB
                    }
                ]
            }, ctx => {
                ctx.Entity.Id = definitionId;
                foreach (var schedule in ctx.Entity.JobSchedules) {
                    if (schedule.Id == default)
                        schedule.Id = LyoGuid.CreateCombPostgres();

                    schedule.JobDefinitionId = definitionId;
                }

                JobBlackoutCalendarEntityHelper.AssignNestedBlackoutCalendarIds(ctx.Entity);
            }, ct: TestContext.Current.CancellationToken);

        Assert.True(created.IsSuccess, created.Error?.Detail ?? "create failed");
        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var calendarIds = await db.JobSchedules.AsNoTracking()
            .Where(s => s.JobDefinitionId == definitionId)
            .Select(s => s.JobBlackoutCalendarId)
            .Distinct()
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Single(calendarIds);
        Assert.NotNull(calendarIds[0]);
        Assert.Equal(1, await db.JobBlackoutCalendars.CountAsync(c => c.Id == calendarIds[0], TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteDefinition_GcsUnusedBlackoutCalendar_LeavesShared()
    {
        await using var scope = fixture.ServiceProvider.CreateAsyncScope();
        var create = scope.ServiceProvider.GetRequiredService<ICreateService<JobContext>>();
        var delete = scope.ServiceProvider.GetRequiredService<IDeleteService<JobContext>>();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<JobContext>>();
        var sharedCalendarId = LyoGuid.CreateCombPostgres();
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken)) {
            db.JobBlackoutCalendars.Add(
                new() {
                    Id = sharedCalendarId,
                    Name = "SharedCal",
                    Enabled = true,
                    CreatedTimestamp = DateTime.UtcNow
                });

            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var exclusiveDefId = LyoGuid.CreateCombPostgres();
        var sharedDefId = LyoGuid.CreateCombPostgres();
        Assert.True(
            (await create.CreateAsync<JobDefinitionReq, JobDefinition, JobDefinitionRes>(
                BuildDefinitionReq(exclusiveDefId, true, false), ApplyDefinitionIds(exclusiveDefId), ct: TestContext.Current.CancellationToken)).IsSuccess);

        Assert.True(
            (await create.CreateAsync<JobDefinitionReq, JobDefinition, JobDefinitionRes>(
                new() {
                    Name = $"SharedDef-{sharedDefId:N}".Length <= 100 ? $"SharedDef-{sharedDefId:N}" : $"SharedDef-{sharedDefId:N}"[..100],
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
                            JobBlackoutCalendarId = sharedCalendarId
                        }
                    ]
                }, ApplyDefinitionIds(sharedDefId), ct: TestContext.Current.CancellationToken)).IsSuccess);

        Guid exclusiveCalendarId;
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken)) {
            exclusiveCalendarId = await db.JobSchedules.Where(s => s.JobDefinitionId == exclusiveDefId)
                .Select(s => s.JobBlackoutCalendarId!.Value)
                .SingleAsync(TestContext.Current.CancellationToken);
        }

        Assert.True(
            (await delete.DeleteAsync<JobDefinition, JobDefinitionRes>(
                [exclusiveDefId], ctx => JobDefinitionCascadeDelete.RemoveDependents(ctx.DbContext, ctx.Entity.Id), ct: TestContext.Current.CancellationToken)).IsSuccess);

        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken)) {
            Assert.False(await db.JobBlackoutCalendars.AnyAsync(c => c.Id == exclusiveCalendarId, TestContext.Current.CancellationToken));
            Assert.True(await db.JobBlackoutCalendars.AnyAsync(c => c.Id == sharedCalendarId, TestContext.Current.CancellationToken));
        }

        Assert.True(
            (await delete.DeleteAsync<JobDefinition, JobDefinitionRes>(
                [sharedDefId], ctx => JobDefinitionCascadeDelete.RemoveDependents(ctx.DbContext, ctx.Entity.Id), ct: TestContext.Current.CancellationToken)).IsSuccess);

        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
            Assert.False(await db.JobBlackoutCalendars.AnyAsync(c => c.Id == sharedCalendarId, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteSchedule_RemovesParams_AndExclusiveCalendar_KeepsShared()
    {
        await using var scope = fixture.ServiceProvider.CreateAsyncScope();
        var create = scope.ServiceProvider.GetRequiredService<ICreateService<JobContext>>();
        var delete = scope.ServiceProvider.GetRequiredService<IDeleteService<JobContext>>();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<JobContext>>();
        var definitionId = LyoGuid.CreateCombPostgres();
        var sharedCalendar = new JobBlackoutCalendarReq {
            Name = "Shared",
            CreateBlackoutWindows = [
                new() {
                    Name = "W",
                    DayFlags = DayFlags.EveryDay,
                    StartTime = TimeOnly.Parse("00:00"),
                    EndTime = TimeOnly.Parse("01:00")
                }
            ]
        };

        Assert.True(
            (await create.CreateAsync<JobDefinitionReq, JobDefinition, JobDefinitionRes>(
                new() {
                    Name = $"SchedDel-{definitionId:N}".Length <= 100 ? $"SchedDel-{definitionId:N}" : $"SchedDel-{definitionId:N}"[..100],
                    Type = "Test",
                    WorkerType = "cs",
                    Enabled = true,
                    CreateBlackoutCalendar = sharedCalendar,
                    CreateSchedules = [
                        new() {
                            Type = ScheduleType.Cron,
                            MonthFlags = MonthFlags.EveryMonth,
                            DayFlags = DayFlags.EveryDay,
                            CronExpression = "0 * * * *",
                            Enabled = true,
                            Description = "keep",
                            CreateBlackoutCalendar = sharedCalendar,
                            CreateScheduleParameters = [
                                new() {
                                    Key = "Keep",
                                    Type = JobParameterType.String,
                                    Value = "1",
                                    Enabled = true
                                }
                            ]
                        },
                        new() {
                            Type = ScheduleType.Cron,
                            MonthFlags = MonthFlags.EveryMonth,
                            DayFlags = DayFlags.Weekdays,
                            CronExpression = "0 9 * * *",
                            Enabled = true,
                            Description = "drop",
                            CreateBlackoutCalendar = new() {
                                Name = "Exclusive",
                                CreateBlackoutWindows = [
                                    new() {
                                        Name = "X",
                                        DayFlags = DayFlags.Weekdays,
                                        StartTime = TimeOnly.Parse("03:00"),
                                        EndTime = TimeOnly.Parse("04:00")
                                    }
                                ]
                            },
                            CreateScheduleParameters = [
                                new() {
                                    Key = "Drop",
                                    Type = JobParameterType.String,
                                    Value = "2",
                                    Enabled = true
                                }
                            ]
                        }
                    ]
                }, ApplyDefinitionIds(definitionId), ct: TestContext.Current.CancellationToken)).IsSuccess);

        Guid dropScheduleId, exclusiveCalendarId, sharedCalendarId;
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken)) {
            var schedules = await db.JobSchedules.AsNoTracking().Where(s => s.JobDefinitionId == definitionId).ToListAsync(TestContext.Current.CancellationToken);
            var drop = schedules.Single(s => s.Description == "drop");
            var keep = schedules.Single(s => s.Description == "keep");
            dropScheduleId = drop.Id;
            exclusiveCalendarId = drop.JobBlackoutCalendarId!.Value;
            sharedCalendarId = keep.JobBlackoutCalendarId!.Value;
            Assert.NotEqual(exclusiveCalendarId, sharedCalendarId);
            db.JobRuns.Add(
                new() {
                    Id = LyoGuid.CreateCombPostgres(),
                    JobDefinitionId = definitionId,
                    JobScheduleId = dropScheduleId,
                    State = JobState.Finished,
                    CreatedBy = "test",
                    CreatedTimestamp = DateTime.UtcNow,
                    AllowTriggers = false
                });

            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        Assert.True(
            (await delete.DeleteAsync<JobSchedule, JobScheduleRes>(
                [dropScheduleId], ctx => {
                    // Mirror BuildJobGroup schedule BeforeDelete
                    var db = ctx.DbContext;
                    db.JobScheduleParameters.RemoveRange(db.JobScheduleParameters.Where(p => p.JobScheduleId == ctx.Entity.Id).ToList());
                    foreach (var run in db.JobRuns.Where(r => r.JobScheduleId == ctx.Entity.Id).ToList())
                        run.JobScheduleId = null;

                    if (ctx.Entity.JobBlackoutCalendarId is { } calendarId && !db.JobSchedules.Any(s => s.Id != ctx.Entity.Id && s.JobBlackoutCalendarId == calendarId)) {
                        db.JobBlackoutWindows.RemoveRange(db.JobBlackoutWindows.Where(w => w.JobBlackoutCalendarId == calendarId).ToList());
                        var calendar = db.JobBlackoutCalendars.Find(calendarId);
                        if (calendar is not null)
                            db.JobBlackoutCalendars.Remove(calendar);
                    }
                }, ct: TestContext.Current.CancellationToken)).IsSuccess);

        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken)) {
            Assert.False(await db.JobSchedules.AnyAsync(s => s.Id == dropScheduleId, TestContext.Current.CancellationToken));
            Assert.False(await db.JobScheduleParameters.AnyAsync(p => p.Key == "Drop", TestContext.Current.CancellationToken));
            Assert.False(await db.JobBlackoutCalendars.AnyAsync(c => c.Id == exclusiveCalendarId, TestContext.Current.CancellationToken));
            Assert.True(await db.JobBlackoutCalendars.AnyAsync(c => c.Id == sharedCalendarId, TestContext.Current.CancellationToken));
            Assert.True(await db.JobRuns.AnyAsync(r => r.JobDefinitionId == definitionId && r.JobScheduleId == null, TestContext.Current.CancellationToken));
        }
    }

    [Fact]
    public async Task DeleteBlackoutCalendar_WhileReferenced_Fails()
    {
        await using var scope = fixture.ServiceProvider.CreateAsyncScope();
        var create = scope.ServiceProvider.GetRequiredService<ICreateService<JobContext>>();
        var delete = scope.ServiceProvider.GetRequiredService<IDeleteService<JobContext>>();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<JobContext>>();
        var definitionId = LyoGuid.CreateCombPostgres();
        Assert.True(
            (await create.CreateAsync<JobDefinitionReq, JobDefinition, JobDefinitionRes>(
                BuildDefinitionReq(definitionId, true, false), ApplyDefinitionIds(definitionId), ct: TestContext.Current.CancellationToken)).IsSuccess);

        Guid calendarId;
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken)) {
            calendarId = await db.JobSchedules.Where(s => s.JobDefinitionId == definitionId)
                .Select(s => s.JobBlackoutCalendarId!.Value)
                .SingleAsync(TestContext.Current.CancellationToken);
        }

        var deleted = await delete.DeleteAsync<JobBlackoutCalendar, JobBlackoutCalendarRes>(
            [calendarId], ctx => {
                if (ctx.DbContext.JobSchedules.Any(s => s.JobBlackoutCalendarId == ctx.Entity.Id))
                    throw new InvalidOperationException($"Cannot delete blackout calendar '{ctx.Entity.Name}' ({ctx.Entity.Id}) while schedules still reference it.");
            }, ct: TestContext.Current.CancellationToken);

        Assert.False(deleted.IsSuccess);
        Assert.Contains("still reference", deleted.Error?.Detail ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteDefinition_WithWorkflowRunSteps_SucceedsWithoutFkViolation()
    {
        await using var scope = fixture.ServiceProvider.CreateAsyncScope();
        var create = scope.ServiceProvider.GetRequiredService<ICreateService<JobContext>>();
        var delete = scope.ServiceProvider.GetRequiredService<IDeleteService<JobContext>>();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<JobContext>>();
        var definitionId = LyoGuid.CreateCombPostgres();
        Assert.True(
            (await create.CreateAsync<JobDefinitionReq, JobDefinition, JobDefinitionRes>(
                BuildDefinitionReq(definitionId, false, false), ApplyDefinitionIds(definitionId), ct: TestContext.Current.CancellationToken)).IsSuccess);

        var workflowId = LyoGuid.CreateCombPostgres();
        var stepId = LyoGuid.CreateCombPostgres();
        var runId = LyoGuid.CreateCombPostgres();
        var workflowRunId = LyoGuid.CreateCombPostgres();
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken)) {
            db.JobWorkflows.Add(
                new() {
                    Id = workflowId,
                    Name = "wf",
                    Enabled = true,
                    CreatedTimestamp = DateTime.UtcNow
                });

            db.JobWorkflowSteps.Add(
                new() {
                    Id = stepId,
                    JobWorkflowId = workflowId,
                    JobDefinitionId = definitionId,
                    StepName = "s1",
                    StepOrder = 1,
                    FailurePolicy = nameof(JobWorkflowFailurePolicy.Stop),
                    Enabled = true,
                    CreatedTimestamp = DateTime.UtcNow
                });

            db.JobRuns.Add(
                new() {
                    Id = runId,
                    JobDefinitionId = definitionId,
                    State = JobState.Finished,
                    CreatedBy = "test",
                    CreatedTimestamp = DateTime.UtcNow,
                    AllowTriggers = false
                });

            db.JobWorkflowRuns.Add(
                new() {
                    Id = workflowRunId,
                    JobWorkflowId = workflowId,
                    State = JobWorkflowRunState.Finished,
                    CreatedTimestamp = DateTime.UtcNow
                });

            db.JobWorkflowRunSteps.Add(
                new() {
                    Id = LyoGuid.CreateCombPostgres(),
                    JobWorkflowRunId = workflowRunId,
                    JobWorkflowStepId = stepId,
                    JobRunId = runId,
                    State = JobWorkflowStepState.Finished,
                    CreatedTimestamp = DateTime.UtcNow
                });

            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var deleted = await delete.DeleteAsync<JobDefinition, JobDefinitionRes>(
            [definitionId], ctx => JobDefinitionCascadeDelete.RemoveDependents(ctx.DbContext, ctx.Entity.Id), ct: TestContext.Current.CancellationToken);

        Assert.True(deleted.IsSuccess, deleted.Error?.Detail ?? "delete failed");
    }

    [Fact]
    public async Task DeleteTrigger_WithParamsAndRunRefs_SucceedsWithoutFkViolation()
    {
        await using var scope = fixture.ServiceProvider.CreateAsyncScope();
        var create = scope.ServiceProvider.GetRequiredService<ICreateService<JobContext>>();
        var delete = scope.ServiceProvider.GetRequiredService<IDeleteService<JobContext>>();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<JobContext>>();
        var sourceDefId = LyoGuid.CreateCombPostgres();
        var targetDefId = LyoGuid.CreateCombPostgres();
        Assert.True(
            (await create.CreateAsync<JobDefinitionReq, JobDefinition, JobDefinitionRes>(
                new() {
                    Name = $"Src-{sourceDefId:N}".Length <= 100 ? $"Src-{sourceDefId:N}" : $"Src-{sourceDefId:N}"[..100],
                    Type = "Test",
                    WorkerType = "cs",
                    Enabled = true
                }, ApplyDefinitionIds(sourceDefId), ct: TestContext.Current.CancellationToken)).IsSuccess);

        Assert.True(
            (await create.CreateAsync<JobDefinitionReq, JobDefinition, JobDefinitionRes>(
                new() {
                    Name = $"Tgt-{targetDefId:N}".Length <= 100 ? $"Tgt-{targetDefId:N}" : $"Tgt-{targetDefId:N}"[..100],
                    Type = "Test",
                    WorkerType = "cs",
                    Enabled = true,
                    CreateTriggers = [
                        new() {
                            TriggersJobDefinitionId = sourceDefId,
                            JobResultKey = "Result",
                            Comparison = ComparisonOperatorEnum.Equals,
                            JobResultValue = "Ok",
                            Enabled = true,
                            CreateTriggerParameters = [
                                new() {
                                    Key = "P",
                                    Type = JobParameterType.String,
                                    Value = "v",
                                    Enabled = true
                                }
                            ]
                        }
                    ]
                }, ApplyDefinitionIds(targetDefId), ct: TestContext.Current.CancellationToken)).IsSuccess);

        Guid triggerId;
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken)) {
            triggerId = await db.JobTriggers.Where(t => t.JobDefinitionId == targetDefId).Select(t => t.Id).SingleAsync(TestContext.Current.CancellationToken);
            db.JobRuns.Add(
                new() {
                    Id = LyoGuid.CreateCombPostgres(),
                    JobDefinitionId = targetDefId,
                    JobTriggerId = triggerId,
                    State = JobState.Finished,
                    CreatedBy = "test",
                    CreatedTimestamp = DateTime.UtcNow,
                    AllowTriggers = false
                });

            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var deleted = await delete.DeleteAsync<JobTrigger, JobTriggerRes>(
            [triggerId], ctx => {
                var db = ctx.DbContext;
                db.JobTriggerParameters.RemoveRange(db.JobTriggerParameters.Where(p => p.JobTriggerId == ctx.Entity.Id).ToList());
                foreach (var run in db.JobRuns.Where(r => r.JobTriggerId == ctx.Entity.Id).ToList())
                    run.JobTriggerId = null;
            }, ct: TestContext.Current.CancellationToken);

        Assert.True(deleted.IsSuccess, deleted.Error?.Detail ?? "delete failed");
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken)) {
            Assert.False(await db.JobTriggers.AnyAsync(t => t.Id == triggerId, TestContext.Current.CancellationToken));
            Assert.False(await db.JobTriggerParameters.AnyAsync(p => p.JobTriggerId == triggerId, TestContext.Current.CancellationToken));
            Assert.True(await db.JobRuns.AnyAsync(r => r.JobDefinitionId == targetDefId && r.JobTriggerId == null, TestContext.Current.CancellationToken));
        }
    }

    private static JobDefinitionReq BuildDefinitionReq(Guid definitionId, bool withBlackout, bool withTrigger)
        => new() {
            Name = $"Def-{definitionId:N}".Length <= 100 ? $"Def-{definitionId:N}" : $"Def-{definitionId:N}"[..100],
            Type = "Test",
            WorkerType = "cs",
            Enabled = true,
            CreateParameters = [
                new() {
                    Key = "Counties",
                    Type = JobParameterType.String,
                    Value = "Allegheny",
                    Enabled = true
                }
            ],
            CreateBlackoutCalendar =
                withBlackout
                    ? new JobBlackoutCalendarReq {
                        Name = "Maint",
                        CreateBlackoutWindows = [
                            new() {
                                Name = "Night",
                                DayFlags = DayFlags.Weekdays,
                                StartTime = TimeOnly.Parse("02:00"),
                                EndTime = TimeOnly.Parse("04:00")
                            }
                        ]
                    }
                    : null,
            CreateSchedules = [
                new() {
                    Type = ScheduleType.Interval,
                    MonthFlags = MonthFlags.EveryMonth,
                    DayFlags = DayFlags.EveryDay,
                    IntervalMinutes = 60,
                    StartTime = new TimeOnly(0, 0),
                    EndTime = new TimeOnly(23, 59),
                    Enabled = true,
                    CreateScheduleParameters = [
                        new() {
                            Key = "ClientId",
                            Type = JobParameterType.Guid,
                            Value = Guid.NewGuid().ToString("D"),
                            Enabled = true
                        }
                    ]
                }
            ],
            CreateTriggers = withTrigger
                ? [
                    new() {
                        TriggersJobDefinitionId = Guid.NewGuid(),
                        JobResultKey = "Result",
                        Comparison = ComparisonOperatorEnum.Equals,
                        Enabled = true
                    }
                ]
                : []
        };

    private static Action<CreateContext<JobDefinitionReq, JobDefinition, JobContext>> ApplyDefinitionIds(Guid definitionId)
        => ctx => {
            ctx.Entity.Id = definitionId;
            ctx.Entity.Type = "Test";
            ctx.Entity.WorkerType = "cs";
            foreach (var p in ctx.Entity.JobParameters) {
                if (p.Id == default)
                    p.Id = LyoGuid.CreateCombPostgres();

                p.JobDefinitionId = definitionId;
            }

            foreach (var s in ctx.Entity.JobSchedules) {
                if (s.Id == default)
                    s.Id = LyoGuid.CreateCombPostgres();

                s.JobDefinitionId = definitionId;
                foreach (var sp in s.JobScheduleParameters) {
                    if (sp.Id == default)
                        sp.Id = LyoGuid.CreateCombPostgres();

                    sp.JobScheduleId = s.Id;
                }
            }

            foreach (var t in ctx.Entity.JobTriggerJobDefinitions) {
                if (t.Id == default)
                    t.Id = LyoGuid.CreateCombPostgres();

                t.JobDefinitionId = definitionId;
                foreach (var tp in t.JobTriggerParameters) {
                    if (tp.Id == default)
                        tp.Id = LyoGuid.CreateCombPostgres();

                    tp.JobTriggerId = t.Id;
                }
            }

            JobBlackoutCalendarEntityHelper.AssignNestedBlackoutCalendarIds(ctx.Entity);
        };
}