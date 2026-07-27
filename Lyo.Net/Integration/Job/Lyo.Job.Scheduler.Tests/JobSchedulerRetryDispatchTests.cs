using System.Collections;
using System.Reflection;
using System.Text.Json;
using Lyo.Formatter;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Response;
using Lyo.MessageQueue;

namespace Lyo.Job.Scheduler.Tests;

/// <summary>
/// Regression tests for the scheduler review fixes: retry backoff produces exactly one dispatch path (delayed-MQ envelope or future slot — never an immediate publish plus
/// a delayed one), retry/trigger creation is idempotent across duplicate completion messages, timeouts count as failures, and deleted definitions are evicted from the
/// in-memory cache on 404.
/// </summary>
public class JobSchedulerRetryDispatchTests
{
    [Fact]
    public async Task FailedRun_WithDelayedMq_SuppressesImmediateDispatchAndSendsDelayedEnvelope()
    {
        var definition = BuildDefinition(maxRetryCount: 2, retryBackoffSeconds: 30);
        var api = new FakeSchedulerApiClient(definition);
        var delayedMq = new RecordingDelayedMqService();
        var scheduler = CreateScheduler(api, delayedMq);
        await scheduler.RefreshDefinitionsAsync(TestContext.Current.CancellationToken);
        var failedRun = BuildFailedRun(definition.Id, JobRunResult.Failure);

        await InvokeProcessCompletedAsync(scheduler, failedRun);

        var retryReq = Assert.Single(api.CreatedRunRequests);
        Assert.True(retryReq.SuppressDispatch); // the delayed envelope below is the sole dispatch
        Assert.Null(retryReq.ScheduledSlotUtc);
        Assert.Equal(1, retryReq.RetryAttempt);
        Assert.Equal(failedRun.Id, retryReq.ReRanFromJobRunId);
        Assert.Equal($"retry:{failedRun.Id:N}:1", retryReq.IdempotencyKey);

        var delayed = Assert.Single(delayedMq.DelayedSends);
        Assert.Equal(Models.Constants.Mq.QueueGetJobRunCreated(definition.WorkerType), delayed.QueueName);
        Assert.Equal(TimeSpan.FromSeconds(30), delayed.Delay);
    }

    [Fact]
    public async Task FailedRun_WithoutDelayedMq_SchedulesRetryViaFutureSlot()
    {
        var definition = BuildDefinition(maxRetryCount: 2, retryBackoffSeconds: 30);
        var api = new FakeSchedulerApiClient(definition);
        var scheduler = CreateScheduler(api, mqService: null);
        await scheduler.RefreshDefinitionsAsync(TestContext.Current.CancellationToken);
        var failedRun = BuildFailedRun(definition.Id, JobRunResult.Failure);

        var before = DateTime.UtcNow;
        await InvokeProcessCompletedAsync(scheduler, failedRun);

        // No delayed transport: the future slot suppresses the API's immediate publish and the maintenance service dispatches when due.
        var retryReq = Assert.Single(api.CreatedRunRequests);
        Assert.False(retryReq.SuppressDispatch);
        Assert.NotNull(retryReq.ScheduledSlotUtc);
        Assert.InRange(retryReq.ScheduledSlotUtc!.Value, before.AddSeconds(20), DateTime.UtcNow.AddSeconds(40));
    }

    [Fact]
    public async Task DuplicateCompletionMessages_ProduceIdenticalRetryIdempotencyKeys()
    {
        var definition = BuildDefinition(maxRetryCount: 3, retryBackoffSeconds: 0);
        var api = new FakeSchedulerApiClient(definition);
        var scheduler = CreateScheduler(api, mqService: null);
        await scheduler.RefreshDefinitionsAsync(TestContext.Current.CancellationToken);
        var failedRun = BuildFailedRun(definition.Id, JobRunResult.Failure);

        // The same completion message delivered twice (competing schedulers / broker redelivery).
        await InvokeProcessCompletedAsync(scheduler, failedRun);
        await InvokeProcessCompletedAsync(scheduler, failedRun);

        Assert.Equal(2, api.CreatedRunRequests.Count);
        Assert.Single(api.CreatedRunRequests.Select(r => r.IdempotencyKey).Distinct());
        Assert.All(api.CreatedRunRequests, r => Assert.Equal($"retry:{failedRun.Id:N}:1", r.IdempotencyKey));
    }

