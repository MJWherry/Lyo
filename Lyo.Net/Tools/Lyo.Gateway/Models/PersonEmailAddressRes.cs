using System.Diagnostics;

namespace Lyo.Gateway.Models;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record PersonEmailAddressRes(Guid Id, Guid PersonId, string Address)
{
    public override string ToString()
        => $"PersonEmailAddressRes: {Address}, person={PersonId}";
}
