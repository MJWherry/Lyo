namespace Lyo.MessageQueue;

/// <summary>
/// Optional capability interface for message queue transports that support per-message priorities (e.g. RabbitMQ priority queues declared with <c>x-max-priority</c>).
/// Publishers type-check for this interface and fall back to a normal send when the transport (or the queue) does not support priorities.
/// </summary>
public interface IPriorityMqService
{
    /// <summary>
    /// Sends a message to the specified queue with a priority. Higher priorities are delivered first when the queue was declared with priority support; on queues without
    /// priority support the priority is ignored by the broker.
    /// </summary>
    /// <param name="queueName">The destination queue.</param>
    /// <param name="data">The message bytes.</param>
    /// <param name="priority">Message priority (0 = lowest). Values above the queue's maximum are capped by the broker.</param>
    /// <returns>True when the message was accepted by the transport.</returns>
    Task<bool> SendToQueueWithPriority(string queueName, byte[] data, byte priority);
}