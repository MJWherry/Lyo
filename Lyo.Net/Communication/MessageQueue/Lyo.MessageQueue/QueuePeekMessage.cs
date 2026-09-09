namespace Lyo.MessageQueue;

/// <summary>A message inspected on a queue without being consumed.</summary>
public sealed record QueuePeekMessage(
    string Payload,
    string? PayloadEncoding = null,
    string? Exchange = null,
    string? RoutingKey = null,
    long? MessageCount = null,
    bool Redelivered = false);