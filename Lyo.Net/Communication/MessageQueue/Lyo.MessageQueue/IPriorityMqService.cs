namespace Lyo.MessageQueue;

/// <summary>
/// Optional capability for transports that honor per-message priority (for example RabbitMQ queues declared with <c>x-max-priority</c>).
/// Publishers check for this interface and fall back to a normal send when the transport or queue does not support priorities.
/// </summary>
public interface IPriorityMqService
{
    /// <summary>
    /// Publishes to <paramref name="queueName" /> with a priority. Higher values are delivered first when the queue was declared with priority support; on queues without
    /// it the broker ignores the priority.
    /// </summary>
    /// <param name="queueName">Destination queue.</param>
    /// <param name="data">Message bytes.</param>
    /// <param name="priority">Priority (0 is lowest). Values above the queue maximum are capped by the broker.</param>
    /// <returns>True when the transport accepted the message.</returns>
    Task<bool> SendToQueueWithPriority(string queueName, byte[] data, byte priority);
}