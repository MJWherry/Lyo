using System.Diagnostics;

namespace Lyo.MessageQueue;

/// <summary>Snapshot of an exchange from the broker. Implementation-specific properties can be stored in AdditionalProperties.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public record MessageExchangeInfo(
    string Name,
    string? Type,
    bool Durable,
    bool AutoDelete,
    bool Internal,
    Dictionary<string, object> AdditionalProperties)
{
    public override string ToString() => $"{Name} Type={Type ?? "(unknown)"} Durable={Durable}";
}
