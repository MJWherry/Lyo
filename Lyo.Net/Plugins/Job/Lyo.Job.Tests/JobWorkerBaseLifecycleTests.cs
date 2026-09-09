using Lyo.Api.Client;
using Lyo.Api.Models.Common.Request;
using Lyo.Job.Client;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Events;
using Lyo.Job.Models.Response;
using Lyo.Job.Tests.Postgres;
using Lyo.Job.Worker;
using Lyo.MessageQueue;
using Lyo.Result;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Job.Tests;

/// <summary>
/// Regression checks for the worker lifecycle review fixes: a Started CAS rejection (400) acks without requeue instead of redelivering forever, a late finish (run already
/// finalized by dead-job detection) is dropped cleanly, transient finish failures are retried, and host shutdown hands the run back to Queued via the requeue endpoint.
/// </summary>
public class JobWorkerBaseLifecycleTests
{
    [Fact]
    public async Task DoWork_WhenStartRejectedWith400_FailsWithoutRequeue()
    {
        var api = new ControllableJobRunApiClient { StartException = new ApiException(400, "Run not in Queued state") };
        var worker = CreateWorker(api, _ => Task.CompletedTask);
        var result = await worker.InvokeDoWorkAsync(api.RunId, TestContext.Current.CancellationToken);

        // Duplicate dispatch delivery: requeueing would be rejected the same way every time, churning the message to the DLQ.
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Metadata);
        Assert.Equal(false, result.Metadata!["requeue"]);
        Assert.Equal(0, api.FinishAttempts);
    }

    [Fact]
    public async Task DoWork_WhenStartFailsTransiently_FailsWithRequeue()
    {
        var api = new ControllableJobRunApiClient { StartException = new HttpRequestException("Connection refused") };
        var worker = CreateWorker(api, _ => Task.CompletedTask);
        var result = await worker.InvokeDoWorkAsync(api.RunId, TestContext.Current.CancellationToken);

        // The run is still Queued, so the counted requeue may retry it.
        Assert.False(result.IsSuccess);
        Assert.True(result.Metadata is null || !result.Metadata.ContainsKey("requeue") || Equals(result.Metadata["requeue"], true));
    }

    [Fact]
    public async Task DoWork_WhenFinishRejectedWith400_DropsLateFinishWithoutRetryOrRequeue()
    {
        var api = new ControllableJobRunApiClient { FinishException = new ApiException(400, "Run is not finishable") };
        var worker = CreateWorker(api, _ => Task.CompletedTask);
        var result = await worker.InvokeDoWorkAsync(api.RunId, TestContext.Current.CancellationToken);

        // The run was already finalized (typically Timeout via dead-job detection). retrying or requeueing can never succeed.
        Assert.False(result.IsSuccess);
        Assert.Equal(false, result.Metadata!["requeue"]);
        Assert.Equal(1, api.FinishAttempts);
    }

    [Fact]
    public async Task DoWork_WhenFinishFailsTransiently_RetriesAndSucceeds()
    {
        var api = new ControllableJobRunApiClient { FailFinishTransientCount = 2 };
        var worker = CreateWorker(api, _ => Task.CompletedTask);
        var result = await worker.InvokeDoWorkAsync(api.RunId, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess);
        Assert.Equal(3, api.FinishAttempts);
    }

    [Fact]
    public async Task DoWork_WhenHostShutsDownMidExecution_RequeuesRunAndRethrows()
    {
        var api = new ControllableJobRunApiClient();
        using var hostShutdown = new CancellationTokenSource();
        var worker = CreateWorker(
            api, _ => {
                // Simulate the host stopping while the job is executing.
                hostShutdown.Cancel();
                throw new OperationCanceledException(hostShutdown.Token);
            });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => worker.InvokeDoWorkAsync(api.RunId, hostShutdown.Token));

        // The run is handed back to Queued for redelivery instead of being terminally cancelled or finished.
        Assert.Equal(1, api.RequeueAttempts);
        Assert.Equal(0, api.FinishAttempts);
    }

    [Fact]
    public async Task DoWork_WhenRunCancelledByUser_ReportsCancelledFinish()
    {
        var api = new ControllableJobRunApiClient();
        var worker = CreateWorker(api, ctx => throw new OperationCanceledException(ctx.CancellationToken));
        var result = await worker.InvokeDoWorkAsync(api.RunId, TestContext.Current.CancellationToken);

        // User cancellation (host token not signalled) still finishes the run as Cancelled. no requeue, no hand-back.
        Assert.True(result.IsSuccess);
        Assert.Equal(1, api.FinishAttempts);
        Assert.Equal(0, api.RequeueAttempts);
    }

    [Fact]
    public async Task DoWork_WhenRunDoesNotExist_AcksWithoutRequeue()
    {
        var api = new ControllableJobRunApiClient { GetException = new ApiException(404, "Not found") };
        var worker = CreateWorker(api, _ => Task.CompletedTask);
        var result = await worker.InvokeDoWorkAsync(api.RunId, TestContext.Current.CancellationToken);

        // A deleted or retention-purged run can never be found by redelivery, so it cannot burn the requeue budget.
        Assert.False(result.IsSuccess);
        Assert.Equal(false, result.Metadata!["requeue"]);
        Assert.Equal(0, api.FinishAttempts);
    }

    [Fact]
    public async Task DoWork_WhenRunFetchFailsTransiently_LeavesTheMessageRequeueable()
    {
        var api = new ControllableJobRunApiClient { GetException = new HttpRequestException("Connection refused") };
        var worker = CreateWorker(api, _ => Task.CompletedTask);
        var result = await worker.InvokeDoWorkAsync(api.RunId, TestContext.Current.CancellationToken);
        Assert.False(result.IsSuccess);
        Assert.True(result.Metadata is null || !result.Metadata.ContainsKey("requeue"));
    }

    [Fact]
    public async Task DoWork_WhenShutdownHandBackFails_AcksInsteadOfStrandingTheRun()
    {
        var api = new ControllableJobRunApiClient { RequeueException = new ApiException(400, "Run is not Running") };
        using var hostShutdown = new CancellationTokenSource();
        var worker = CreateWorker(
            api, _ => {
                hostShutdown.Cancel();
                throw new OperationCanceledException(hostShutdown.Token);
            });

        // Rethrowing here returned the message to the broker while the row stayed Running: redelivery is rejected by the Started CAS and
        // nothing ever owns the run again. Ack instead and let dead-job detection finalize it.
        var result = await worker.InvokeDoWorkAsync(api.RunId, hostShutdown.Token);
        Assert.False(result.IsSuccess);
        Assert.Equal(false, result.Metadata!["requeue"]);
        Assert.Equal(1, api.RequeueAttempts);
    }

    /// <summary>
    /// The worker is a singleton and <c>ProcessingLimit</c> may allow several runs at once, so progress used to live on shared instance fields and one run's heartbeat could
    /// report another run's percentage.
    /// </summary>
    [Fact]
    public async Task ConcurrentRuns_DoNotCrossReportProgress()
    {
        var api = new ControllableJobRunApiClient();
        var runA = Guid.NewGuid();
        var runB = Guid.NewGuid();
        var expected = new Dictionary<Guid, int> { [runA] = 10, [runB] = 90 };
        using var bothReported = new SemaphoreSlim(0, 2);
        var worker = CreateWorker(
            api, async ctx => {
                await ctx.ReportProgressAsync(expected[ctx.Run.Id], $"run {ctx.Run.Id}", ctx.CancellationToken);
                bothReported.Release();

                // Hold both runs open long enough for several heartbeats to fire under the shared-state bug.
                await Task.Delay(TimeSpan.FromMilliseconds(250), ctx.CancellationToken);
            }, TimeSpan.FromMilliseconds(25));

        await Task.WhenAll(
            worker.InvokeDoWorkAsync(runA, TestContext.Current.CancellationToken),
            worker.InvokeDoWorkAsync(runB, TestContext.Current.CancellationToken));

        Assert.NotEmpty(api.ProgressPatches);
        Assert.All(api.ProgressPatches, patch => Assert.Equal(expected[patch.RunId], patch.Percent));
    }

    private static TestJobWorker CreateWorker(ControllableJobRunApiClient api, Func<IJobWorkerContext, Task> execute, TimeSpan? heartbeatInterval = null)
        => new(new FakeMqService(), new JobClient(api), new FakeJobEventPublisher(), execute, heartbeatInterval ?? TimeSpan.FromHours(1));

    private sealed class TestJobWorker(
        IMqService mq,
        IJobClient jobClient,
        IJobEventPublisher events,
        Func<IJobWorkerContext, Task> execute,
        TimeSpan heartbeatInterval)
        : JobWorkerBase(mq, jobClient, events, "cs", NullLogger.Instance)
    {
        protected override TimeSpan HeartbeatInterval => heartbeatInterval;

        protected override Task ExecuteAsync(IJobWorkerContext ctx) => execute(ctx);

        public Task<Result<Unit>> InvokeDoWorkAsync(Guid runId, CancellationToken ct) => DoWorkAsync(runId, ct);
    }

    /// <summary>API client stub for the job-run lifecycle routes (get/started/finished/requeue) with controllable failures.</summary>
    private sealed class ControllableJobRunApiClient : StubApiClient
    {
        private int _remainingTransientFinishFailures;

        public Guid RunId { get; } = Guid.NewGuid();

        public Exception? StartException { get; set; }

        public Exception? FinishException { get; set; }

        public Exception? GetException { get; set; }

        public Exception? RequeueException { get; set; }

        /// <summary>Every run patch that carried a progress percentage, so a test can prove concurrent runs never report each other's value.</summary>
        public List<(Guid RunId, int Percent)> ProgressPatches { get; } = [];

        public int FailFinishTransientCount {
            set => _remainingTransientFinishFailures = value;
        }

        public int FinishAttempts { get; private set; }

        public int RequeueAttempts { get; private set; }

        public override Task<TResult?> GetAsAsync<TResult>(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
            where TResult : default
        {
            if (GetException is not null)
                throw GetException;

            return Task.FromResult((TResult?)(object?)BuildRun(JobState.Queued, ExtractRunId(uri)));
        }

        public override Task<TResult> PostAsAsync<TResult>(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        {
            if (uri.Contains("/Started", StringComparison.OrdinalIgnoreCase)) {
                if (StartException is not null)
                    throw StartException;

                return Task.FromResult((TResult)(object)BuildRun(JobState.Running, ExtractRunId(uri)));
            }

            if (uri.Contains("/Requeue", StringComparison.OrdinalIgnoreCase)) {
                RequeueAttempts++;
                if (RequeueException is not null)
                    throw RequeueException;

                return Task.FromResult((TResult)(object)BuildRun(JobState.Queued, ExtractRunId(uri)));
            }

            throw new NotImplementedException(uri);
        }

        public override Task<TResult> PostAsAsync<TRequest, TResult>(string uri, TRequest? request = default, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
            where TRequest : default
        {
            if (uri.Contains("/Started", StringComparison.OrdinalIgnoreCase)) {
                if (StartException is not null)
                    throw StartException;

                return Task.FromResult((TResult)(object)BuildRun(JobState.Running, ExtractRunId(uri)));
            }

            if (uri.Contains("/Finished", StringComparison.OrdinalIgnoreCase)) {
                FinishAttempts++;
                if (FinishException is not null)
                    throw FinishException;

                if (_remainingTransientFinishFailures > 0) {
                    _remainingTransientFinishFailures--;
                    throw new HttpRequestException("Connection reset");
                }

                return Task.FromResult((TResult)(object)BuildRun(JobState.Finished, ExtractRunId(uri)));
            }

            return Task.FromResult(default(TResult)!);
        }

        public override Task<TResult> PatchAsAsync<TRequest, TResult>(string uri, TRequest? request = default, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
            where TRequest : default
        {
            if (request is PatchRequest patch && patch.Properties.TryGetValue("ProgressPercent", out var raw) && raw is int percent) {
                lock (ProgressPatches)
                    ProgressPatches.Add((ExtractRunId(uri), percent));
            }

            return Task.FromResult(default(TResult)!);
        }

        private static Guid ExtractRunId(string uri)
        {
            foreach (var segment in uri.Split('?')[0].Split('/')) {
                if (Guid.TryParse(segment, out var id))
                    return id;
            }

            return Guid.Empty;
        }

        private JobRunRes BuildRun(JobState state, Guid? runId = null)
            => new() {
                Id = runId is { } id && id != Guid.Empty ? id : RunId,
                State = state,
                CreatedTimestamp = DateTime.UtcNow,
                JobDefinitionId = Guid.NewGuid(),
                JobDefinition = new(Guid.NewGuid(), "TestDef", null, "Test", "cs", true, null, null, null, null),
                JobRunParameters = []
            };
    }
}