using System.Reflection;
using Lyo.Job.Models.Enums;
using Lyo.Job.Postgres;
using Lyo.Job.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Job.Tests.Postgres;

[Trait("Category", "Integration")]
[Collection(JobMaintenanceCollection.Name)]
public class JobMaintenanceSlaTests
{
    private readonly JobPostgresFixture _fixture;

    public JobMaintenanceSlaTests(JobPostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task RunMaintenance_WhenQueuedRunPastMustStartBy_MarksSlaBreached()
    {
        var factory = GetDbContextFactory();
        var runId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken)) {
            var definition = await db.JobDefinitions.FirstAsync(d => d.Id == _fixture.JobDefinitionId, TestContext.Current.CancellationToken);
            definition.MustStartByMinutes = 5;
            definition.ExpectedDurationMinutes = 0;
            db.JobRuns.Add(
                new() {
                    Id = runId,
                    JobDefinitionId = _fixture.JobDefinitionId,
                    State = JobState.Queued,
                    CreatedBy = "test",
                    CreatedTimestamp = now.AddMinutes(-10),
                    AllowTriggers = false
                });

            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await InvokeMaintenanceAsync(factory);
        await using var verify = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var run = await verify.JobRuns.AsNoTracking().FirstAsync(r => r.Id == runId, TestContext.Current.CancellationToken);
        Assert.True(run.SlaBreached);
    }

    [Fact]
    public async Task RunMaintenance_WhenRunningRunExceedsExpectedDuration_MarksSlaBreached()
    {
        var factory = GetDbContextFactory();
        var runId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken)) {
            var definition = await db.JobDefinitions.FirstAsync(d => d.Id == _fixture.JobDefinitionId, TestContext.Current.CancellationToken);
            definition.ExpectedDurationMinutes = 5;
            definition.MustStartByMinutes = 0;
            db.JobRuns.Add(
                new() {
                    Id = runId,
                    JobDefinitionId = _fixture.JobDefinitionId,
                    State = JobState.Running,
                    CreatedBy = "test",
                    CreatedTimestamp = now.AddMinutes(-20),
                    StartedTimestamp = now.AddMinutes(-15),
                    AllowTriggers = false
                });

            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await InvokeMaintenanceAsync(factory);
        await using var verify = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var run = await verify.JobRuns.AsNoTracking().FirstAsync(r => r.Id == runId, TestContext.Current.CancellationToken);
        Assert.True(run.SlaBreached);
    }

    private async Task InvokeMaintenanceAsync(IDbContextFactory<JobContext> factory)
    {
        using var scope = _fixture.ServiceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<JobMaintenanceService>>();
        var maintenance = new JobMaintenanceService(factory, logger, _fixture.FakePublisher);
        var method = typeof(JobMaintenanceService).GetMethod("RunMaintenanceAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        await (Task)method.Invoke(maintenance, [TestContext.Current.CancellationToken])!;
    }

    private IDbContextFactory<JobContext> GetDbContextFactory()
    {
        using var scope = _fixture.ServiceProvider.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IDbContextFactory<JobContext>>();
    }
}