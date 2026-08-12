using Lyo.Api;
using Lyo.Cache;
using Lyo.Common.Identifiers;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Events;
using Lyo.Job.Models.Request;
using Lyo.Job.Postgres;
using Lyo.Job.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using JobRunResultEnum = Lyo.Job.Models.Enums.JobRunResult;

namespace Lyo.Job.Tests.Postgres;

/// <summary>
/// Regression tests for the job-system review fixes: Started CAS guard, queued-run cancellation, shutdown requeue, dispatch suppression, idempotency-key handling on
/// create/rerun/child-creation, and the batch latest-runs query.
/// </summary>
[Trait("Category", "Integration")]
public class JobRunLifecycleFixTests
{
    private readonly JobPostgresFixture _fixture;

    public JobRunLifecycleFixTests(JobPostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task StartedJobRun_WhenAlreadyRunning_ReturnsErrorWithoutDoubleStart()
    {
        var runId = await SeedRunAsync(JobState.Queued);
        var (first, firstError) = await _fixture.JobService.StartedJobRun(runId);
        Assert.Null(firstError);
        Assert.Equal(JobState.Running, first!.State);

        // Duplicate dispatch delivery: the CAS guard must reject the second start instead of re-executing the run.
        var (second, secondError) = await _fixture.JobService.StartedJobRun(runId);
        Assert.Null(second);
        Assert.NotNull(secondError);
        var run = await GetRunAsync(runId);
        Assert.Equal(JobState.Running, run.State);
    }

    [Fact]
    public async Task StartedJobRun_WhenCancelling_ReturnsErrorAndDoesNotResurrect()
    {
        var runId = await SeedRunAsync(JobState.Cancelling);
        var (result, error) = await _fixture.JobService.StartedJobRun(runId);
        Assert.Null(result);
        Assert.NotNull(error);
        var run = await GetRunAsync(runId);
        Assert.Equal(JobState.Cancelling, run.State);
    }

    [Fact]
    public async Task StartedJobRun_WhenFinished_ReturnsError()
    {
        var runId = await SeedRunAsync(JobState.Finished, r => r.FinishedTimestamp = DateTime.UtcNow);
        var (result, error) = await _fixture.JobService.StartedJobRun(runId);
        Assert.Null(result);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task CancelJobRun_WhenQueued_FinalizesRunAndPublishesFinished()
    {
        var runId = await SeedRunAsync(JobState.Queued);
        var publishCountBefore = _fixture.FakePublisher.Published.Count;
        var (result, error) = await _fixture.JobService.CancelJobRun(runId);
        Assert.Null(error);
        Assert.NotNull(result);
        Assert.Equal(JobState.Finished, result.State);
        Assert.Equal(JobRunResultEnum.Cancelled, result.Result);
        var published = _fixture.FakePublisher.Published.Skip(publishCountBefore).ToList();
        Assert.Contains(published, e => e.Event == "RunCancelled" && e.RunId == runId);
        Assert.Contains(published, e => e.Event == "RunFinished" && e.RunId == runId);

        // A late dispatch delivery for the cancelled run must be rejected by the Started CAS guard.
        var (started, startError) = await _fixture.JobService.StartedJobRun(runId);
        Assert.Null(started);
        Assert.NotNull(startError);
    }

    [Fact]
    public async Task RequeueJobRun_WhenRunning_ReturnsRunToQueued()
    {
        var runId = await SeedRunAsync(
            JobState.Running, r => {
                r.StartedTimestamp = DateTime.UtcNow;
                r.LastHeartbeatUtc = DateTime.UtcNow;
            });

        var (result, error) = await _fixture.JobService.RequeueJobRun(runId);
        Assert.Null(error);
        Assert.NotNull(result);
        Assert.Equal(JobState.Queued, result.State);
        var run = await GetRunAsync(runId);
        Assert.Equal(JobState.Queued, run.State);
        Assert.Null(run.StartedTimestamp);
        Assert.Null(run.LastHeartbeatUtc);

        // The redelivered dispatch message can start the requeued run again.
        var (restarted, restartError) = await _fixture.JobService.StartedJobRun(runId);
        Assert.Null(restartError);
        Assert.Equal(JobState.Running, restarted!.State);
    }

    [Fact]
    public async Task RequeueJobRun_WhenNotRunning_ReturnsError()
    {
        var runId = await SeedRunAsync(JobState.Cancelling, r => r.StartedTimestamp = DateTime.UtcNow);
        var (result, error) = await _fixture.JobService.RequeueJobRun(runId);

        // A pending user cancellation must not be forgotten by a shutdown hand-back.
        Assert.Null(result);
        Assert.NotNull(error);
        Assert.Equal(JobState.Cancelling, (await GetRunAsync(runId)).State);
    }

    [Fact]
    public async Task CreateJobRun_WithSuppressDispatch_PersistsQueuedWithoutPublishing()
    {
        var publishCountBefore = _fixture.FakePublisher.Published.Count;
        var req = new JobRunReq(_fixture.JobDefinitionId, "test-user", false) { SuppressDispatch = true };
        var result = await _fixture.JobService.CreateJobRun(req, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess);
        Assert.Equal(JobState.Queued, result.Data!.State);
        Assert.DoesNotContain(_fixture.FakePublisher.Published.Skip(publishCountBefore), e => e.Event == "RunCreated" && e.RunId == result.Data.Id);
    }

    [Fact]
    public async Task CreateJobRun_WithFutureScheduledSlot_SuppressesImmediateDispatch()
    {
        var publishCountBefore = _fixture.FakePublisher.Published.Count;
        var req = new JobRunReq(_fixture.JobDefinitionId, "test-user", false) { ScheduledSlotUtc = DateTime.UtcNow.AddMinutes(5) };
        var result = await _fixture.JobService.CreateJobRun(req, TestContext.Current.CancellationToken);

        // Delayed retry: the run must wait for its slot (maintenance redispatch), not be delivered immediately.
        Assert.True(result.IsSuccess);
        Assert.DoesNotContain(_fixture.FakePublisher.Published.Skip(publishCountBefore), e => e.Event == "RunCreated" && e.RunId == result.Data!.Id);
    }

    [Fact]
    public async Task CreateJobRun_WithSuppressDispatch_SucceedsWhenMqDisconnected()
    {
        var disconnectedPublisher = new FakeJobEventPublisher();
        disconnectedPublisher.SetConnected(false);
        using var sp = BuildServiceProvider(disconnectedPublisher);
        using var scope = sp.CreateScope();
        var jobService = scope.ServiceProvider.GetRequiredService<JobService>();
        var req = new JobRunReq(_fixture.JobDefinitionId, "test-user", false) { SuppressDispatch = true };
        var result = await jobService.CreateJobRun(req, TestContext.Current.CancellationToken);

        // The caller owns dispatch, so a disconnected MQ must not block persistence.
        Assert.True(result.IsSuccess);
        Assert.Equal(JobState.Queued, result.Data!.State);
    }

    [Fact]
    public async Task CreateJobRun_WithDuplicateIdempotencyKey_ReturnsExistingRunWithoutThrowing()
    {
        var key = $"lifecycle-{Guid.NewGuid():N}";
        var first = await _fixture.JobService.CreateJobRun(new(_fixture.JobDefinitionId, "test-user", false) { IdempotencyKey = key }, TestContext.Current.CancellationToken);
        Assert.True(first.IsSuccess);
        var second = await _fixture.JobService.CreateJobRun(new(_fixture.JobDefinitionId, "test-user", false) { IdempotencyKey = key }, TestContext.Current.CancellationToken);
        Assert.True(second.IsSuccess);
        Assert.Equal(first.Data!.Id, second.Data!.Id);
    }

    [Fact]
    public async Task CreateJobRun_ConcurrentDuplicateIdempotencyKeys_AllResolveToOneRun()
    {
        var key = $"race-{Guid.NewGuid():N}";
        var tasks = Enumerable.Range(0, 4)
            .Select(_ => _fixture.JobService.CreateJobRun(new(_fixture.JobDefinitionId, "test-user", false) { IdempotencyKey = key }, TestContext.Current.CancellationToken))
            .ToList();

        var results = await Task.WhenAll(tasks);

        // Regression for the double-commit bug: the unique-violation recovery path must return the existing run, not throw
        // "transaction has already been committed".
        Assert.All(results, r => Assert.True(r.IsSuccess, r.Error?.Detail ?? "create failed"));
        Assert.Single(results.Select(r => r.Data!.Id).Distinct());
    }

    [Fact]
    public async Task RerunJob_WhenOriginalHasIdempotencyKey_CreatesFreshRun()
    {
        var key = $"rerun-{Guid.NewGuid():N}";
        var original = await _fixture.JobService.CreateJobRun(new(_fixture.JobDefinitionId, "test-user", false) { IdempotencyKey = key }, TestContext.Current.CancellationToken);
        Assert.True(original.IsSuccess);
        await FinishRunAsync(original.Data!.Id);
        var rerun = await _fixture.JobService.RerunJob(original.Data.Id);

        // A copied key would resolve back to the original run (or violate the unique index) instead of creating a new one.
        Assert.NotNull(rerun);
        Assert.True(rerun.IsSuccess, rerun.Error?.Detail ?? "rerun failed");
        Assert.NotEqual(original.Data.Id, rerun.Data!.Id);
        Assert.Null(rerun.Data.IdempotencyKey);
        Assert.Equal(original.Data.Id, (await GetRunAsync(rerun.Data.Id)).ReRanFromJobRunId);
    }

    [Fact]
    public async Task CreateChildRuns_WhenParentHasIdempotencyKey_CreatesRealChildren()
    {
        var key = $"parent-{Guid.NewGuid():N}";
        var parent = await _fixture.JobService.CreateJobRun(new(_fixture.JobDefinitionId, "test-user", false) { IdempotencyKey = key }, TestContext.Current.CancellationToken);
        Assert.True(parent.IsSuccess);
        var children = await _fixture.JobService.CreateChildRunsAsync(
            parent.Data!.Id, new JobCreateChildRunsReq { Children = [new() { BatchIndex = 0 }, new() { BatchIndex = 1 }] }, TestContext.Current.CancellationToken);

        // With the key copied from the parent, both "children" silently resolved to the parent run via the idempotency lookup.
        Assert.Equal(2, children.Count);
        Assert.All(children, c => Assert.NotEqual(parent.Data.Id, c.Id));
        Assert.Equal(2, children.Select(c => c.Id).Distinct().Count());
        foreach (var child in children) {
            var entity = await GetRunAsync(child.Id);
            Assert.Equal(parent.Data.Id, entity.ParentJobRunId);
            Assert.Null(entity.IdempotencyKey);
        }
    }

    [Fact]
    public async Task GetLatestRuns_ReturnsLatestRunPerCategory()
    {
        var definitionId = await CreateDefinitionAsync();
        var now = DateTime.UtcNow;
        var oldSuccessId = await SeedRunAsync(
            JobState.Finished, r => {
                r.JobDefinitionId = definitionId;
                r.Result = JobRunResultEnum.Success;
                r.CreatedTimestamp = now.AddMinutes(-30);
                r.FinishedTimestamp = now.AddMinutes(-29);
            });

        await SeedRunAsync(
            JobState.Finished, r => {
                r.JobDefinitionId = definitionId;
                r.Result = JobRunResultEnum.Failure;
                r.CreatedTimestamp = now.AddMinutes(-20);
                r.FinishedTimestamp = now.AddMinutes(-19);
            });

        var latestFailureId = await SeedRunAsync(
            JobState.Finished, r => {
                r.JobDefinitionId = definitionId;
                r.Result = JobRunResultEnum.Failure;
                r.CreatedTimestamp = now.AddMinutes(-10);
                r.FinishedTimestamp = now.AddMinutes(-9);
            });

        var results = await _fixture.JobService.GetLatestRuns([definitionId], TestContext.Current.CancellationToken);
        var latest = Assert.Single(results);
        Assert.Equal(definitionId, latest.JobDefinitionId);
        Assert.Equal(latestFailureId, latest.LastRun!.Id);
        Assert.Equal(oldSuccessId, latest.LastSuccessfulRun!.Id);
        Assert.Equal(latestFailureId, latest.LastFailedRun!.Id);
    }

    private ServiceProvider BuildServiceProvider(IJobEventPublisher publisher)
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
        services.AddLocalCache();
        services.AddLyoQueryServices();
        services.AddPostgresJobManagement(new PostgresJobOptions { ConnectionString = _fixture.ConnectionString });
        services.AddSingleton(publisher);
        services.AddScoped<JobService>();
        return services.BuildServiceProvider();
    }

    private async Task<Guid> SeedRunAsync(JobState state, Action<JobRun>? configure = null)
    {
        var run = new JobRun {
            Id = LyoGuid.CreateCombPostgres(),
            JobDefinitionId = _fixture.JobDefinitionId,
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

    private async Task FinishRunAsync(Guid runId)
    {
        await using var db = await CreateDbContextAsync();
        var run = await db.JobRuns.FirstAsync(r => r.Id == runId, TestContext.Current.CancellationToken);
        run.State = JobState.Finished;
        run.Result = JobRunResultEnum.Success;
        run.FinishedTimestamp = DateTime.UtcNow;
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private async Task<JobRun> GetRunAsync(Guid runId)
    {
        await using var db = await CreateDbContextAsync();
        return await db.JobRuns.AsNoTracking().FirstAsync(r => r.Id == runId, TestContext.Current.CancellationToken);
    }

    private async Task<Guid> CreateDefinitionAsync()
    {
        var id = LyoGuid.CreateCombPostgres();
        await using var db = await CreateDbContextAsync();
        db.JobDefinitions.Add(
            new() {
                Id = id,
                Name = $"LatestRuns-{id:N}"[..32],
                Type = "Test",
                WorkerType = "cs",
                Enabled = true,
                CreatedTimestamp = DateTime.UtcNow
            });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return id;
    }

    private async Task<JobContext> CreateDbContextAsync()
    {
        using var scope = _fixture.ServiceProvider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<JobContext>>();
        return await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
    }
}