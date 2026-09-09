using System.Diagnostics;

namespace Lyo.Endato.Client.Models.Person.Response;

/// <summary>City/state for a person.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record Location(string City, string State)
{
    public override string ToString() => $"Location: '{City}', '{State}'";
}