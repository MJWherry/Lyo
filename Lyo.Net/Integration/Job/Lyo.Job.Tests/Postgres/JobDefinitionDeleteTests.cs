using Lyo.Api.Services.Crud.Create;
using Lyo.Api.Services.Crud.Delete;
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

public class JobDefinitionDeleteTests(JobPostgresFixture fixture) : IClassFixture<JobPostgresFixture>
{
    [Fact]
    public async Task Delete_WithParametersSchedulesAndRuns_SucceedsWithoutFkViolation()
    {
        await using var scope = fixture.ServiceProvider.CreateAsyncScope();
        var create = scope.ServiceProvider.GetRequiredService<ICreateService<JobContext>>();
        var delete = scope.ServiceProvider.GetRequiredService<IDeleteService<JobContext>>();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<JobContext>>();
        var definitionId = LyoGuid.CreateCombPostgres();
        var created = await create.CreateAsync<JobDefinitionReq, JobDefinition, JobDefinitionRes>(
            new() {
                Name = $"DeleteMe-{definitionId:N}".Length <= 100 ? $"DeleteMe-{definitionId:N}" : $"DeleteMe-{definitionId:N}"[..100],
                Description = "cascade delete test",
                Type = "Test",
                WorkerType = "cs",
                Enabled = true,
                CreateParameters = [
                    new() {
                        Key = "Counties",
                        Type = JobParameterType.String,
                        Value = "Allegheny",
                        Description = "test",
                        Enabled = true
                    }
                ],
                CreateSchedules = [
                    new() {
                        Type = ScheduleType.Interval,
                        MonthFlags = MonthFlags.EveryMonth,
                        DayFlags = DayFlags.EveryDay,
                        IntervalMinutes = 60,
                        StartTime = new TimeOnly(0, 0),
                        EndTime = new TimeOnly(23, 59),
                        Enabled = true,
                        Description = "hourly",
                        CreateScheduleParameters = [
                            new() {
                                Key = "ClientId",
                                Type = JobParameterType.Guid,
                                Value = Guid.NewGuid().ToString("D"),
                                Enabled = true
                            }
                        ]
                    }
                ]
            }, ctx => {
                ctx.Entity.Id = definitionId;
                ctx.Entity.Type = "Test";
                ctx.Entity.WorkerType = "cs";
                foreach (var p in ctx.Entity.JobParameters)
                    p.Id = LyoGuid.CreateCombPostgres();

                foreach (var s in ctx.Entity.JobSchedules) {
                    s.Id = LyoGuid.CreateCombPostgres();
                    foreach (var sp in s.JobScheduleParameters)
                        sp.Id = LyoGuid.CreateCombPostgres();
                }
            }, ct: TestContext.Current.CancellationToken);

        Assert.True(created.IsSuccess, created.Error?.Detail ?? "create failed");
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken)) {
            var scheduleId = await db.JobSchedules.Where(s => s.JobDefinitionId == definitionId).Select(s => s.Id).FirstAsync(TestContext.Current.CancellationToken);
            db.JobRuns.Add(
                new() {
                    Id = LyoGuid.CreateCombPostgres(),
                    JobDefinitionId = definitionId,
                    JobScheduleId = scheduleId,
                    State = JobState.Finished,
                    CreatedBy = "test",
                    CreatedTimestamp = DateTime.UtcNow,
                    AllowTriggers = false
                });

            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using (var verifyDb = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
            Assert.True(await verifyDb.JobParameters.AnyAsync(p => p.JobDefinitionId == definitionId, TestContext.Current.CancellationToken));

        var deleted = await delete.DeleteAsync<JobDefinition, JobDefinitionRes>(
            [definitionId], ctx => JobDefinitionCascadeDelete.RemoveDependents(ctx.DbContext, ctx.Entity.Id), ct: TestContext.Current.CancellationToken);

        Assert.True(deleted.IsSuccess, deleted.Error?.Detail ?? "delete failed");
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken)) {
            Assert.False(await db.JobDefinitions.AnyAsync(d => d.Id == definitionId, TestContext.Current.CancellationToken));
            Assert.False(await db.JobParameters.AnyAsync(p => p.JobDefinitionId == definitionId, TestContext.Current.CancellationToken));
            Assert.False(await db.JobSchedules.AnyAsync(s => s.JobDefinitionId == definitionId, TestContext.Current.CancellationToken));
            Assert.False(await db.JobRuns.AnyAsync(r => r.JobDefinitionId == definitionId, TestContext.Current.CancellationToken));
        }
    }

    [Fact]
    public async Task Delete_WithoutCascade_FailsOnJobParameterFk()
    {
        await using var scope = fixture.ServiceProvider.CreateAsyncScope();
        var create = scope.ServiceProvider.GetRequiredService<ICreateService<JobContext>>();
        var delete = scope.ServiceProvider.GetRequiredService<IDeleteService<JobContext>>();
        var definitionId = LyoGuid.CreateCombPostgres();
        var created = await create.CreateAsync<JobDefinitionReq, JobDefinition, JobDefinitionRes>(
            new() {
                Name = $"NoCascade-{definitionId:N}".Length <= 100 ? $"NoCascade-{definitionId:N}" : $"NoCascade-{definitionId:N}"[..100],
                Type = "Test",
                WorkerType = "cs",
                Enabled = true,
                CreateParameters = [
                    new() {
                        Key = "X",
                        Type = JobParameterType.String,
                        Value = "1",
                        Enabled = true
                    }
                ]
            }, ctx => {
                ctx.Entity.Id = definitionId;
                ctx.Entity.Type = "Test";
                ctx.Entity.WorkerType = "cs";
                foreach (var p in ctx.Entity.JobParameters)
                    p.Id = LyoGuid.CreateCombPostgres();
            }, ct: TestContext.Current.CancellationToken);

        Assert.True(created.IsSuccess, created.Error?.Detail ?? "create failed");

        // Reproduce the production bug: delete definition without removing job_parameter rows first.
        var deleted = await delete.DeleteAsync<JobDefinition, JobDefinitionRes>([definitionId], ct: TestContext.Current.CancellationToken);
        Assert.False(deleted.IsSuccess);
        var detail = $"{deleted.Error?.Detail} {deleted.Error?.Title} {deleted.Error?.Type}";
        Assert.True(
            detail.Contains("job_parameter", StringComparison.OrdinalIgnoreCase) || detail.Contains("23503", StringComparison.OrdinalIgnoreCase) ||
            detail.Contains("foreign key", StringComparison.OrdinalIgnoreCase) || detail.Contains("saving the entity changes", StringComparison.OrdinalIgnoreCase), detail);
    }
}