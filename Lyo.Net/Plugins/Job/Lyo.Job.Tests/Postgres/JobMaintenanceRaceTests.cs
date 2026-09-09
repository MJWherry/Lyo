using System.Reflection;
using Lyo.Common.Core.Identifiers;
using Lyo.Job.Models.Enums;
using Lyo.Job.Postgres;
using Lyo.Job.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using JobRunResultEnum = Lyo.Job.Models.Enums.JobRunResult;

namespace Lyo.Job.Tests.Postgres;

/// <summary>
/// Regression checks for the maintenance-versus-finish race: dead-job detection re-reads the heartbeat inside a guarded update, the orphan ceiling bounds runs whose definition
/// sets no timeout, and <c>job_run</c> carries an <c>xmin</c> concurrency token so a read-modify-write pass cannot clobber a worker's concurrent finish.
/// </summary>
[Trait("Category", "Integration")]
[Collection(JobMaintenanceCollection.Name)]
public class JobMaintenanceRaceTests
{
    private readonly JobPostgresFixture _fixture;

    public JobMaintenanceRaceTests(JobPostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task FreshHeartbeat_OnALongRunningRun_IsNotTimedOut()
    {
        // A run started an hour ago but heartbeating right now is healthy: the deadline is measured from the last heartbeat, not from the start.
        var definitionId = await CreateDefinitionAsync(d => d.TimeoutMinutes = 5);
        var runId = await SeedRunAsync(
            definitionId, JobState.Running, r => {
                r.StartedTimestamp = DateTime.UtcNow.AddHours(-1);
                r.LastHeartbeatUtc = DateTime.UtcNow;
            });

        await InvokeMaintenanceAsync();
        var run = await GetRunAsync(runId);
        Assert.Equal(JobState.Running, run.State);
        Assert.Null(run.FinishedTimestamp);
    }

    [Fact]
    public async Task RunThatDiedBeforeItsFirstHeartbeat_FallsBackToTheStartTimestamp()
    {
        var definitionId = await CreateDefinitionAsync(d => d.TimeoutMinutes = 5);
        var runId = await SeedRunAsync(definitionId, JobState.Running, r => r.StartedTimestamp = DateTime.UtcNow.AddHours(-1));
        await InvokeMaintenanceAsync();
        var run = await GetRunAsync(runId);
        Assert.Equal(JobState.Finished, run.State);
        Assert.Equal(JobRunResultEnum.Timeout, run.Result);
    }

    [Fact]
    public async Task CancellingRunWithAStaleHeartbeat_IsAlsoTimedOut()
    {
        // A worker that dies while draining a cancellation would otherwise sit in Cancelling forever.
        var definitionId = await CreateDefinitionAsync(d => d.TimeoutMinutes = 5);
        var runId = await SeedRunAsync(
            definitionId, JobState.Cancelling, r => {
                r.StartedTimestamp = DateTime.UtcNow.AddHours(-1);
                r.LastHeartbeatUtc = DateTime.UtcNow.AddHours(-1);
            });

        await InvokeMaintenanceAsync();
        Assert.Equal(JobState.Finished, (await GetRunAsync(runId)).State);
    }

    [Fact]
    public async Task RunWithoutADefinitionTimeout_IsBoundedByTheOrphanCeiling()
    {
        var definitionId = await CreateDefinitionAsync(d => d.TimeoutMinutes = 0);
        var runId = await SeedRunAsync(
            definitionId, JobState.Running, r => {
                r.StartedTimestamp = DateTime.UtcNow.AddDays(-3);
                r.LastHeartbeatUtc = DateTime.UtcNow.AddDays(-3);
            });

        await InvokeMaintenanceAsync(new() { OrphanedRunTimeoutMinutes = 1440 });
        var run = await GetRunAsync(runId);
        Assert.Equal(JobState.Finished, run.State);
        Assert.Equal(JobRunResultEnum.Timeout, run.Result);
    }

    [Fact]
    public async Task RunWithoutADefinitionTimeout_InsideTheOrphanCeiling_KeepsRunning()
    {
        var definitionId = await CreateDefinitionAsync(d => d.TimeoutMinutes = 0);
        var runId = await SeedRunAsync(
            definitionId, JobState.Running, r => {
                r.StartedTimestamp = DateTime.UtcNow.AddHours(-2);
                r.LastHeartbeatUtc = DateTime.UtcNow.AddHours(-2);
            });

        await InvokeMaintenanceAsync(new() { OrphanedRunTimeoutMinutes = 1440 });
        Assert.Equal(JobState.Running, (await GetRunAsync(runId)).State);
    }

    [Fact]
    public async Task OrphanCeilingOfZero_LeavesUnboundedRunsAlone()
    {
        var definitionId = await CreateDefinitionAsync(d => d.TimeoutMinutes = 0);
        var runId = await SeedRunAsync(
            definitionId, JobState.Running, r => {
                r.StartedTimestamp = DateTime.UtcNow.AddDays(-30);
                r.LastHeartbeatUtc = DateTime.UtcNow.AddDays(-30);
            });

        await InvokeMaintenanceAsync(new() { OrphanedRunTimeoutMinutes = 0 });
        Assert.Equal(JobState.Running, (await GetRunAsync(runId)).State);
    }

    [Fact]
    public async Task WorkerHeartbeatDuringThePass_WinsOverTheStaleCandidateScan()
    {
        // Reproduces the race by hand: the pass sees a stale candidate, the worker heartbeats, and the guarded update must then find nothing to do.
        var definitionId = await CreateDefinitionAsync(d => d.TimeoutMinutes = 5);
        var runId = await SeedRunAsync(
            definitionId, JobState.Running, r => {
                r.StartedTimestamp = DateTime.UtcNow.AddHours(-1);
                r.LastHeartbeatUtc = DateTime.UtcNow.AddHours(-1);
            });

        var cutoff = DateTime.UtcNow.AddMinutes(-5);
        await using (var refresher = await CreateDbContextAsync()) {
            await refresher.JobRuns.Where(r => r.Id == runId)
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.LastHeartbeatUtc, DateTime.UtcNow), TestContext.Current.CancellationToken);
        }

