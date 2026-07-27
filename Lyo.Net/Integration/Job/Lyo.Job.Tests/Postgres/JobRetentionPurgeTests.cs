using System.Reflection;
using Lyo.Common.Identifiers;
using Lyo.Job.Models.Enums;
using Lyo.Job.Postgres;
using Lyo.Job.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Job.Tests.Postgres;

/// <summary>
/// Regression tests for retention-purge FK handling: purging a run referenced by a workflow run step, or a parent run whose children survive the batch, must not violate
/// foreign keys (which would permanently wedge the purge for that definition).
/// </summary>
[Trait("Category", "Integration")]
[Collection(JobMaintenanceCollection.Name)]
public class JobRetentionPurgeTests
{
    private readonly JobPostgresFixture _fixture;

    public JobRetentionPurgeTests(JobPostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Purge_RunReferencedByWorkflowStep_DetachesStepAndDeletesRun()
    {
        var definitionId = await CreateDefinitionAsync(retentionDays: 1);
        var runId = await SeedExpiredRunAsync(definitionId);
        var stepRunId = await SeedWorkflowRunStepAsync(definitionId, runId);

        await InvokeMaintenanceAsync();

        await using var db = await CreateDbContextAsync();
        Assert.False(await db.JobRuns.AnyAsync(r => r.Id == runId, TestContext.Current.CancellationToken));
        var step = await db.JobWorkflowRunSteps.AsNoTracking().SingleAsync(s => s.Id == stepRunId, TestContext.Current.CancellationToken);
        Assert.Null(step.JobRunId); // workflow history preserved, run reference detached
    }

    [Fact]
    public async Task Purge_ParentWithSurvivingChildren_DetachesChildrenAndDeletesParent()
    {
        var definitionId = await CreateDefinitionAsync(retentionDays: 1);
        var parentId = await SeedExpiredRunAsync(definitionId);

        // Child finished recently — it survives the purge and must not block deleting the parent.
        Guid childId;
        await using (var db = await CreateDbContextAsync()) {
            childId = LyoGuid.CreateCombPostgres();
            db.JobRuns.Add(new JobRun {
                Id = childId,
                JobDefinitionId = definitionId,
                ParentJobRunId = parentId,
                State = JobState.Finished,
                Result = Models.Enums.JobRunResult.Success,
                CreatedBy = "test",
                CreatedTimestamp = DateTime.UtcNow,
                FinishedTimestamp = DateTime.UtcNow,
                AllowTriggers = false
            });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await InvokeMaintenanceAsync();

        await using (var db = await CreateDbContextAsync()) {
            Assert.False(await db.JobRuns.AnyAsync(r => r.Id == parentId, TestContext.Current.CancellationToken));
            var child = await db.JobRuns.AsNoTracking().SingleAsync(r => r.Id == childId, TestContext.Current.CancellationToken);
            Assert.Null(child.ParentJobRunId);
        }
    }

    private async Task<Guid> CreateDefinitionAsync(int retentionDays)
    {
        var id = LyoGuid.CreateCombPostgres();
        await using var db = await CreateDbContextAsync();
        db.JobDefinitions.Add(new JobDefinition {
            Id = id,
            Name = $"Purge-{id:N}"[..24],
            Type = "Test",
            WorkerType = "cs",
            Enabled = true,
            RetentionDays = retentionDays,
            CreatedTimestamp = DateTime.UtcNow
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return id;
    }

    private async Task<Guid> SeedExpiredRunAsync(Guid definitionId)
    {
        var runId = LyoGuid.CreateCombPostgres();
        await using var db = await CreateDbContextAsync();
        db.JobRuns.Add(new JobRun {
            Id = runId,
            JobDefinitionId = definitionId,
            State = JobState.Finished,
            Result = Models.Enums.JobRunResult.Success,
            CreatedBy = "test",
            CreatedTimestamp = DateTime.UtcNow.AddDays(-10),
            FinishedTimestamp = DateTime.UtcNow.AddDays(-10),
            AllowTriggers = false
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return runId;
    }

    private async Task<Guid> SeedWorkflowRunStepAsync(Guid definitionId, Guid runId)
    {
        var workflowId = LyoGuid.CreateCombPostgres();
        var stepId = LyoGuid.CreateCombPostgres();
        var workflowRunId = LyoGuid.CreateCombPostgres();
        var runStepId = LyoGuid.CreateCombPostgres();

        await using var db = await CreateDbContextAsync();
        db.JobWorkflows.Add(new JobWorkflow { Id = workflowId, Name = "purge-wf", Enabled = true, CreatedTimestamp = DateTime.UtcNow });
        db.JobWorkflowSteps.Add(new JobWorkflowStep {
            Id = stepId, JobWorkflowId = workflowId, JobDefinitionId = definitionId, StepName = "s1", StepOrder = 1,
            FailurePolicy = nameof(JobWorkflowFailurePolicy.Stop), Enabled = true, CreatedTimestamp = DateTime.UtcNow
        });
        db.JobWorkflowRuns.Add(new JobWorkflowRun {
            Id = workflowRunId, JobWorkflowId = workflowId, State = JobWorkflowRunState.Finished, CreatedTimestamp = DateTime.UtcNow
        });
        db.JobWorkflowRunSteps.Add(new JobWorkflowRunStep {
            Id = runStepId, JobWorkflowRunId = workflowRunId, JobWorkflowStepId = stepId, JobRunId = runId,
            State = JobWorkflowStepState.Finished, CreatedTimestamp = DateTime.UtcNow
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return runStepId;
    }

    private async Task InvokeMaintenanceAsync()
    {
        using var scope = _fixture.ServiceProvider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<JobContext>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<JobMaintenanceService>>();
        var maintenance = new JobMaintenanceService(factory, logger, _fixture.FakePublisher);

        var method = typeof(JobMaintenanceService).GetMethod("RunMaintenanceAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        await (Task)method.Invoke(maintenance, [TestContext.Current.CancellationToken])!;
    }

    private async Task<JobContext> CreateDbContextAsync()
    {
        using var scope = _fixture.ServiceProvider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<JobContext>>();
        return await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
    }
}
