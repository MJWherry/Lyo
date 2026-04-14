using Lyo.Common;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.MessageQueue.Tests;

internal sealed record TestRequest(string Id, string Payload);

internal sealed class TestQueueWorker : QueueWorkerBase<TestRequest, Result<TestRequest>>
{
    public readonly List<TestRequest> ProcessedRequests = new();

    public TestQueueWorker(IMqService mqService, string queueName)
        : base(mqService, queueName, NullLogger.Instance, null, new() { PropertyNameCaseInsensitive = true }) { }

    protected override Task<Result<TestRequest>> DoWorkAsync(TestRequest request, CancellationToken ct)
    {
        ProcessedRequests.Add(request);
        return Task.FromResult(Result<TestRequest>.Success(request));
    }
}