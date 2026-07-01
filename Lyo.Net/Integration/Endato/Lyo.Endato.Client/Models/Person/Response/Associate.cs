using System.Diagnostics;

namespace Lyo.Endato.Client.Models.Person.Response;

/// <summary>Associate linked to a Person Search result.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record Associate(string TahoeId, Name Name)
{
    public override string ToString() => $"Associate: TahoeId={TahoeId}, {Name}";
}