using System.Diagnostics;

namespace Lyo.Endato.Client.Models.Person.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record Location(string City, string State)
{
    public override string ToString()
        => $"Location: '{City}', '{State}'";
}