using System.Text.Json;
using Lyo.Api.Client;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Lyo.Common.Records;
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

public class JobWorkerBaseRegistrationTests
{
    [Fact]
    public async Task StartAsync_WhenApiUnavailable_RetriesRegistrationOnHeartbeatInterval()
    {
        var api = new ControllableWorkerInstanceApiClient { FailRegisterCount = 1 };
        var worker = CreateWorker(api);
        try {
            await worker.StartAsync(TestContext.Current.CancellationToken);
            Assert.True(worker.IsRunning);
            Assert.Equal(1, api.RegisterAttempts);
            Assert.Equal(0, api.SuccessfulRegisters);
            await WaitUntilAsync(() => api.SuccessfulRegisters >= 1 && api.HeartbeatCount >= 1, TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            Assert.True(api.RegisterAttempts >= 2);
            Assert.Equal(1, api.SuccessfulRegisters);
            Assert.True(api.HeartbeatCount >= 1);
            AssertRegisteredMetadata(api.LastRegisterRequest?.Metadata);
        }
        finally {
            await worker.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task Heartbeat_WhenInstanceNotFound_ReRegisters()
    {
        var api = new ControllableWorkerInstanceApiClient { FailHeartbeat404Count = 1 };
        var worker = CreateWorker(api);
        try {
            await worker.StartAsync(TestContext.Current.CancellationToken);
            Assert.Equal(1, api.SuccessfulRegisters);
            await WaitUntilAsync(() => api.SuccessfulRegisters >= 2 && api.HeartbeatCount >= 1, TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            Assert.True(api.RegisterAttempts >= 2);
            Assert.Equal(2, api.SuccessfulRegisters);
            Assert.True(api.Heartbeat404Count >= 1);
            Assert.True(api.HeartbeatCount >= 1);
        }
        finally {
            await worker.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task StartAsync_RegistersSystemInfoAndQueueSubscriptions()
    {
        var api = new ControllableWorkerInstanceApiClient();
        var worker = CreateWorker(api);
        try {
            await worker.StartAsync(TestContext.Current.CancellationToken);
            AssertRegisteredMetadata(api.LastRegisterRequest?.Metadata);
        }
        finally {
            await worker.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    private static TestJobWorker CreateWorker(ControllableWorkerInstanceApiClient api) => new(new FakeMqService(), new JobClient(api), new FakeJobEventPublisher(), "cs");

    private static void AssertRegisteredMetadata(IReadOnlyDictionary<string, string?>? metadata)
    {
        Assert.NotNull(metadata);
        Assert.False(string.IsNullOrWhiteSpace(metadata[Constants.WorkerMetadata.Os]));
        Assert.False(string.IsNullOrWhiteSpace(metadata[Constants.WorkerMetadata.Framework]));
        Assert.True(int.Parse(metadata[Constants.WorkerMetadata.ProcessorCount]!) > 0);
        Assert.Equal(Constants.Mq.QueueGetJobRunCreated("cs"), metadata[Constants.WorkerMetadata.Queue]);
        Assert.Equal(Constants.Mq.QueueGetJobRunCreatedWait("cs"), metadata[Constants.WorkerMetadata.WaitQueue]);
        Assert.StartsWith(Constants.Mq.QueueGetJobRunCancel("cs") + ".", metadata[Constants.WorkerMetadata.CancelQueue], StringComparison.Ordinal);
        Assert.Contains(metadata[Constants.WorkerMetadata.Queue]!, metadata[Constants.WorkerMetadata.Subscriptions], StringComparison.Ordinal);
        Assert.Contains(Constants.WorkerMetadata.WorkingSetBytes, metadata.Keys);
        Assert.Contains(Constants.WorkerMetadata.GcHeapBytes, metadata.Keys);
    }

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout, CancellationToken ct)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (!condition()) {
            if (DateTime.UtcNow >= deadline)
                throw new TimeoutException("Condition was not met within the timeout.");

            await Task.Delay(20, ct);
        }
    }

    private sealed class TestJobWorker(IMqService mq, IJobClient jobClient, IJobEventPublisher events, string workerType)
        : JobWorkerBase(mq, jobClient, events, workerType, NullLogger.Instance)
    {
        protected override TimeSpan HeartbeatInterval => TimeSpan.FromMilliseconds(50);

        protected override Task ExecuteAsync(IJobWorkerContext ctx) => Task.CompletedTask;
    }

    /// <summary>API client that simulates Job WorkerInstance register/heartbeat failures for recovery tests.</summary>
    private sealed class ControllableWorkerInstanceApiClient : IApiClient
    {
        private int _remainingHeartbeat404s;
        private int _remainingRegisterFailures;

        public int FailRegisterCount {
            set => _remainingRegisterFailures = value;
        }

        public int FailHeartbeat404Count {
            set => _remainingHeartbeat404s = value;
        }

        public int RegisterAttempts { get; private set; }

        public int SuccessfulRegisters { get; private set; }

        public int HeartbeatCount { get; private set; }

        public int Heartbeat404Count { get; private set; }

        public JobWorkerInstanceReq? LastRegisterRequest { get; private set; }

        public void Dispose() { }

        public JsonSerializerOptions GetSerializerOptions() => new();

        public HttpClient GetClient() => new();

        public Task<TResult?> GetAsAsync<TRequest, TResult>(
            string uri,
            TRequest? query = default,
            string? enumerableDelimiter = null,
            Action<HttpRequestMessage>? before = null,
            CancellationToken ct = default)
            => Task.FromResult(default(TResult));

        public Task<TResult?> GetAsAsync<TResult>(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default) => Task.FromResult(default(TResult));

        public Task<byte[]> GetFileAsync(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default) => throw new NotImplementedException();

        public Task<(Stream Content, string? FileName, long? ContentLength)> GetFileStreamAsync(
            string uri,
            Action<HttpRequestMessage>? before = null,
            CancellationToken ct = default)
            => throw new NotImplementedException();

        public Task<(byte[] Content, FileTypeInfo FileType)> GetFileWithTypeAsync(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
            => throw new NotImplementedException();

        public Task<TResult> PutAsAsync<TRequest, TResult>(string uri, TRequest? request = default, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
            => throw new NotImplementedException();

        public Task<TResult> PatchAsAsync<TRequest, TResult>(string uri, TRequest? request = default, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        {
            if (!IsWorkerInstanceRoute(uri))
                return Task.FromResult(default(TResult)!);

            // StopAsync patches State=Stopped; do not count as heartbeat and do not inject 404s.
            if (request is PatchRequest patch && patch.Properties.TryGetValue("State", out var state) && Equals(state, JobWorkerInstanceState.Stopped))
                return Task.FromResult(default(TResult)!);

            if (_remainingHeartbeat404s > 0) {
                _remainingHeartbeat404s--;
                Heartbeat404Count++;
                throw new ApiException(404, "Worker instance not found");
            }

            HeartbeatCount++;
            return Task.FromResult(default(TResult)!);
        }

        public Task<TResult> PostAsAsync<TRequest, TResult>(string uri, TRequest? request = default, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        {
            if (!IsWorkerInstanceRoute(uri))
                return Task.FromResult(default(TResult)!);

            RegisterAttempts++;
            if (_remainingRegisterFailures > 0) {
                _remainingRegisterFailures--;
                throw new HttpRequestException("Connection refused (localhost:5074)");
            }

            var id = Guid.NewGuid();
            SuccessfulRegisters++;
            var now = DateTime.UtcNow;
            LastRegisterRequest = request as JobWorkerInstanceReq;
            var created = new CreateResult<JobWorkerInstanceRes>(
                true,
                new() {
                    Id = id,
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

        public Task<TResult> PostAsAsync<TResult>(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default) => throw new NotImplementedException();

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

        private static bool IsWorkerInstanceRoute(string uri) => uri.Contains("WorkerInstance", StringComparison.OrdinalIgnoreCase);
    }
}