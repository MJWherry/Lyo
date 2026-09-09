using System.Diagnostics;

namespace Lyo.Authentication.Models.Records;

/// <summary>Admin-assigned extra JWT claim on a Lyo user. Backed by <c>[user].[claim]</c> when persisted via <c>Lyo.Authentication.Postgres</c>.</summary>
/// <param name="Id">Stable row id.</param>
/// <param name="UserId">Owning Lyo user.</param>
/// <param name="Type">Claim type (JWT name). Reserved names such as <c>iss</c>, <c>sub</c>, <c>scope</c>, and <c>lyo:*</c> are ignored at issue time.</param>
/// <param name="Value">Claim value written onto issued access JWTs.</param>
/// <param name="CreatedAt">When the row was first written.</param>
/// <param name="UpdatedAt">When the row was last updated.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record LyoUserClaim(Guid Id, Guid UserId, string Type, string Value, DateTime CreatedAt, DateTime? UpdatedAt)
{
    public override string ToString() => $"LyoUserClaim: user={UserId}, type={Type}";
}
