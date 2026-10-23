namespace Lyo.Outbox.Dispatcher;

/// <summary>Bytes placed on the queue. The stored outbox payload is unchanged. Consumers dedupe on <see cref="Id" />.</summary>
public sealed class OutboxDelivery
{
    /// <summary>Idempotency row id.</summary>
    public Guid Id { get; init; }

    /// <summary>Original caller payload.</summary>
    public byte[] Payload { get; init; } = [];
}