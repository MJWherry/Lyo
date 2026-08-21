using System.Text.Json;
using Lyo.Api.Client;
using Lyo.Common.Records;
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
/// Regression tests for the worker lifecycle review fixes: a Started CAS rejection (400) acks without requeue instead of redelivering forever, a late finish (run already
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

        // The run was already finalized (typically Timeout via dead-job detection) — retrying or requeueing can never succeed.
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

        // User cancellation (host token not signalled) still finishes the run as Cancelled — no requeue, no hand-back.
        Assert.True(result.IsSuccess);
        Assert.Equal(1, api.FinishAttempts);
        Assert.Equal(0, api.RequeueAttempts);
    }

    private static TestJobWorker CreateWorker(ControllableJobRunApiClient api, Func<IJobWorkerContext, Task> execute)
        => new(new FakeMqService(), new JobClient(api), new FakeJobEventPublisher(), execute);

    private sealed class TestJobWorker(IMqService mq, IJobClient jobClient, IJobEventPublisher events, Func<IJobWorkerContext, Task> execute)
        : JobWorkerBase(mq, jobClient, events, "cs", NullLogger.Instance)
    {
        protected override TimeSpan HeartbeatInterval => TimeSpan.FromHours(1);

        protected override Task ExecuteAsync(IJobWorkerContext ctx) => execute(ctx);

        public Task<Result<Unit>> InvokeDoWorkAsync(Guid runId, CancellationToken ct) => DoWorkAsync(runId, ct);
    }

    /// <summary>API client stub for the job-run lifecycle routes (get/started/finished/requeue) with controllable failures.</summary>
    private sealed class ControllableJobRunApiClient : IApiClient
    {
        private int _remainingTransientFinishFailures;

        public Guid RunId { get; } = Guid.NewGuid();

        public Exception? StartException { get; set; }

        public Exception? FinishException { get; set; }

        public int FailFinishTransientCount {
            set => _remainingTransientFinishFailures = value;
        }

        public int FinishAttempts { get; private set; }

        public int RequeueAttempts { get; private set; }

        public void Dispose() { }

        public JsonSerializerOptions GetSerializerOptions() => new();

        public HttpClient GetClient() => new();

        public Task<TResult?> GetAsAsync<TResult>(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
            => Task.FromResult((TResult?)(object?)BuildRun(JobState.Queued));

        public Task<TResult?> GetAsAsync<TRequest, TResult>(
            string uri,
            TRequest? query = default,
            string? enumerableDelimiter = null,
            Action<HttpRequestMessage>? before = null,
            CancellationToken ct = default)
            => Task.FromResult(default(TResult));

        public Task<TResult> PostAsAsync<TResult>(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        {
            if (uri.Contains("/Started", StringComparison.OrdinalIgnoreCase)) {
                if (StartException is not null)
                    throw StartException;

                return Task.FromResult((TResult)(object)BuildRun(JobState.Running));
            }

            if (uri.Contains("/Requeue", StringComparison.OrdinalIgnoreCase)) {
                RequeueAttempts++;
                return Task.FromResult((TResult)(object)BuildRun(JobState.Queued));
            }

            throw new NotImplementedException(uri);
        }

        public Task<TResult> PostAsAsync<TRequest, TResult>(string uri, TRequest? request = default, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        {
            if (uri.Contains("/Started", StringComparison.OrdinalIgnoreCase)) {
                if (StartException is not null)
                    throw StartException;

                return Task.FromResult((TResult)(object)BuildRun(JobState.Running));
            }

            if (uri.Contains("/Finished", StringComparison.OrdinalIgnoreCase)) {
                FinishAttempts++;
                if (FinishException is not null)
                    throw FinishException;

                if (_remainingTransientFinishFailures > 0) {
                    _remainingTransientFinishFailures--;
                    throw new HttpRequestException("Connection reset");
                }

                return Task.FromResult((TResult)(object)BuildRun(JobState.Finished));
            }

            return Task.FromResult(default(TResult)!);
        }

        public Task<TResult> PatchAsAsync<TRequest, TResult>(string uri, TRequest? request = default, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
            => Task.FromResult(default(TResult)!);

        public Task<TResult> PutAsAsync<TRequest, TResult>(string uri, TRequest? request = default, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
            => throw new NotImplementedException();

        public Task<byte[]> GetFileAsync(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default) => throw new NotImplementedException();

        public Task<(Stream Content, string? FileName, long? ContentLength)> GetFileStreamAsync(
            string uri,
            Action<HttpRequestMessage>? before = null,
            CancellationToken ct = default)
            => throw new NotImplementedException();

        public Task<(byte[] Content, FileTypeInfo FileType)> GetFileWithTypeAsync(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
            => throw new NotImplementedException();

        public Task<byte[]> PostAsBinaryAsync<TRequest>(string uri, TRequest? request = default, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
            => throw new NotImplementedException();

        public Task<TResult> PostFileAsAsync<TResult>(
            string uri,
            Stream stream,
            FileTypeInfo fileType,
            string? fileName = null,
            Action<HttpRequestMessage>? before = null,
            CancellationToken ct = default)
            => throw new NotImplementedException();

        public Task<TResult> PostFileAsAsync<TResult>(string uri, Stream stream, string fileName, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
            => throw new NotImplementedException();

        public Task<TResult> PostFileAsAsync<TResult>(
            string uri,
            byte[] data,
            FileTypeInfo fileType,
            string? fileName = null,
            Action<HttpRequestMessage>? before = null,
            CancellationToken ct = default)
            => throw new NotImplementedException();

        public Task<TResult> PostFileAsAsync<TResult>(string uri, byte[] data, string fileName, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
            => throw new NotImplementedException();

        public Task<TResult> PostFileAsAsync<TResult>(string uri, string filePath, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
            => throw new NotImplementedException();

        public Task<TResult> DeleteAsAsync<TRequest, TResult>(string uri, TRequest? request = default, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
            => throw new NotImplementedException();

        public Task<TResult> DeleteAsAsync<TResult>(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default) => throw new NotImplementedException();

        private JobRunRes BuildRun(JobState state)
            => new() {
                Id = RunId,
                State = state,
                CreatedTimestamp = DateTime.UtcNow,
                JobDefinitionId = Guid.NewGuid(),
                JobDefinition = new(Guid.NewGuid(), "TestDef", null, "Test", "cs", true, null, null, null, null),
                JobRunParameters = []
            };
    }
}