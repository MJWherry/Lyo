using System.Reflection;
using Lyo.Common.Identifiers;
using Lyo.Job.Models.Enums;
using Lyo.Job.Postgres;
using Lyo.Job.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using JobRunResultEnum = Lyo.Job.Models.Enums.JobRunResult;

namespace Lyo.Job.Tests.Postgres;

/// <summary>
/// Regression tests for the maintenance-service review fixes: dead-job timeouts publish run-finished events (so retries/triggers/circuit breaker fire), stuck <c>Queued</c>
/// runs are redispatched, and audit timestamps are stamped on async saves.
/// </summary>
[Trait("Category", "Integration")]
[Collection(JobMaintenanceCollection.Name)]
public class JobMaintenanceRecoveryTests
{
    private readonly JobPostgresFixture _fixture;

    public JobMaintenanceRecoveryTests(JobPostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task DeadJobTimeout_PublishesRunFinishedEvent()
    {
        var definitionId = await CreateDefinitionAsync(d => d.TimeoutMinutes = 5);
        var runId = await SeedRunAsync(
            definitionId, JobState.Running, r => {
                r.StartedTimestamp = DateTime.UtcNow.AddMinutes(-30);
                r.LastHeartbeatUtc = DateTime.UtcNow.AddMinutes(-30);
            });

        var publishCountBefore = _fixture.FakePublisher.Published.Count;
        await InvokeMaintenanceAsync();
        await using var db = await CreateDbContextAsync();
        var run = await db.JobRuns.AsNoTracking().SingleAsync(r => r.Id == runId, TestContext.Current.CancellationToken);
        Assert.Equal(JobState.Finished, run.State);
        Assert.Equal(JobRunResultEnum.Timeout, run.Result);

        // Without the finished event, a timeout is a silent dead end: no retries, triggers, or circuit-breaker accounting.
        Assert.Contains(_fixture.FakePublisher.Published.Skip(publishCountBefore), e => e.Event == "RunFinished" && e.RunId == runId);
    }

    [Fact]
    public async Task StuckQueuedRun_IsRedispatched()
    {
        var definitionId = await CreateDefinitionAsync();
        var runId = await SeedRunAsync(
            definitionId, JobState.Queued, r => {
                r.CreatedTimestamp = DateTime.UtcNow.AddMinutes(-30);
                r.UpdatedTimestamp = DateTime.UtcNow.AddMinutes(-30);
            });

        var publishCountBefore = _fixture.FakePublisher.Published.Count;
        await InvokeMaintenanceAsync(new() { QueuedRunRedispatchMinutes = 10 });
        Assert.Contains(_fixture.FakePublisher.Published.Skip(publishCountBefore), e => e.Event == "RunCreated" && e.RunId == runId);

        // Redispatch bumps UpdatedTimestamp so the run is retried once per threshold window, not every tick.
        await using var db = await CreateDbContextAsync();
        var run = await db.JobRuns.AsNoTracking().SingleAsync(r => r.Id == runId, TestContext.Current.CancellationToken);
        Assert.True(run.UpdatedTimestamp > DateTime.UtcNow.AddMinutes(-5));
    }

    [Fact]
    public async Task DueDelayedRetry_IsDispatchedBySlot()
    {
        var definitionId = await CreateDefinitionAsync();
        var runId = await SeedRunAsync(
            definitionId, JobState.Queued, r => {
                // Delayed retry: created (with dispatch suppressed) before its slot; the slot has now come due.
                r.CreatedTimestamp = DateTime.UtcNow.AddMinutes(-3);
                r.UpdatedTimestamp = DateTime.UtcNow.AddMinutes(-3);
                r.ScheduledSlotUtc = DateTime.UtcNow.AddMinutes(-1);
            });

        var publishCountBefore = _fixture.FakePublisher.Published.Count;
        await InvokeMaintenanceAsync(new() { QueuedRunRedispatchMinutes = 10 });
        Assert.Contains(_fixture.FakePublisher.Published.Skip(publishCountBefore), e => e.Event == "RunCreated" && e.RunId == runId);
    }

    [Fact]
    public async Task QueuedRunWithFutureSlot_IsNotRedispatched()
    {
        var definitionId = await CreateDefinitionAsync();
        var runId = await SeedRunAsync(
            definitionId, JobState.Queued, r => {
                r.CreatedTimestamp = DateTime.UtcNow.AddMinutes(-30);
                r.UpdatedTimestamp = DateTime.UtcNow.AddMinutes(-30);
                r.ScheduledSlotUtc = DateTime.UtcNow.AddMinutes(30);
            });

        var publishCountBefore = _fixture.FakePublisher.Published.Count;
        await InvokeMaintenanceAsync(new() { QueuedRunRedispatchMinutes = 10 });
        Assert.DoesNotContain(_fixture.FakePublisher.Published.Skip(publishCountBefore), e => e.Event == "RunCreated" && e.RunId == runId);
    }

    [Fact]
    public async Task SaveChangesAsync_StampsCreatedAndUpdatedTimestamps()
    {
        var definitionId = LyoGuid.CreateCombPostgres();
        await using (var db = await CreateDbContextAsync()) {
            // CreatedTimestamp deliberately left at default — the SavingChanges hook must stamp it on the async save path.
            db.JobDefinitions.Add(
                new() {
                    Id = definitionId,
                    Name = $"Stamp-{definitionId:N}"[..24],
                    Type = "Test",
                    WorkerType = "cs",
                    Enabled = true
                });

            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        DateTime created;
        await using (var db = await CreateDbContextAsync()) {
            var def = await db.JobDefinitions.AsNoTracking().SingleAsync(d => d.Id == definitionId, TestContext.Current.CancellationToken);
            Assert.NotEqual(default, def.CreatedTimestamp);
            Assert.Null(def.UpdatedTimestamp);
            created = def.CreatedTimestamp;
        }

        await using (var db = await CreateDbContextAsync()) {
            var def = await db.JobDefinitions.SingleAsync(d => d.Id == definitionId, TestContext.Current.CancellationToken);
            def.Description = "modified";
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using (var verify = await CreateDbContextAsync()) {
            var def = await verify.JobDefinitions.AsNoTracking().SingleAsync(d => d.Id == definitionId, TestContext.Current.CancellationToken);
            Assert.NotNull(def.UpdatedTimestamp); // before the fix, only sync SaveChanges() stamped this
            Assert.Equal(created, def.CreatedTimestamp);
        }
    }

    private async Task<Guid> CreateDefinitionAsync(Action<JobDefinition>? configure = null)
    {
        var id = LyoGuid.CreateCombPostgres();
        var definition = new JobDefinition {
            Id = id,
            Name = $"Recovery-{id:N}"[..24],
            Type = "Test",
            WorkerType = "cs",
            Enabled = true,
            CreatedTimestamp = DateTime.UtcNow
        };

        configure?.Invoke(definition);
        await using var db = await CreateDbContextAsync();
        db.JobDefinitions.Add(definition);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return id;
    }

    private async Task<Guid> SeedRunAsync(Guid definitionId, JobState state, Action<JobRun>? configure = null)
    {
        var run = new JobRun {
            Id = LyoGuid.CreateCombPostgres(),
            JobDefinitionId = definitionId,
            State = state,
            CreatedBy = "test",
            CreatedTimestamp = DateTime.UtcNow,
            AllowTriggers = false
        };

        configure?.Invoke(run);
        await using var db = await CreateDbContextAsync();
        db.JobRuns.Add(run);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return run.Id;
    }

    private async Task InvokeMaintenanceAsync(JobMaintenanceOptions? options = null)
    {
        using var scope = _fixture.ServiceProvider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<JobContext>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<JobMaintenanceService>>();
        var maintenance = new JobMaintenanceService(factory, logger, _fixture.FakePublisher, options);
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