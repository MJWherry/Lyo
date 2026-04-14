using System.Diagnostics;

namespace Lyo.MessageQueue;

/// <summary>Generic information about a message queue. Implementation-specific properties can be stored in AdditionalProperties.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public record MessageQueueInfo(
    string Name,
    string? State,
    string? Type,
    long Messages,
    long MessagesReady,
    long MessagesUnacknowledged,
    int Consumers,
    Dictionary<string, object> AdditionalProperties)
{
    public override string ToString() => $"{Name} Messages={Messages} Ready={MessagesReady} Unacked={MessagesUnacknowledged} Consumers={Consumers}";
}