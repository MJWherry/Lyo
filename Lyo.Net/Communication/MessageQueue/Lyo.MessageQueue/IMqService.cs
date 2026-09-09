using Lyo.Health;

namespace Lyo.MessageQueue;

/// <summary>Contract for queue work: manage queues, send messages, and receive them.</summary>
public interface IMqService : IHealth
{
    /// <summary>Opens a connection to the queue service.</summary>
    /// <param name="ct">Token used to cancel the call.</param>
    Task ConnectAsync(CancellationToken ct = default);

    /// <summary>Closes the connection and releases resources.</summary>
    /// <param name="ct">Token used to cancel the call.</param>
    Task DisconnectAsync(CancellationToken ct = default);

    /// <summary>Whether the service is connected right now.</summary>
    bool IsConnected();

    // Queue management

    /// <summary>Declares a queue with the given name and options.</summary>
    /// <param name="queueName">Name of the queue to create.</param>
    /// <param name="durable">If true, the queue lives through a broker restart.</param>
    /// <param name="exclusive">If true, only one connection may use the queue.</param>
    /// <param name="autoDelete">If true, the queue is deleted when unused.</param>
    /// <param name="arguments">Extra queue arguments (provider-specific).</param>
    /// <param name="ct">Token used to cancel the call.</param>
    /// <returns>True when the queue was created; otherwise false.</returns>
    Task<bool> CreateQueue(
        string queueName,
        bool durable = true,
        bool exclusive = false,
        bool autoDelete = false,
        IDictionary<string, object>? arguments = null,
        CancellationToken ct = default);

    /// <summary>Deletes a queue.</summary>
    /// <param name="queueName">Name of the queue to delete.</param>
    /// <param name="ifUnused">If true, delete only when there are no consumers.</param>
    /// <param name="ifEmpty">If true, delete only when the queue is empty.</param>
    /// <param name="ct">Token used to cancel the call.</param>
    /// <returns>True when the queue was deleted; otherwise false.</returns>
    Task<bool> DeleteQueue(string queueName, bool ifUnused = false, bool ifEmpty = false, CancellationToken ct = default);

    /// <summary>Removes every message from a queue without deleting the queue.</summary>
    /// <param name="queueName">Name of the queue to clear.</param>
    /// <param name="ct">Token used to cancel the call.</param>
    /// <returns>True when the queue was cleared; otherwise false.</returns>
    Task<bool> ClearQueue(string queueName, CancellationToken ct = default);

    // Sending

    /// <summary>Publishes a message to a queue.</summary>
    /// <param name="queueName">Destination queue.</param>
    /// <param name="data">Message bytes.</param>
    /// <returns>True when the send succeeded; otherwise false.</returns>
    Task<bool> SendToQueue(string queueName, byte[] data);

    /// <summary>Reads messages from a queue without consuming them.</summary>
    /// <param name="queueName">Queue to inspect.</param>
    /// <param name="maxMessages">Upper bound on how many messages to read.</param>
    /// <param name="ct">Token used to cancel the call.</param>
    Task<IReadOnlyList<QueuePeekMessage>> PeekQueueMessages(string queueName, int maxMessages = 10, CancellationToken ct = default);

    // Receiving

    /// <summary>
    /// Subscribes to a queue. <paramref name="onMessage" /> runs for each delivery. Return true to requeue the message,
    /// or false to ack and remove it.
    /// </summary>
    /// <param name="queueName">Queue to subscribe to.</param>
    /// <param name="onMessage">Handler that processes the message and returns whether to requeue it (true = requeue, false = ack/remove).</param>
    /// <param name="ct">Token used to cancel. Cancellation stops the subscription.</param>
    /// <returns>True when the subscription was established; otherwise false.</returns>
    Task<bool> SubscribeToQueue(string queueName, Func<byte[], Task<bool>> onMessage, CancellationToken ct = default);

    // Exchanges

    /// <summary>
    /// Declares an exchange. Brokers treat this as idempotent when the name and arguments already match; a mismatch (for example a different type) fails.
    /// </summary>
    /// <param name="exchangeName">Name of the exchange to create.</param>
    /// <param name="exchangeType">Exchange type (for example <c>direct</c>, <c>topic</c>, <c>fanout</c>). Defaults to <c>direct</c>.</param>
    /// <param name="durable">If true, the exchange lives through a broker restart.</param>
    /// <param name="autoDelete">If true, the exchange is deleted when unused.</param>
    /// <param name="arguments">Extra exchange arguments (provider-specific).</param>
    /// <param name="ct">Token used to cancel the call.</param>
    /// <returns>True when the exchange was created or already existed with matching arguments; otherwise false.</returns>
    Task<bool> CreateExchange(
        string exchangeName,
        string exchangeType = "direct",
        bool durable = true,
        bool autoDelete = false,
        IDictionary<string, object>? arguments = null,
        CancellationToken ct = default);

    /// <summary>
    /// Binds a queue to an exchange with a routing key so messages published with that key reach the queue. On non-RabbitMQ brokers this may become
    /// a topic subscription.
    /// </summary>
    /// <param name="queueName">Queue to bind.</param>
    /// <param name="exchangeName">Exchange (or topic) to bind to.</param>
    /// <param name="routingKey">Routing key that selects messages for this queue.</param>
    /// <param name="ct">Token used to cancel the call.</param>
    /// <returns>True when the binding was created; otherwise false.</returns>
    Task<bool> BindQueueToExchange(string queueName, string exchangeName, string routingKey, CancellationToken ct = default);

    /// <summary>Publishes a message to an exchange with a routing key.</summary>
    /// <param name="exchangeName">Exchange to publish to.</param>
    /// <param name="routingKey">Routing key used to route the message.</param>
    /// <param name="data">Message bytes.</param>
    /// <returns>True when the publish succeeded; otherwise false.</returns>
    Task<bool> SendToExchange(string exchangeName, string routingKey, byte[] data);
}