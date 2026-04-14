using Lyo.Health;

namespace Lyo.MessageQueue;

/// <summary>Abstract interface for message queue operations. Implementations provide queue management, message sending, and message receiving capabilities.</summary>
public interface IMqService : IHealth
{
    /// <summary>Connects to the message queue service.</summary>
    /// <param name="ct">Cancellation token.</param>
    Task ConnectAsync(CancellationToken ct = default);

    /// <summary>Disconnects from the message queue service and cleans up resources.</summary>
    /// <param name="ct">Cancellation token.</param>
    Task DisconnectAsync(CancellationToken ct = default);

    /// <summary>Checks if the service is currently connected.</summary>
    bool IsConnected();

    // Queue Management

    /// <summary>Creates a new queue with the specified name and options.</summary>
    /// <param name="queueName">The name of the queue to create.</param>
    /// <param name="durable">If true, the queue will survive broker restarts.</param>
    /// <param name="exclusive">If true, the queue can only be used by one connection.</param>
    /// <param name="autoDelete">If true, the queue will be deleted when no longer used.</param>
    /// <param name="arguments">Additional queue arguments (implementation-specific).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the queue was created successfully, false otherwise.</returns>
    Task<bool> CreateQueue(
        string queueName,
        bool durable = true,
        bool exclusive = false,
        bool autoDelete = false,
        IDictionary<string, object>? arguments = null,
        CancellationToken ct = default);

    /// <summary>Deletes the specified queue.</summary>
    /// <param name="queueName">The name of the queue to delete.</param>
    /// <param name="ifUnused">If true, only delete if the queue has no consumers.</param>
    /// <param name="ifEmpty">If true, only delete if the queue is empty.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the queue was deleted, false otherwise.</returns>
    Task<bool> DeleteQueue(string queueName, bool ifUnused = false, bool ifEmpty = false, CancellationToken ct = default);

    /// <summary>Clears all messages from the specified queue without deleting the queue itself.</summary>
    /// <param name="queueName">The name of the queue to clear.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the queue was cleared successfully, false otherwise.</returns>
    Task<bool> ClearQueue(string queueName, CancellationToken ct = default);

    // Message Sending

    /// <summary>Sends a message to the specified queue.</summary>
    /// <param name="queueName">The name of the queue to send the message to.</param>
    /// <param name="data">The message data as bytes.</param>
    /// <returns>True if the message was sent successfully, false otherwise.</returns>
    Task<bool> SendToQueue(string queueName, byte[] data);

    /// <summary>Reads messages from a queue without removing them.</summary>
    /// <param name="queueName">The queue to inspect.</param>
    /// <param name="maxMessages">The maximum number of messages to read.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<IReadOnlyList<QueuePeekMessage>> PeekQueueMessages(string queueName, int maxMessages = 10, CancellationToken ct = default);

    // Message Receiving

    /// <summary>
    /// Subscribes to messages from the specified queue. The provided function will be called for each message received. The function should return true to requeue the message,
    /// or false to acknowledge and remove it.
    /// </summary>
    /// <param name="queueName">The name of the queue to subscribe to.</param>
    /// <param name="onMessage">A function that processes the message and returns whether to requeue it (true = requeue, false = acknowledge/remove).</param>
    /// <param name="ct">Cancellation token. When cancelled, the subscription will stop.</param>
    /// <returns>True if subscription was successful, false otherwise.</returns>
    Task<bool> SubscribeToQueue(string queueName, Func<byte[], Task<bool>> onMessage, CancellationToken ct = default);
}