using System.Reflection;
using Lyo.Common.Core.Identifiers;
using Lyo.Job.Models.Enums;
using Lyo.Job.Postgres;
using Lyo.Job.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Job.Tests.Postgres;

/// <summary>
/// Workflow runs are not covered by job-run retention: they outlive the runs they orchestrated, so without their own purge they accumulate forever. These tests pin the two
/// halves of that — a settled run past the cutoff goes away with its steps, and an in-flight run is never touched no matter how old it is.
/// </summary>
[Trait("Category", "Integration")]
[Collection(JobMaintenanceCollection.Name)]
public class JobWorkflowRetentionPurgeTests
{
    private readonly JobPostgresFixture _fixture;

    public JobWorkflowRetentionPurgeTests(JobPostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Purge_FinishedRunOlderThanRetention_DeletesRunAndSteps()
    {
        var seeded = await SeedWorkflowRunAsync(JobWorkflowRunState.Finished, DateTime.UtcNow.AddDays(-10));
        await InvokeMaintenanceAsync(1);
        await using var db = await CreateDbContextAsync();
        Assert.False(await db.JobWorkflowRuns.AnyAsync(r => r.Id == seeded.RunId, TestCt));
        Assert.False(await db.JobWorkflowRunSteps.AnyAsync(s => s.Id == seeded.RunStepId, TestCt));
    }

    [Fact]
    public async Task Purge_FailedRunOlderThanRetention_IsAlsoDeleted()
    {
        var seeded = await SeedWorkflowRunAsync(JobWorkflowRunState.Failed, DateTime.UtcNow.AddDays(-10));
        await InvokeMaintenanceAsync(1);
        await using var db = await CreateDbContextAsync();
        Assert.False(await db.JobWorkflowRuns.AnyAsync(r => r.Id == seeded.RunId, TestCt));
    }

    [Fact]
    public async Task Purge_RunningRun_IsNotPurged()
    {
        // Age is measured from FinishedTimestamp, so an in-flight run has no cutoff to be past. Deleting one would orphan the job runs still reporting into it.
        var seeded = await SeedWorkflowRunAsync(JobWorkflowRunState.Running, null, DateTime.UtcNow.AddDays(-30));
        await InvokeMaintenanceAsync(1);
        await using var db = await CreateDbContextAsync();
        Assert.True(await db.JobWorkflowRuns.AnyAsync(r => r.Id == seeded.RunId, TestCt));
    }

    [Fact]
    public async Task Purge_RetentionDisabled_KeepsEverything()
    {
        var seeded = await SeedWorkflowRunAsync(JobWorkflowRunState.Finished, DateTime.UtcNow.AddDays(-999));
        await InvokeMaintenanceAsync(0);
        await using var db = await CreateDbContextAsync();
        Assert.True(await db.JobWorkflowRuns.AnyAsync(r => r.Id == seeded.RunId, TestCt));
    }

    [Fact]
    public async Task Purge_FinishedRunInsideRetention_IsKept()
    {
        var seeded = await SeedWorkflowRunAsync(JobWorkflowRunState.Finished, DateTime.UtcNow.AddHours(-2));
        await InvokeMaintenanceAsync(7);
        await using var db = await CreateDbContextAsync();
        Assert.True(await db.JobWorkflowRuns.AnyAsync(r => r.Id == seeded.RunId, TestCt));
    }

    private async Task<(Guid RunId, Guid RunStepId)> SeedWorkflowRunAsync(JobWorkflowRunState state, DateTime? finishedTimestamp, DateTime? createdTimestamp = null)
    {
        var definitionId = LyoGuid.CreateCombPostgres();
        var workflowId = LyoGuid.CreateCombPostgres();
        var stepId = LyoGuid.CreateCombPostgres();
        var runId = LyoGuid.CreateCombPostgres();
        var runStepId = LyoGuid.CreateCombPostgres();
        await using var db = await CreateDbContextAsync();
        db.JobDefinitions.Add(
            new() {
                Id = definitionId,
                Name = $"WfPurge-{definitionId:N}"[..24],
                Type = "Test",
                WorkerType = "cs",
                Enabled = true,
                CreatedTimestamp = DateTime.UtcNow
            });

        db.JobWorkflows.Add(
            new() {
                Id = workflowId,
                Name = $"wf-{workflowId:N}"[..20],
                Enabled = true,
                CreatedTimestamp = DateTime.UtcNow
            });

        db.JobWorkflowSteps.Add(
            new() {
                Id = stepId,
                JobWorkflowId = workflowId,
                JobDefinitionId = definitionId,
                StepName = "s1",
                StepOrder = 0,
                FailurePolicy = nameof(JobWorkflowFailurePolicy.Stop),
                Enabled = true,
                CreatedTimestamp = DateTime.UtcNow
            });

        db.JobWorkflowRuns.Add(
            new() {
                Id = runId,
                JobWorkflowId = workflowId,
                State = state,
                CreatedTimestamp = createdTimestamp ?? finishedTimestamp ?? DateTime.UtcNow,
                FinishedTimestamp = finishedTimestamp
            });

        db.JobWorkflowRunSteps.Add(
            new() {
                Id = runStepId,
                JobWorkflowRunId = runId,
                JobWorkflowStepId = stepId,
                State = state == JobWorkflowRunState.Running ? JobWorkflowStepState.Running : JobWorkflowStepState.Finished,
                CreatedTimestamp = DateTime.UtcNow
            });

        await db.SaveChangesAsync(TestCt);
        return (runId, runStepId);
    }

    private async Task InvokeMaintenanceAsync(int workflowRetentionDays)
    {
        using var scope = _fixture.ServiceProvider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<JobContext>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<JobMaintenanceService>>();
        var options = new JobMaintenanceOptions { WorkflowRetentionDays = workflowRetentionDays };
        var maintenance = new JobMaintenanceService(factory, logger, _fixture.FakePublisher, options);
        var method = typeof(JobMaintenanceService).GetMethod("RunMaintenanceAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        await (Task)method.Invoke(maintenance, [TestCt])!;
    }

    private async Task<JobContext> CreateDbContextAsync()
    {
        using var scope = _fixture.ServiceProvider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<JobContext>>();
        return await factory.CreateDbContextAsync(TestCt);
    }
}