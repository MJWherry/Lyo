namespace Lyo.MessageQueue;

/// <summary>
/// Optional capability for transports that can hold a message until a delay elapses (for example RabbitMQ TTL plus dead-letter wait queues). Callers such as
/// <see cref="QueueWorkerBase{TRequest, TResult}" /> check for this interface so retries can wait on the broker instead of sleeping in-process.
/// </summary>
public interface IDelayedMqService
{
    /// <summary>Publishes a message that appears on <paramref name="queueName" /> only after <paramref name="delay" />.</summary>
    /// <param name="queueName">Destination queue.</param>
    /// <param name="data">Message bytes.</param>
    /// <param name="delay">How long the message stays hidden. Non-positive values publish immediately.</param>
    /// <param name="ct">Token used to cancel the call.</param>
    /// <returns>True when the transport accepted the message.</returns>
    Task<bool> SendToQueueDelayed(string queueName, byte[] data, TimeSpan delay, CancellationToken ct = default);
}