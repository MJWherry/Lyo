using System.Diagnostics;

namespace Lyo.Gateway.Models;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record PersonPhoneNumberRes(Guid Id, Guid PersonId, string Number, string? Type, DateOnly CreatedDate, DateOnly UpdatedDate)
{
    public override string ToString()
        => $"PersonPhoneNumberRes: {Number}, type={Type}, person={PersonId}";
}
