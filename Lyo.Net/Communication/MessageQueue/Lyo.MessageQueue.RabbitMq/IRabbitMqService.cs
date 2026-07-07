namespace Lyo.MessageQueue.RabbitMq;

/// <summary>RabbitMQ-specific queue operations such as exchange management in addition to the base <see cref="IMqService" /> contract.</summary>
public interface IRabbitMqService : IMqService, IDelayedMqService
{
    /// <summary>Declares an exchange on the broker.</summary>
    Task<bool> CreateExchange(
        string exchangeName,
        string exchangeType = "direct",
        bool durable = true,
        bool autoDelete = false,
        IDictionary<string, object>? arguments = null,
        CancellationToken ct = default);

    /// <summary>Deletes an exchange from the broker.</summary>
    Task<bool> DeleteExchange(string exchangeName, bool ifUnused = false, CancellationToken ct = default);

    /// <summary>
    /// Declares a queue together with a companion dead-letter queue (default <c>{queueName}.dlq</c>) and wires the main queue's <c>x-dead-letter-exchange</c> /
    /// <c>x-dead-letter-routing-key</c> arguments so broker-side rejections (nack without requeue, TTL expiry, overflow) land in the DLQ instead of being dropped.
    /// Note: RabbitMQ cannot change arguments on an existing queue — declaring over an existing queue with different arguments fails.
    /// </summary>
    /// <param name="queueName">The main queue name.</param>
    /// <param name="durable">Whether both queues survive a broker restart.</param>
    /// <param name="dlqName">Dead-letter queue name. Defaults to <c>{queueName}.dlq</c>.</param>
    /// <param name="arguments">Additional arguments for the main queue (merged with the dead-letter arguments).</param>
    /// <param name="ct">Cancellation token.</param>
    Task<bool> CreateQueueWithDlq(string queueName, bool durable = true, string? dlqName = null, IDictionary<string, object>? arguments = null, CancellationToken ct = default);

    /// <summary>Gets live statistics for a queue via the management API (message counts, consumers, state). Returns null when the queue does not exist or the API call fails.</summary>
    Task<MessageQueueInfo?> GetQueueInfoAsync(string queueName, CancellationToken ct = default);

    /// <summary>Gets live statistics for all queues on the configured virtual host via the management API.</summary>
    Task<IReadOnlyList<MessageQueueInfo>> GetAllQueuesInfoAsync(CancellationToken ct = default);
}
