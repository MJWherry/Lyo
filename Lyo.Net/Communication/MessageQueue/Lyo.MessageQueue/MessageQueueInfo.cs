using System.Diagnostics;

namespace Lyo.MessageQueue;

/// <summary>Generic facts about a queue. Provider-specific values can live in AdditionalProperties.</summary>
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
