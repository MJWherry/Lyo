using System.Text.Json;
using Lyo.Common.Core.Enums;
using Lyo.Common.Core.Identifiers;
using Lyo.Common.Metadata.Records;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Request;
using Lyo.Job.Postgres;
using Lyo.Job.Postgres.Database;
using Lyo.Schedule.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Job.Tests.Postgres;

/// <summary>
/// Locks the workflow start path: a start materializes the run and one run step per enabled step, repeats resolve to the first run, and an unusable workflow is rejected
/// before any rows are written.
/// </summary>
public class JobWorkflowStartTests(JobPostgresFixture fixture) : IClassFixture<JobPostgresFixture>
{
    [Fact]
    public async Task StartWorkflowAsync_EnabledSteps_CreatesPendingRunWithOneStepPerEnabledStep()
    {
        var workflowId = await CreateWorkflowAsync(true, [true, true, false]);
        await using var scope = fixture.ServiceProvider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<JobWorkflowService>();
        var result = await service.StartWorkflowAsync(workflowId, new(), TestCt);

        Assert.True(result.IsSuccess, result.Error?.Detail ?? "start failed");
        Assert.Equal(JobWorkflowRunState.Pending, result.Data!.State);

        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<JobContext>>();
        await using var db = await factory.CreateDbContextAsync(TestCt);
        var runSteps = await db.JobWorkflowRunSteps.AsNoTracking().Where(s => s.JobWorkflowRunId == result.Data.Id).ToListAsync(TestCt);

        // The disabled step must not get a run step: a Pending row nothing ever starts would stop the run from finalizing.
        Assert.Equal(2, runSteps.Count);
        Assert.All(runSteps, s => Assert.Equal(JobWorkflowStepState.Pending, s.State));
        Assert.All(runSteps, s => Assert.Null(s.JobRunId));
    }

    [Fact]
    public async Task StartWorkflowAsync_SameIdempotencyKey_ReturnsExistingRun()
    {
        var workflowId = await CreateWorkflowAsync(true, [true]);
        await using var scope = fixture.ServiceProvider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<JobWorkflowService>();
        var key = $"test:{LyoGuid.CreateV7():N}";
        var first = await service.StartWorkflowAsync(workflowId, new() { IdempotencyKey = key }, TestCt);
        var second = await service.StartWorkflowAsync(workflowId, new() { IdempotencyKey = key }, TestCt);

        Assert.True(first.IsSuccess, first.Error?.Detail ?? "first start failed");
        Assert.True(second.IsSuccess, second.Error?.Detail ?? "second start failed");
        Assert.Equal(first.Data!.Id, second.Data!.Id);

        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<JobContext>>();
        await using var db = await factory.CreateDbContextAsync(TestCt);
        Assert.Equal(1, await db.JobWorkflowRuns.CountAsync(r => r.JobWorkflowId == workflowId, TestCt));
    }

    [Fact]
    public async Task StartWorkflowAsync_DuplicateScheduleSlot_ReturnsExistingRun()
    {
        var workflowId = await CreateWorkflowAsync(true, [true]);
        var scheduleId = await CreateWorkflowScheduleAsync(workflowId);
        await using var scope = fixture.ServiceProvider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<JobWorkflowService>();
        var slot = new DateTime(2026, 9, 23, 5, 0, 0, DateTimeKind.Utc);
        var first = await service.StartWorkflowAsync(workflowId, new() { JobScheduleId = scheduleId, ScheduledSlotUtc = slot }, TestCt);
        var second = await service.StartWorkflowAsync(workflowId, new() { JobScheduleId = scheduleId, ScheduledSlotUtc = slot }, TestCt);

        // Two schedulers racing the same cron slot must converge on one run, the way job runs already do through ix_job_run_schedule_slot_unique.
        Assert.True(first.IsSuccess, first.Error?.Detail ?? "first start failed");
        Assert.True(second.IsSuccess, second.Error?.Detail ?? "second start failed");
        Assert.Equal(first.Data!.Id, second.Data!.Id);

        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<JobContext>>();
        await using var db = await factory.CreateDbContextAsync(TestCt);
        Assert.Equal(1, await db.JobWorkflowRuns.CountAsync(r => r.JobWorkflowId == workflowId, TestCt));
    }

