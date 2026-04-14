namespace Lyo.MessageQueue;

/// <summary>Represents a message read from a queue without removing it.</summary>
public sealed record QueuePeekMessage(
    string Payload,
    string? PayloadEncoding = null,
    string? Exchange = null,
    string? RoutingKey = null,
    long? MessageCount = null,
    bool Redelivered = false);