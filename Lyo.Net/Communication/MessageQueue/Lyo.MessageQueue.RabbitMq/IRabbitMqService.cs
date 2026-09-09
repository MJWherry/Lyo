namespace Lyo.MessageQueue.RabbitMq;

/// <summary>RabbitMQ-only operations such as exchange management, on top of <see cref="IMqService" />.</summary>
public interface IRabbitMqService : IMqService, IDelayedMqService, IPriorityMqService
{
    /// <summary>Declares an exchange.</summary>
    Task<bool> CreateExchange(
        string exchangeName,
        string exchangeType = "direct",
        bool durable = true,
        bool autoDelete = false,
        IDictionary<string, object>? arguments = null,
        CancellationToken ct = default);

    /// <summary>Deletes an exchange.</summary>
    Task<bool> DeleteExchange(string exchangeName, bool ifUnused = false, CancellationToken ct = default);

    /// <summary>
    /// Declares a queue plus a companion dead-letter queue (default <c>{queueName}.dlq</c>) and sets the main queue's <c>x-dead-letter-exchange</c> /
    /// <c>x-dead-letter-routing-key</c> so broker rejections (nack without requeue, TTL expiry, overflow) land in the DLQ instead of being dropped. RabbitMQ cannot
    /// change arguments on an existing queue — declaring over one with different arguments fails.
    /// </summary>
    /// <param name="queueName">Main queue name.</param>
    /// <param name="durable">Whether both queues live through a broker restart.</param>
    /// <param name="dlqName">Dead-letter queue name. Defaults to <c>{queueName}.dlq</c>.</param>
    /// <param name="arguments">Extra arguments for the main queue (merged with the dead-letter arguments).</param>
    /// <param name="ct">Token used to cancel the call.</param>
    Task<bool> CreateQueueWithDlq(string queueName, bool durable = true, string? dlqName = null, IDictionary<string, object>? arguments = null, CancellationToken ct = default);

    /// <summary>Live queue stats from the management API (message counts, consumers, state). AMQP flags and <c>x-*</c> arguments go into <see cref="MessageQueueInfo.AdditionalProperties" />. Null when the queue is missing or the API call fails.</summary>
    Task<MessageQueueInfo?> GetQueueInfoAsync(string queueName, CancellationToken ct = default);

    /// <summary>Live stats for every queue on the configured virtual host, from the management API.</summary>
    Task<IReadOnlyList<MessageQueueInfo>> GetAllQueuesInfoAsync(CancellationToken ct = default);

    /// <summary>One exchange from the management API. Null when it is missing or the API call fails.</summary>
    Task<MessageExchangeInfo?> GetExchangeInfoAsync(string exchangeName, CancellationToken ct = default);

    /// <summary>Every exchange on the configured virtual host from the management API (including broker defaults such as <c>amq.*</c>).</summary>
    Task<IReadOnlyList<MessageExchangeInfo>> GetAllExchangesInfoAsync(CancellationToken ct = default);
}