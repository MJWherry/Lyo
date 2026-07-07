namespace Lyo.MessageQueue;

/// <summary>
/// Optional capability interface for message queue transports that can deliver a message after a delay (e.g. RabbitMQ via TTL + dead-letter wait queues). Consumers such as
/// <see cref="QueueWorkerBase{TRequest, TResult}" /> type-check for this interface to space out retries with broker-side backpressure instead of in-process waits.
/// </summary>
public interface IDelayedMqService
{
    /// <summary>Sends a message that becomes visible on <paramref name="queueName" /> only after <paramref name="delay" /> has elapsed.</summary>
    /// <param name="queueName">The destination queue.</param>
    /// <param name="data">The message bytes.</param>
    /// <param name="delay">How long the message stays invisible before delivery. Non-positive values send immediately.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True when the message was accepted by the transport.</returns>
    Task<bool> SendToQueueDelayed(string queueName, byte[] data, TimeSpan delay, CancellationToken ct = default);
}
