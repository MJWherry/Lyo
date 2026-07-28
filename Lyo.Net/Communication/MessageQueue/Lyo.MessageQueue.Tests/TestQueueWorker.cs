using Lyo.Result;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.MessageQueue.Tests;

internal sealed record TestRequest(string Id, string Payload);

internal sealed class TestQueueWorker : QueueWorkerBase<TestRequest, Result<TestRequest>>
{
    public readonly List<TestRequest> ProcessedRequests = [];

    public TestQueueWorker(IMqService mqService, string queueName)
        : base(mqService, queueName, NullLogger.Instance, null, new() { PropertyNameCaseInsensitive = true }) { }

    protected override Task<Result<TestRequest>> DoWorkAsync(TestRequest request, CancellationToken ct)
    {
        ProcessedRequests.Add(request);
        return Task.FromResult(Result<TestRequest>.Success(request));
    }
}

/// <summary>
/// Test worker with pluggable DoWork behavior and requeue/DLQ configuration for retry-cap and poison-message tests. The behavior func is invoked per message with the request
/// and the 1-based call count; thrown exceptions propagate to the worker like a real DoWork failure. When <c>maxRequeueCount</c> is null, the effective cap is resolved from
/// <c>workerOptions</c> the same way the DI registration path (<c>AddJobWorker</c>) does.
/// </summary>
internal sealed class ConfigurableTestQueueWorker : QueueWorkerBase<TestRequest, Result<TestRequest>>
{
    private readonly Func<TestRequest, int, Result<TestRequest>> _behavior;
    private int _callCount;

    public int CallCount => _callCount;

    public ConfigurableTestQueueWorker(
        IMqService mqService,
        string queueName,
        Func<TestRequest, int, Result<TestRequest>> behavior,
        int? maxRequeueCount = null,
        string? dlqName = null,
        QueueWorkerOptions? workerOptions = null)
        : base(mqService, queueName, NullLogger.Instance, null, new() { PropertyNameCaseInsensitive = true }, maxRequeueCount ?? workerOptions?.DefaultMaxRequeueCount, dlqName)
    {
        _behavior = behavior;

        // Mirror the DI registration path: RequeueDelay comes from options post-construction. Tests default to no delay to stay fast.
        RequeueDelay = workerOptions?.RequeueDelay;
    }

    protected override Task<Result<TestRequest>> DoWorkAsync(TestRequest request, CancellationToken ct)
    {
        var count = Interlocked.Increment(ref _callCount);
        return Task.FromResult(_behavior(request, count));
    }
}