        await using var db = await CreateDbContextAsync();
        var rows = await db.JobRuns
            .Where(r => r.Id == runId && (r.State == JobState.Running || r.State == JobState.Cancelling) &&
                (r.LastHeartbeatUtc ?? r.StartedTimestamp ?? r.CreatedTimestamp) <= cutoff)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.State, JobState.Finished), TestContext.Current.CancellationToken);

        Assert.Equal(0, rows);
        Assert.Equal(JobState.Running, (await GetRunAsync(runId)).State);
    }

    [Fact]
    public async Task JobRun_CarriesAConcurrencyTokenSoAStaleWriteIsRejected()
    {
        var definitionId = await CreateDefinitionAsync(d => d.TimeoutMinutes = 5);
        var runId = await SeedRunAsync(definitionId, JobState.Running, r => r.StartedTimestamp = DateTime.UtcNow);

        await using var stale = await CreateDbContextAsync();
        var staleRun = await stale.JobRuns.FirstAsync(r => r.Id == runId, TestContext.Current.CancellationToken);

        await using (var winner = await CreateDbContextAsync()) {
            var run = await winner.JobRuns.FirstAsync(r => r.Id == runId, TestContext.Current.CancellationToken);
            run.State = JobState.Finished;
            run.Result = JobRunResultEnum.Success;
            run.FinishedTimestamp = DateTime.UtcNow;
            await winner.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Without the xmin token this stale write would silently overwrite the worker's finish with a timeout.
        staleRun.State = JobState.Finished;
        staleRun.Result = JobRunResultEnum.Timeout;
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => stale.SaveChangesAsync(TestContext.Current.CancellationToken));

        var persisted = await GetRunAsync(runId);
        Assert.Equal(JobRunResultEnum.Success, persisted.Result);
    }

    [Fact]
    public async Task LateWorkerFinish_AfterMaintenanceTimedTheRunOut_IsRejected()
    {
        var definitionId = await CreateDefinitionAsync(d => d.TimeoutMinutes = 5);
        var runId = await SeedRunAsync(
            definitionId, JobState.Running, r => {
                r.StartedTimestamp = DateTime.UtcNow.AddHours(-1);
                r.LastHeartbeatUtc = DateTime.UtcNow.AddHours(-1);
            });

        await InvokeMaintenanceAsync();
        Assert.Equal(JobRunResultEnum.Timeout, (await GetRunAsync(runId)).Result);

        // The worker eventually reports success; the finish CAS guard must reject it instead of append results to a finished run.
        var (result, error) = await _fixture.JobService.FinishedJobRun(runId, [], TestContext.Current.CancellationToken);
        Assert.Null(result);
        Assert.NotNull(error);
        Assert.Equal(JobRunResultEnum.Timeout, (await GetRunAsync(runId)).Result);
    }

    private async Task<Guid> CreateDefinitionAsync(Action<JobDefinition>? configure = null)
    {
        var id = LyoGuid.CreateCombPostgres();
        var definition = new JobDefinition {
            Id = id,
            Name = $"Race-{id:N}"[..24],
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

    private async Task<JobRun> GetRunAsync(Guid runId)
    {
        await using var db = await CreateDbContextAsync();
        return await db.JobRuns.AsNoTracking().FirstAsync(r => r.Id == runId, TestContext.Current.CancellationToken);
    }

    private async Task<JobContext> CreateDbContextAsync()
    {
        using var scope = _fixture.ServiceProvider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<JobContext>>();
        return await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
    }
}
