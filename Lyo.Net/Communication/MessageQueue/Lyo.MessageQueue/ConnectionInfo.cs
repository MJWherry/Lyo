using System.Diagnostics;

namespace Lyo.MessageQueue;

[DebuggerDisplay("{ToString(),nq}")]
public record ConnectionInfo(string User, string? UserProvidedName, string State, string VHost)
{
    public override string ToString() => UserProvidedName ?? User;
}