    [Fact]
    public async Task TimedOutRun_CountsAsFailureAndSchedulesRetry()
    {
        var definition = BuildDefinition(maxRetryCount: 1, retryBackoffSeconds: 0);
        var api = new FakeSchedulerApiClient(definition);
        var scheduler = CreateScheduler(api, mqService: null);
        await scheduler.RefreshDefinitionsAsync(TestContext.Current.CancellationToken);

        await InvokeProcessCompletedAsync(scheduler, BuildFailedRun(definition.Id, JobRunResult.Timeout));

        // Dead-job timeouts must feed the retry pipeline like any other failure.
        var retryReq = Assert.Single(api.CreatedRunRequests);
        Assert.Equal(1, retryReq.RetryAttempt);
    }

    [Fact]
    public async Task CancelledRun_DoesNotScheduleRetry()
    {
        var definition = BuildDefinition(maxRetryCount: 3, retryBackoffSeconds: 0);
        var api = new FakeSchedulerApiClient(definition);
        var scheduler = CreateScheduler(api, mqService: null);
        await scheduler.RefreshDefinitionsAsync(TestContext.Current.CancellationToken);

        await InvokeProcessCompletedAsync(scheduler, BuildFailedRun(definition.Id, JobRunResult.Cancelled));

        Assert.Empty(api.CreatedRunRequests);
    }

    [Fact]
    public async Task OnDefinitionUpdated_WhenDefinitionDeleted_EvictsItFromCache()
    {
        var definition = BuildDefinition(maxRetryCount: 0, retryBackoffSeconds: 0);
        var api = new FakeSchedulerApiClient(definition);
        var scheduler = CreateScheduler(api, mqService: null);
        await scheduler.RefreshDefinitionsAsync(TestContext.Current.CancellationToken);
        Assert.True(GetCachedJobs(scheduler).Contains(definition.Id));

        api.Return404ForDefinitionGet = true;
        var requeue = await InvokeOnDefinitionUpdatedAsync(scheduler, definition.Id);

        // The deleted definition must leave the cache, or the scheduler keeps creating doomed runs for it.
        Assert.False(requeue);
        Assert.False(GetCachedJobs(scheduler).Contains(definition.Id));
    }

    private static JobScheduler CreateScheduler(FakeSchedulerApiClient api, IMqService? mqService)
        => new(
            new JobSchedulerOptions {
                ApiBaseUrl = "http://localhost/api",
                DefinitionRefreshIntervalSeconds = 3600,
                ScheduleCheckIntervalSeconds = 3600,
                EnableMisfireCatchUp = false
            },
            api,
            new FormatterService(),
            new FakeEventPublisher(),
            mqService: mqService);

    private static JobDefinitionRes BuildDefinition(int maxRetryCount, int retryBackoffSeconds)
        => new(
            Guid.NewGuid(), "RetryDef", null, "Test", "cs", true,
            JobParameters: [], JobSchedules: [], JobTriggers: [], JobParallelRestrictions: null,
            MaxRetryCount: maxRetryCount, RetryBackoffSeconds: retryBackoffSeconds);

    private static JobRunRes BuildFailedRun(Guid definitionId, JobRunResult result)
        => new() {
            Id = Guid.NewGuid(),
            JobDefinitionId = definitionId,
            State = JobState.Finished,
            Result = result,
            RetryAttempt = 0,
            AllowTriggers = false,
            CreatedTimestamp = DateTime.UtcNow.AddMinutes(-2),
            FinishedTimestamp = DateTime.UtcNow
        };

    private static async Task<bool> InvokeProcessCompletedAsync(JobScheduler scheduler, JobRunRes run)
    {
        var method = typeof(JobScheduler).GetMethod("ProcessCompletedJobRunAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return await (Task<bool>)method.Invoke(scheduler, [run])!;
    }

    private static async Task<bool> InvokeOnDefinitionUpdatedAsync(JobScheduler scheduler, Guid definitionId)
    {
        var method = typeof(JobScheduler).GetMethod("OnDefinitionUpdatedAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return await (Task<bool>)method.Invoke(scheduler, [JsonSerializer.SerializeToUtf8Bytes(definitionId)])!;
    }

    private static IDictionary GetCachedJobs(JobScheduler scheduler)
        => (IDictionary)typeof(JobScheduler).GetField("_jobs", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(scheduler)!;
}
