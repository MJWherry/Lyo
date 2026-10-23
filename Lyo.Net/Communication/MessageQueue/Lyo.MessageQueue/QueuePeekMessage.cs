using System.Diagnostics;

namespace Lyo.MessageQueue;

/// <summary>A message inspected on a queue without being consumed.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record QueuePeekMessage(
    string Payload,
    string? PayloadEncoding = null,
    string? Exchange = null,
    string? RoutingKey = null,
    long? MessageCount = null,
    bool Redelivered = false)
{
    /// <summary>Queue location and payload length. Payload text is omitted.</summary>
    public override string ToString()
        => $"Exchange={Exchange} RoutingKey={RoutingKey} Encoding={PayloadEncoding} Bytes={Payload.Length} Count={MessageCount} Redelivered={Redelivered}";
}