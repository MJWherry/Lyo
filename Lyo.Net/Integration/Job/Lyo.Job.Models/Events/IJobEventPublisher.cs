namespace Lyo.Job.Models.Events;

/// <summary>
/// Transport-agnostic abstraction for job lifecycle event publishing and subscription. Implement this interface to use any message broker (RabbitMQ, Azure Service Bus, AWS
/// SQS, etc.) and register the implementation in the DI container. The default <c>MqJobEventPublisher</c> (in <c>Lyo.Job.Postgres</c>) wraps <c>IMqService</c>.
/// </summary>
public interface IJobEventPublisher
{
    /// <summary>Whether the underlying transport is connected and ready to send/receive.</summary>
    bool IsConnected();

    /// <summary>Connects to the underlying transport, creates any required queues/topics, and establishes exchange bindings. Called once on startup before subscriptions are established.</summary>
    /// <param name="ct">Cancellation token.</param>
    Task SetupAsync(CancellationToken ct = default);

    /// <summary>Send the newly-created run ID to the per-worker-type delivery queue and broadcast the creation event to any interested subscribers.</summary>
    /// <param name="runId">The ID of the newly created job run.</param>
    /// <param name="workerType">The worker type used to derive the target queue name.</param>
    /// <param name="ct">Cancellation token.</param>
    Task PublishRunCreatedAsync(Guid runId, string workerType, CancellationToken ct = default);

    /// <summary>Broadcast that a run has started executing.</summary>
    /// <param name="runId">The ID of the job run that started.</param>
    /// <param name="ct">Cancellation token.</param>
    Task PublishRunStartedAsync(Guid runId, CancellationToken ct = default);

    /// <summary>Send the finished run ID to the scheduler completion queue and broadcast the finished event so the scheduler can process triggers and retries.</summary>
    /// <param name="runId">The ID of the job run that finished.</param>
    /// <param name="ct">Cancellation token.</param>
    Task PublishRunFinishedAsync(Guid runId, CancellationToken ct = default);

    /// <summary>Broadcast a cancellation request to any worker currently processing this run.</summary>
    /// <param name="runId">The ID of the job run to cancel.</param>
    /// <param name="ct">Cancellation token.</param>
    Task PublishRunCancelledAsync(Guid runId, CancellationToken ct = default);

    /// <summary>Broadcast that a job definition has changed (e.g. enabled/disabled, schedule updated). Scheduler instances subscribe to this to refresh their in-memory definition cache.</summary>
    /// <param name="definitionId">The ID of the changed job definition.</param>
    /// <param name="ct">Cancellation token.</param>
    Task PublishDefinitionUpdatedAsync(Guid definitionId, CancellationToken ct = default);

    /// <summary>Subscribe to definition-change notifications. The scheduler calls this once on startup to invalidate its in-memory cache whenever a definition is updated.</summary>
    /// <param name="subscriberQueueName">The queue name to bind/subscribe on (transport-specific).</param>
    /// <param name="handler">Message handler — return <c>true</c> to requeue (e.g. on transient error), <c>false</c> to acknowledge.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SubscribeToDefinitionUpdatesAsync(string subscriberQueueName, Func<byte[], Task<bool>> handler, CancellationToken ct = default);

    /// <summary>Subscribe to run-completion notifications. The scheduler calls this once on startup to process triggers and retry logic after each run finishes.</summary>
    /// <param name="handler">Message handler — return <c>true</c> to requeue (e.g. on transient error), <c>false</c> to acknowledge.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SubscribeToRunCompletionsAsync(Func<byte[], Task<bool>> handler, CancellationToken ct = default);

    /// <summary>Subscribe to run-cancellation notifications for a specific worker type. Workers call this on startup to receive cancellation signals for runs they are processing.</summary>
    /// <param name="workerType">The worker type — used to create a per-worker-type subscription.</param>
    /// <param name="handler">Called with the <see cref="Guid" /> of the run that should be cancelled.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SubscribeToRunCancellationsAsync(string workerType, Func<Guid, Task> handler, CancellationToken ct = default);
}