    [Fact]
    public async Task StartWorkflowAsync_DisabledWorkflow_ReturnsConflictFailure()
    {
        var workflowId = await CreateWorkflowAsync(false, [true]);
        await using var scope = fixture.ServiceProvider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<JobWorkflowService>();
        var result = await service.StartWorkflowAsync(workflowId, new(), TestCt);

        Assert.False(result.IsSuccess);
        Assert.Contains("disabled", result.Error?.GetFullMessage() ?? "", StringComparison.OrdinalIgnoreCase);

        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<JobContext>>();
        await using var db = await factory.CreateDbContextAsync(TestCt);
        Assert.False(await db.JobWorkflowRuns.AnyAsync(r => r.JobWorkflowId == workflowId, TestCt));
    }

    [Fact]
    public async Task StartWorkflowAsync_WorkflowWithNoEnabledSteps_ReturnsInvalidRequestFailure()
    {
        var workflowId = await CreateWorkflowAsync(true, [false, false]);
        await using var scope = fixture.ServiceProvider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<JobWorkflowService>();
        var result = await service.StartWorkflowAsync(workflowId, new(), TestCt);

        // Without this guard the run would be created with zero steps and sit Pending forever: nothing completes to finalize it.
        Assert.False(result.IsSuccess);
        Assert.Contains("no enabled steps", result.Error?.GetFullMessage() ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task StartWorkflowAsync_UnknownWorkflow_ReturnsNotFoundFailure()
    {
        await using var scope = fixture.ServiceProvider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<JobWorkflowService>();
        var result = await service.StartWorkflowAsync(LyoGuid.CreateV7(), new(), TestCt);

        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Error?.GetFullMessage() ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task StartWorkflowAsync_WithInputs_StoresThemOnTheRun()
    {
        var workflowId = await CreateWorkflowAsync(true, [true]);
        await using var scope = fixture.ServiceProvider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<JobWorkflowService>();
        var result = await service.StartWorkflowAsync(workflowId, new() { Inputs = new Dictionary<string, string> { ["Env"] = "prod" } }, TestCt);

        Assert.True(result.IsSuccess, result.Error?.Detail ?? "start failed");
        Assert.Equal("prod", ReadInput(result.Data!.InputsJson, "Env"));
    }

    [Fact]
    public async Task StartWorkflowAsync_ScheduleParameters_AreOverriddenByCallerInputs()
    {
        var workflowId = await CreateWorkflowAsync(true, [true]);
        var scheduleId = await CreateWorkflowScheduleAsync(workflowId, ("Env", "stage", true), ("Region", "eu", true));
        await using var scope = fixture.ServiceProvider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<JobWorkflowService>();
        var request = new JobWorkflowStartReq {
            JobScheduleId = scheduleId,
            ScheduledSlotUtc = new DateTime(2026, 9, 23, 7, 0, 0, DateTimeKind.Utc),
            Inputs = new Dictionary<string, string> { ["Env"] = "prod" }
        };

        var result = await service.StartWorkflowAsync(workflowId, request, TestCt);

        // The schedule supplies the base, the caller overrides it. Anything else would make a manual override of a scheduled start impossible.
        Assert.True(result.IsSuccess, result.Error?.Detail ?? "start failed");
        Assert.Equal("prod", ReadInput(result.Data!.InputsJson, "Env"));
        Assert.Equal("eu", ReadInput(result.Data.InputsJson, "Region"));
    }

    [Fact]
    public async Task StartWorkflowAsync_DisabledScheduleParameters_AreNotStored()
    {
        var workflowId = await CreateWorkflowAsync(true, [true]);
        var scheduleId = await CreateWorkflowScheduleAsync(workflowId, ("Env", "stage", true), ("Retired", "old", false));
        await using var scope = fixture.ServiceProvider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<JobWorkflowService>();
        var request = new JobWorkflowStartReq { JobScheduleId = scheduleId, ScheduledSlotUtc = new DateTime(2026, 9, 23, 8, 0, 0, DateTimeKind.Utc) };

        var result = await service.StartWorkflowAsync(workflowId, request, TestCt);

        // A disabled parameter is excluded from a scheduled job run, so a branch must not see it either.
        Assert.True(result.IsSuccess, result.Error?.Detail ?? "start failed");
        Assert.Equal("stage", ReadInput(result.Data!.InputsJson, "Env"));
        Assert.Null(ReadInput(result.Data.InputsJson, "Retired"));
    }

    private static string? ReadInput(string? inputsJson, string key)
        => inputsJson is null ? null : JsonSerializer.Deserialize<Dictionary<string, string>>(inputsJson)?.GetValueOrDefault(key);

    /// <summary>Persists a workflow-owned hourly schedule. The run's schedule FK is real, so the slot unique index is what the test exercises rather than a dangling id.</summary>
    private async Task<Guid> CreateWorkflowScheduleAsync(Guid workflowId, params (string Key, string Value, bool Enabled)[] parameters)
    {
        await using var scope = fixture.ServiceProvider.CreateAsyncScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<JobContext>>();
        await using var db = await factory.CreateDbContextAsync(TestCt);
        var scheduleId = LyoGuid.CreateCombPostgres();
        db.JobSchedules.Add(
            new() {
                Id = scheduleId,
                JobWorkflowId = workflowId,
                Type = nameof(ScheduleType.Cron),
                CronExpression = "0 * * * *",
                MonthFlags = nameof(MonthFlags.EveryMonth),
                DayFlags = nameof(DayFlags.EveryDay),
                MisfirePolicy = nameof(JobMisfirePolicy.Skip),
                Enabled = true,
                CreatedTimestamp = DateTime.UtcNow
            });

        foreach (var parameter in parameters) {
            db.JobScheduleParameters.Add(
                new() {
                    Id = LyoGuid.CreateCombPostgres(),
                    JobScheduleId = scheduleId,
                    Key = parameter.Key,
                    Type = LyoTypeInfo.String.FullName,
                    Value = parameter.Value,
                    Enabled = parameter.Enabled,
                    CreatedTimestamp = DateTime.UtcNow
                });
        }

        await db.SaveChangesAsync(TestCt);
        return scheduleId;
    }

    private async Task<Guid> CreateWorkflowAsync(bool enabled, IReadOnlyList<bool> stepEnabledFlags)
    {
        await using var scope = fixture.ServiceProvider.CreateAsyncScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<JobContext>>();
        await using var db = await factory.CreateDbContextAsync(TestCt);
        var workflowId = LyoGuid.CreateCombPostgres();
        db.JobWorkflows.Add(
            new() {
                Id = workflowId,
                Name = $"wf-{workflowId:N}",
                Enabled = enabled,
                CreatedTimestamp = DateTime.UtcNow
            });

        for (var i = 0; i < stepEnabledFlags.Count; i++) {
            db.JobWorkflowSteps.Add(
                new() {
                    Id = LyoGuid.CreateCombPostgres(),
                    JobWorkflowId = workflowId,
                    JobDefinitionId = fixture.JobDefinitionId,
                    StepName = $"step-{i}",
                    StepOrder = i,
                    FailurePolicy = nameof(JobWorkflowFailurePolicy.Stop),
                    Enabled = stepEnabledFlags[i],
                    CreatedTimestamp = DateTime.UtcNow
                });
        }

        await db.SaveChangesAsync(TestCt);
        return workflowId;
    }
}
