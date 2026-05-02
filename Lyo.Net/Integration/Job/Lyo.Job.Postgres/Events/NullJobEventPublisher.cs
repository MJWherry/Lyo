using Lyo.Job.Models.Events;

namespace Lyo.Job.Postgres.Events;

/// <summary>
/// No-op <see cref="IJobEventPublisher"/> registered by default when no message-queue transport is
/// configured. Always reports as disconnected so <see cref="JobService"/> operations that require
/// the transport will return an appropriate error rather than throwing.
/// Override by calling <c>AddMqJobEventPublisher()</c> (or registering your own implementation)
/// after <c>AddPostgresJobManagement()</c>.
/// </summary>
internal sealed class NullJobEventPublisher : IJobEventPublisher
{
    public bool IsConnected() => false;
    public Task SetupAsync(CancellationToken ct = default) => Task.CompletedTask;
    public Task PublishRunCreatedAsync(Guid runId, string workerType, CancellationToken ct = default) => Task.CompletedTask;
    public Task PublishRunStartedAsync(Guid runId, CancellationToken ct = default) => Task.CompletedTask;
    public Task PublishRunFinishedAsync(Guid runId, CancellationToken ct = default) => Task.CompletedTask;
    public Task PublishRunCancelledAsync(Guid runId, CancellationToken ct = default) => Task.CompletedTask;
    public Task PublishDefinitionUpdatedAsync(Guid definitionId, CancellationToken ct = default) => Task.CompletedTask;
    public Task SubscribeToDefinitionUpdatesAsync(string subscriberQueueName, Func<byte[], Task<bool>> handler, CancellationToken ct = default) => Task.CompletedTask;
    public Task SubscribeToRunCompletionsAsync(Func<byte[], Task<bool>> handler, CancellationToken ct = default) => Task.CompletedTask;
    public Task SubscribeToRunCancellationsAsync(string workerType, Func<Guid, Task> handler, CancellationToken ct = default) => Task.CompletedTask;
}
