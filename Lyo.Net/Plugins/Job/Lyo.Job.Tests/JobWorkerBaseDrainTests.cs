using System.Text.Json;
using Lyo.Api.Client;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Lyo.Common.Metadata.Records;
using Lyo.Job.Client;
using Lyo.Job.Models;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Events;
using Lyo.Job.Models.Request;
using Lyo.Job.Models.Response;
using Lyo.Job.Tests.Postgres;
using Lyo.Job.Worker;
using Lyo.MessageQueue;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Job.Tests;

/// <summary>
/// Regression checks for the shutdown ordering fix: the worker drains in-flight runs first, then stops heartbeating, then deregisters. Deregistering first published a
/// <c>Stopped</c> instance while runs were still executing, and stopping the heartbeat first let stale-worker pruning delete a registration that was still draining.
/// </summary>
public class JobWorkerBaseDrainTests
{
    [Fact]
    public async Task StopAsync_WaitsForInFlightRunsBeforeDeregistering()
    {
        var mq = new FakeMqService();
        var api = new GatedJobApiClient();
        var worker = new GatedJobWorker(mq, new JobClient(api), new FakeJobEventPublisher());
        await worker.StartAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, api.RegisterCount);

        var handler = mq.GetHandler(Constants.Mq.QueueGetJobRunCreated("cs"));
        Assert.NotNull(handler);
        var delivery = Task.Run(() => handler!(Envelope(api.RunId)), TestContext.Current.CancellationToken);
        await WaitUntilAsync(() => worker.ExecutionStarted, TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        var stopping = worker.StopAsync(CancellationToken.None);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        Assert.False(stopping.IsCompleted);
        Assert.Equal(0, api.DeregisterCount);

        worker.ReleaseExecution();
        await stopping;
        await delivery;
        Assert.Equal(1, api.DeregisterCount);
        Assert.Equal(0, worker.InFlightCount);
    }

    [Fact]
    public async Task StopAsync_WithNothingInFlight_DeregistersImmediately()
    {
        var mq = new FakeMqService();
        var api = new GatedJobApiClient();
        var worker = new GatedJobWorker(mq, new JobClient(api), new FakeJobEventPublisher());
        await worker.StartAsync(TestContext.Current.CancellationToken);
        await worker.StopAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, api.DeregisterCount);
        Assert.False(worker.IsRunning);
    }

    private static byte[] Envelope(Guid runId)
        => JsonSerializer.SerializeToUtf8Bytes(new QueueMessageEnvelope<Guid>(runId, 0, runId.ToString("D"), DateTime.UtcNow));

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout, CancellationToken ct)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (!condition()) {
            if (DateTime.UtcNow >= deadline)
                throw new TimeoutException("Condition was not met within the timeout.");

            await Task.Delay(20, ct);
        }
    }

    /// <summary>Worker whose execution blocks until the test releases it, so a run can be held in flight across shutdown.</summary>
    private sealed class GatedJobWorker(IMqService mq, IJobClient jobClient, IJobEventPublisher events)
        : JobWorkerBase(mq, jobClient, events, "cs", NullLogger.Instance)
    {
        private readonly TaskCompletionSource _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool ExecutionStarted { get; private set; }

        protected override TimeSpan HeartbeatInterval => TimeSpan.FromHours(1);

        public void ReleaseExecution() => _gate.TrySetResult();

        protected override async Task ExecuteAsync(IJobWorkerContext ctx)
        {
            ExecutionStarted = true;

            // Deliberately not cancellable: the drain has to wait for work that ignores the shutdown signal.
            await _gate.Task;
        }
    }

    /// <summary>API client stub covering worker registration/deregistration plus the run lifecycle routes the drain path touches.</summary>
    private sealed class GatedJobApiClient : StubApiClient
    {
        public Guid RunId { get; } = Guid.NewGuid();

        public int RegisterCount { get; private set; }

        public int DeregisterCount { get; private set; }

        public override Task<TResult?> GetAsAsync<TResult>(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
            where TResult : default
            => Task.FromResult((TResult?)(object?)BuildRun(JobState.Queued));

        public override Task<TResult?> GetAsAsync<TRequest, TResult>(
            string uri,
            TRequest? query = default,
            string? enumerableDelimiter = null,
            Action<HttpRequestMessage>? before = null,
            CancellationToken ct = default)
            where TRequest : default
            where TResult : default
            => Task.FromResult(default(TResult));

        public override Task<TResult> PostAsAsync<TResult>(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
            => Task.FromResult((TResult)(object)BuildRun(JobState.Running));

        public override Task<TResult> PostAsAsync<TRequest, TResult>(string uri, TRequest? request = default, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
            where TRequest : default
        {
            if (uri.Contains("WorkerInstance", StringComparison.OrdinalIgnoreCase)) {
                RegisterCount++;
                var now = DateTime.UtcNow;
                var created = new CreateResult<JobWorkerInstanceRes>(
                    true,
                    new() {
                        Id = Guid.NewGuid(),
                        WorkerType = "cs",
                        MachineName = "host",
                        ProcessId = 1,
                        State = JobWorkerInstanceState.Running,
                        InFlightCount = 0,
                        StartedTimestamp = now,
                        LastHeartbeatUtc = now,
                        CreatedTimestamp = now
                    },
                    null);

                return Task.FromResult((TResult)(object)created);
            }

            if (uri.Contains("/Finished", StringComparison.OrdinalIgnoreCase))
                return Task.FromResult((TResult)(object)BuildRun(JobState.Finished));

            return Task.FromResult((TResult)(object)BuildRun(JobState.Running));
        }

        public override Task<TResult> PatchAsAsync<TRequest, TResult>(string uri, TRequest? request = default, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
            where TRequest : default
        {
            if (uri.Contains("WorkerInstance", StringComparison.OrdinalIgnoreCase) && request is PatchRequest patch &&
                patch.Properties.TryGetValue("State", out var state) && Equals(state, JobWorkerInstanceState.Stopped))
                DeregisterCount++;

            return Task.FromResult(default(TResult)!);
        }

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
