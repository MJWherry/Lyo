using System.Diagnostics;

namespace Lyo.Endato.Client.Models.Person.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record Associate(string TahoeId, Name Name)
{
    public override string ToString() => $"Associate: TahoeId={TahoeId}, {Name}";
}