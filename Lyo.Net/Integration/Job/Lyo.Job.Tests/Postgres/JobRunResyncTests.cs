using Lyo.Api;
using Lyo.Cache;
using Lyo.Common.Identifiers;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Events;
using JobConstants = Lyo.Job.Models.Constants;
using Lyo.Job.Postgres;
using Lyo.Job.Postgres.Database;
using Lyo.MessageQueue;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Job.Tests.Postgres;

/// <summary>Manual queued-run resync: peek worker queues and republish only missing due <c>Queued</c> runs.</summary>
[Trait("Category", "Integration")]
public class JobRunResyncTests
{
    private readonly JobPostgresFixture _fixture;

    public JobRunResyncTests(JobPostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task ResyncQueuedRuns_WhenPeekEmpty_PublishesDueQueuedRuns()
    {
        var runId = await SeedRunAsync(JobState.Queued);
        var publishCountBefore = _fixture.FakePublisher.Published.Count;

        var (result, error) = await _fixture.JobService.ResyncQueuedRunsAsync(ct: TestContext.Current.CancellationToken);

        Assert.Null(error);
        Assert.NotNull(result);
        Assert.True(result.Queued >= 1);
        Assert.True(result.Republished >= 1);
        Assert.Contains(_fixture.FakePublisher.Published.Skip(publishCountBefore), e => e.Event == "RunCreated" && e.RunId == runId);
    }

    [Fact]
    public async Task ResyncQueuedRuns_WhenPeekContainsRun_DoesNotRepublish()
    {
        var missingId = await SeedRunAsync(JobState.Queued);
        var queuedId = await SeedRunAsync(JobState.Queued);
        var mq = new FakeMqService();
        mq.SeedPeek(JobConstants.Mq.QueueGetJobRunCreated("cs"), queuedId);
        var publisher = new FakeJobEventPublisher();
        using var sp = BuildServiceProvider(publisher, mq);
        using var scope = sp.CreateScope();
        var jobService = scope.ServiceProvider.GetRequiredService<JobService>();

        var (result, error) = await jobService.ResyncQueuedRunsAsync(_fixture.JobDefinitionId, TestContext.Current.CancellationToken);

        Assert.Null(error);
        Assert.NotNull(result);
        Assert.Contains(publisher.Published, e => e.Event == "RunCreated" && e.RunId == missingId);
        Assert.DoesNotContain(publisher.Published, e => e.Event == "RunCreated" && e.RunId == queuedId);
        Assert.True(result.AlreadyInQueue >= 1);
    }

    [Fact]
    public async Task ResyncQueuedRuns_WhenPeekContainsRunOnWaitQueue_DoesNotRepublish()
    {
        var delayedId = await SeedRunAsync(JobState.Queued);
        var mq = new FakeMqService();
        mq.SeedPeek(JobConstants.Mq.QueueGetJobRunCreatedWait("cs"), delayedId);
        var publisher = new FakeJobEventPublisher();
        using var sp = BuildServiceProvider(publisher, mq);
        using var scope = sp.CreateScope();
        var jobService = scope.ServiceProvider.GetRequiredService<JobService>();

        var (result, error) = await jobService.ResyncQueuedRunsAsync(_fixture.JobDefinitionId, TestContext.Current.CancellationToken);

        Assert.Null(error);
        Assert.DoesNotContain(publisher.Published, e => e.Event == "RunCreated" && e.RunId == delayedId);
        Assert.True(result!.AlreadyInQueue >= 1);
    }

    [Fact]
    public async Task ResyncQueuedRuns_SkipsFutureSlotAndDryRun()
    {
        var futureId = await SeedRunAsync(JobState.Queued, r => r.ScheduledSlotUtc = DateTime.UtcNow.AddMinutes(30));
        var dryRunId = await SeedRunAsync(JobState.Queued, r => r.DryRun = true);
        var publisher = new FakeJobEventPublisher();
        using var sp = BuildServiceProvider(publisher);
        using var scope = sp.CreateScope();
        var jobService = scope.ServiceProvider.GetRequiredService<JobService>();

        var (result, error) = await jobService.ResyncQueuedRunsAsync(_fixture.JobDefinitionId, TestContext.Current.CancellationToken);

        Assert.Null(error);
        Assert.DoesNotContain(publisher.Published, e => e.Event == "RunCreated" && e.RunId == futureId);
        Assert.DoesNotContain(publisher.Published, e => e.Event == "RunCreated" && e.RunId == dryRunId);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task ResyncQueuedRuns_WhenPublisherDisconnected_ReturnsError()
    {
        var publisher = new FakeJobEventPublisher();
        publisher.SetConnected(false);
        using var sp = BuildServiceProvider(publisher);
        using var scope = sp.CreateScope();
        var jobService = scope.ServiceProvider.GetRequiredService<JobService>();

        var (result, error) = await jobService.ResyncQueuedRunsAsync(ct: TestContext.Current.CancellationToken);

        Assert.Null(result);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task ResyncQueuedRuns_WhenDefinitionIdSet_ScopesToThatDefinition()
    {
        var otherDefinitionId = await CreateDefinitionAsync();
        var scopedId = await SeedRunAsync(JobState.Queued);
        var otherId = await SeedRunAsync(JobState.Queued, r => r.JobDefinitionId = otherDefinitionId);
        var publisher = new FakeJobEventPublisher();
        using var sp = BuildServiceProvider(publisher);
        using var scope = sp.CreateScope();
        var jobService = scope.ServiceProvider.GetRequiredService<JobService>();

        var (result, error) = await jobService.ResyncQueuedRunsAsync(_fixture.JobDefinitionId, TestContext.Current.CancellationToken);

        Assert.Null(error);
        Assert.Contains(publisher.Published, e => e.Event == "RunCreated" && e.RunId == scopedId);
        Assert.DoesNotContain(publisher.Published, e => e.Event == "RunCreated" && e.RunId == otherId);
        Assert.NotNull(result);
    }

    private ServiceProvider BuildServiceProvider(IJobEventPublisher publisher, IMqService? mqService = null)
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
        services.AddLocalCache();
        services.AddLyoQueryServices();
        services.AddPostgresJobManagement(new PostgresJobOptions { ConnectionString = _fixture.ConnectionString });
        services.AddSingleton(publisher);
        if (mqService is not null)
            services.AddSingleton(mqService);

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

    private async Task<Guid> CreateDefinitionAsync()
    {
        var id = LyoGuid.CreateCombPostgres();
        await using var db = await CreateDbContextAsync();
        db.JobDefinitions.Add(
            new() {
                Id = id,
                Name = $"Resync-{id:N}"[..32],
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
