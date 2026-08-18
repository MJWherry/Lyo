using System.Text.Json;
using Lyo.Api.Client;
using Lyo.Api.Models.Common.Response;
using Lyo.Common.Records;
using Lyo.Health;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Events;
using Lyo.Job.Models.Request;
using Lyo.Job.Models.Response;
using Lyo.MessageQueue;

namespace Lyo.Job.Scheduler.Tests;

/// <summary>Answers the scheduler's API calls in memory: definition query, batch latest-runs, blackout calendars, and run creation (recorded).</summary>
internal sealed class FakeSchedulerApiClient : IApiClient
{
    private readonly JobDefinitionRes _definition;

    public List<JobRunReq> CreatedRunRequests { get; } = [];

    public bool Return404ForDefinitionGet { get; set; }

    /// <summary>When set, <c>GET Job/Run/{id}</c> throws <see cref="ApiException" /> with this status instead of returning null.</summary>
    public int? ThrowStatusOnRunGet { get; set; }

    public FakeSchedulerApiClient(JobDefinitionRes definition) => _definition = definition;

    public void Dispose() { }

    public JsonSerializerOptions GetSerializerOptions() => new();

    public HttpClient GetClient() => new();

    public Task<TResult> PostAsAsync<TRequest, TResult>(string uri, TRequest? request = default, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
    {
        if (uri.Contains("Definition/QueryConcrete", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult((TResult)(object)BuildQueryRes<JobDefinitionRes>([_definition]));

        if (uri.Contains("Definition/LatestRuns", StringComparison.OrdinalIgnoreCase)) {
            var latest = new List<JobDefinitionLatestRunsRes> { new() { JobDefinitionId = _definition.Id } };
            return Task.FromResult((TResult)(object)latest);
        }

        if (uri.Contains("BlackoutCalendar/QueryConcrete", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult((TResult)(object)BuildQueryRes<JobBlackoutCalendarRes>([]));

        if (uri.Contains("Run/Create", StringComparison.OrdinalIgnoreCase)) {
            var runReq = (JobRunReq)(object)request!;
            CreatedRunRequests.Add(runReq);
            var created = new JobRunRes {
                Id = Guid.NewGuid(),
                JobDefinitionId = runReq.JobDefinitionId,
                State = JobState.Queued,
                CreatedTimestamp = DateTime.UtcNow,
                RetryAttempt = runReq.RetryAttempt
            };

            return Task.FromResult((TResult)(object)new CreateResult<JobRunRes>(true, created, null));
        }

        if (uri.Contains("Run/QueryConcrete", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult((TResult)(object)BuildQueryRes<JobRunRes>([]));

        throw new NotImplementedException(uri);
    }

    public Task<TResult?> GetAsAsync<TResult>(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
    {
        if (uri.Contains("Job/Definition/", StringComparison.OrdinalIgnoreCase)) {
            if (Return404ForDefinitionGet)
                throw new ApiException(404, "Definition not found");

            return Task.FromResult((TResult?)(object?)_definition);
        }

        if (uri.Contains("Job/Run/", StringComparison.OrdinalIgnoreCase) && ThrowStatusOnRunGet is { } status)
            throw new ApiException(status, $"Run GET failed: {status}");

        return Task.FromResult(default(TResult));
    }

    public Task<TResult> PatchAsAsync<TRequest, TResult>(string uri, TRequest? request = default, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        => Task.FromResult(default(TResult)!);

    public Task<TResult?> GetAsAsync<TRequest, TResult>(
        string uri,
        TRequest? query = default,
        string? enumerableDelimiter = null,
        Action<HttpRequestMessage>? before = null,
        CancellationToken ct = default)
        => Task.FromResult(default(TResult));

    public Task<TResult> PostAsAsync<TResult>(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default) => throw new NotImplementedException(uri);

    public Task<TResult> PutAsAsync<TRequest, TResult>(string uri, TRequest? request = default, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<byte[]> GetFileAsync(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default) => throw new NotImplementedException();

    public Task<(Stream Content, string? FileName, long? ContentLength)> GetFileStreamAsync(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
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

    private static QueryRes<T> BuildQueryRes<T>(IReadOnlyList<T> items) => new(new(), true, items, 0, items.Count, items.Count, false, 0, null);
}

/// <summary>Minimal in-memory event publisher — always connected, records nothing the tests need.</summary>
internal sealed class FakeEventPublisher : IJobEventPublisher
{
    public bool IsConnected() => true;

    public Task SetupAsync(CancellationToken ct = default) => Task.CompletedTask;

    public Task PublishRunCreatedAsync(Guid runId, string workerType, int priority = 0, CancellationToken ct = default) => Task.CompletedTask;

    public Task PublishRunStartedAsync(Guid runId, CancellationToken ct = default) => Task.CompletedTask;

    public Task PublishRunFinishedAsync(Guid runId, CancellationToken ct = default) => Task.CompletedTask;

    public Task PublishRunCancelledAsync(Guid runId, CancellationToken ct = default) => Task.CompletedTask;

    public Task PublishDefinitionUpdatedAsync(Guid definitionId, CancellationToken ct = default) => Task.CompletedTask;

    public Task PublishAlertAsync(Guid definitionId, Guid? runId, JobAlertType alertType, string message, CancellationToken ct = default) => Task.CompletedTask;

    public Task SubscribeToDefinitionUpdatesAsync(string subscriberQueueName, Func<byte[], Task<bool>> handler, CancellationToken ct = default) => Task.CompletedTask;

    public Task SubscribeToRunCompletionsAsync(Func<byte[], Task<bool>> handler, CancellationToken ct = default) => Task.CompletedTask;

    public Task SubscribeToRunCancellationsAsync(string workerType, Func<Guid, Task> handler, CancellationToken ct = default) => Task.CompletedTask;
}

/// <summary>Fake MQ implementing <see cref="IDelayedMqService" /> so the scheduler chooses the delayed-envelope dispatch path; records the delayed sends.</summary>
internal sealed class RecordingDelayedMqService : IMqService, IDelayedMqService
{
    public List<(string QueueName, TimeSpan Delay, byte[] Data)> DelayedSends { get; } = [];

    public List<(string QueueName, byte[] Data)> QueueSends { get; } = [];

    public Task<bool> SendToQueueDelayed(string queueName, byte[] data, TimeSpan delay, CancellationToken ct = default)
    {
        DelayedSends.Add((queueName, delay, data));
        return Task.FromResult(true);
    }

    public bool IsConnected() => true;

    public Task ConnectAsync(CancellationToken ct = default) => Task.CompletedTask;

    public Task DisconnectAsync(CancellationToken ct = default) => Task.CompletedTask;

    public Task<bool> CreateQueue(
        string queueName,
        bool durable = true,
        bool exclusive = false,
        bool autoDelete = false,
        IDictionary<string, object>? arguments = null,
        CancellationToken ct = default)
        => Task.FromResult(true);

    public Task<bool> DeleteQueue(string queueName, bool ifUnused = false, bool ifEmpty = false, CancellationToken ct = default) => Task.FromResult(true);

    public Task<bool> ClearQueue(string queueName, CancellationToken ct = default) => Task.FromResult(true);

    public Task<bool> BindQueueToExchange(string queueName, string exchangeName, string routingKey, CancellationToken ct = default) => Task.FromResult(true);

    public Task<bool> SendToQueue(string queueName, byte[] data)
    {
        QueueSends.Add((queueName, data));
        return Task.FromResult(true);
    }

    public Task<bool> SendToExchange(string exchangeName, string routingKey, byte[] data) => Task.FromResult(true);

    public Task<IReadOnlyList<QueuePeekMessage>> PeekQueueMessages(string queueName, int maxMessages = 10, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<QueuePeekMessage>>([]);

    public Task<bool> SubscribeToQueue(string queueName, Func<byte[], Task<bool>> onMessage, CancellationToken ct = default) => Task.FromResult(true);

    public string HealthCheckName => "recording-delayed-mq";

    public Task<HealthResult> CheckHealthAsync(CancellationToken ct = default) => Task.FromResult(HealthResult.Healthy(TimeSpan.Zero, null, new Dictionary<string, object?>()));
}