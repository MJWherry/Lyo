using Lyo.Job.Models.Events;

namespace Lyo.Job.Postgres.Tests;

/// <summary>
/// In-memory <see cref="IJobEventPublisher" /> for integration tests. Captures every publish call so tests can assert what was sent, and exposes <see cref="SetConnected" />
/// to simulate a disconnected transport.
/// </summary>
public sealed class FakeJobEventPublisher : IJobEventPublisher
{
    private bool _connected = true;

    public List<(string Event, Guid RunId)> Published { get; } = [];

    public bool IsConnected() => _connected;

    public Task SetupAsync(CancellationToken ct = default) => Task.CompletedTask;

    public Task PublishRunCreatedAsync(Guid runId, string workerType, CancellationToken ct = default)
    {
        Published.Add(("RunCreated", runId));
        return Task.CompletedTask;
    }

    public Task PublishRunStartedAsync(Guid runId, CancellationToken ct = default)
    {
        Published.Add(("RunStarted", runId));
        return Task.CompletedTask;
    }

    public Task PublishRunFinishedAsync(Guid runId, CancellationToken ct = default)
    {
        Published.Add(("RunFinished", runId));
        return Task.CompletedTask;
    }

    public Task PublishRunCancelledAsync(Guid runId, CancellationToken ct = default)
    {
        Published.Add(("RunCancelled", runId));
        return Task.CompletedTask;
    }

    public Task PublishDefinitionUpdatedAsync(Guid definitionId, CancellationToken ct = default)
    {
        Published.Add(("DefinitionUpdated", definitionId));
        return Task.CompletedTask;
    }

    public Task SubscribeToDefinitionUpdatesAsync(string subscriberQueueName, Func<byte[], Task<bool>> handler, CancellationToken ct = default) => Task.CompletedTask;

    public Task SubscribeToRunCompletionsAsync(Func<byte[], Task<bool>> handler, CancellationToken ct = default) => Task.CompletedTask;

    public Task SubscribeToRunCancellationsAsync(string workerType, Func<Guid, Task> handler, CancellationToken ct = default) => Task.CompletedTask;

    public void SetConnected(bool value) => _connected = value;
}