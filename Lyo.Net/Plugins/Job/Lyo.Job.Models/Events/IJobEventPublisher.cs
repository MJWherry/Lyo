using Lyo.Job.Models.Enums;

namespace Lyo.Job.Models.Events;

/// <summary>
/// Transport-agnostic surface for job lifecycle event publishing and subscription. Implement this interface to use any message broker (RabbitMQ, Azure Service Bus, AWS
/// SQS, and so on) and register the implementation in the DI container. API hosts with a job database use <c>MqJobEventPublisher</c> in <c>Lyo.Job.Postgres</c>;
/// scheduler/worker hosts use <c>Lyo.Job.Client.MqJobEventPublisher</c> (<c>IMqService</c> + Job API client, no EF).
/// </summary>
public interface IJobEventPublisher
{
    /// <summary>Whether the underlying transport is connected and ready to send and receive.</summary>
    bool IsConnected();

    /// <summary>
    /// Connects to the underlying transport, declares <c>job.events</c> and the shared job queues when they are missing, and establishes exchange bindings. Called once on
    /// startup before subscriptions are established.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    Task SetupAsync(CancellationToken ct = default);

    /// <summary>Send the newly created run ID to the per-worker-type delivery queue and broadcast the creation event to interested subscribers.</summary>
    /// <param name="runId">ID of the newly created job run.</param>
    /// <param name="workerType">Worker type used to derive the target queue name.</param>
    /// <param name="priority">Message priority (0 = default). Honored only when the transport and queue support priorities; otherwise ignored.</param>
    /// <param name="ct">Cancellation token.</param>
    Task PublishRunCreatedAsync(Guid runId, string workerType, int priority = 0, CancellationToken ct = default);

    /// <summary>Broadcast that a run has started executing.</summary>
    /// <param name="runId">ID of the job run that started.</param>
    /// <param name="ct">Cancellation token.</param>
    Task PublishRunStartedAsync(Guid runId, CancellationToken ct = default);

    /// <summary>Send the finished run ID to the scheduler completion queue and broadcast the finished event so the scheduler can handle triggers and retries.</summary>
    /// <param name="runId">ID of the job run that finished.</param>
    /// <param name="ct">Cancellation token.</param>
    Task PublishRunFinishedAsync(Guid runId, CancellationToken ct = default);

    /// <summary>Broadcast a cancellation request to any worker currently processing this run.</summary>
    /// <param name="runId">ID of the job run to cancel.</param>
    /// <param name="ct">Cancellation token.</param>
    Task PublishRunCancelledAsync(Guid runId, CancellationToken ct = default);

    /// <summary>
    /// Broadcast that a job definition has changed (for example enabled/disabled, schedule updated). Scheduler instances subscribe to this to refresh their in-memory
    /// definition cache.
    /// </summary>
    /// <param name="definitionId">ID of the changed job definition.</param>
    /// <param name="ct">Cancellation token.</param>
    Task PublishDefinitionUpdatedAsync(Guid definitionId, CancellationToken ct = default);

    /// <summary>Broadcast a job alert (failure, circuit breaker, dead job, SLA breach) to <c>job.notifications.alert</c>.</summary>
    /// <param name="definitionId">Job definition the alert relates to.</param>
    /// <param name="runId">Job run id, when applicable.</param>
    /// <param name="alertType">Alert category.</param>
    /// <param name="message">Human-readable alert message.</param>
    /// <param name="ct">Cancellation token.</param>
    Task PublishAlertAsync(Guid definitionId, Guid? runId, JobAlertType alertType, string message, CancellationToken ct = default);

    /// <summary>
    /// Subscribe to definition-change notifications. The scheduler calls this once on startup to invalidate its in-memory cache whenever a
    /// definition is updated.
    /// </summary>
    /// <param name="subscriberQueueName">Queue name to bind/subscribe on (transport-specific).</param>
    /// <param name="handler">Message handler. Return <c>true</c> to requeue (for example on transient error), <c>false</c> to acknowledge.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SubscribeToDefinitionUpdatesAsync(string subscriberQueueName, Func<byte[], Task<bool>> handler, CancellationToken ct = default);

    /// <summary>Subscribe to run-completion notifications. The scheduler calls this once on startup to handle triggers and retry logic after each run finishes.</summary>
    /// <param name="handler">Message handler. Return <c>true</c> to requeue (for example on transient error), <c>false</c> to acknowledge.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SubscribeToRunCompletionsAsync(Func<byte[], Task<bool>> handler, CancellationToken ct = default);

    /// <summary>
    /// Subscribe to run-cancellation notifications for a specific worker type. Workers call this on startup to receive cancellation signals for runs they are processing.
    /// Implementations must deliver each cancellation to <b>every</b> subscribed instance (broadcast/fanout, for example a per-instance exclusive queue bound to the cancel routing
    /// key). A shared competing-consumer queue would deliver each cancel to only one instance of a scaled-out worker type and silently lose cancellations.
    /// </summary>
    /// <param name="workerType">Worker type, used to derive the subscription name.</param>
    /// <param name="handler">Called with the <see cref="Guid" /> of the run that should be cancelled. Instances not executing that run ignore it.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <param name="instanceSuffix">
    /// Optional per-instance suffix for the exclusive cancel queue (<c>job.run.{workerType}.cancel.{suffix}</c>). When omitted, the implementation generates one. Workers that
    /// report the cancel queue in instance metadata should pass a suffix they already know.
    /// </param>
    Task SubscribeToRunCancellationsAsync(string workerType, Func<Guid, Task> handler, CancellationToken ct = default, string? instanceSuffix = null);
}