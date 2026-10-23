namespace Lyo.Outbox;

/// <summary>One message to record inside the caller's transaction.</summary>
public sealed class OutboxMessage
{
    /// <summary>Queue name. Must not contain <c>/</c> or <c>|</c>.</summary>
    public required string Destination { get; init; }

    /// <summary>Caller bytes. The idempotency hash is SHA-256 of these bytes.</summary>
    public required byte[] Payload { get; init; }

    /// <summary>Caller boundary, stored after <c>outbox/{destination}/</c>.</summary>
    public required string CallerScope { get; init; }

    /// <summary>Idempotency key within that scope.</summary>
    public required string Key { get; init; }
}