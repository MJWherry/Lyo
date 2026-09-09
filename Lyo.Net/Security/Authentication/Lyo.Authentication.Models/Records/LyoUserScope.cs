using System.Diagnostics;

namespace Lyo.Authentication.Models.Records;

/// <summary>Admin-assigned authorization scope on a Lyo user. Backed by <c>[user].[scope]</c> when persisted via <c>Lyo.Authentication.Postgres</c>.</summary>
/// <param name="Id">Stable row id.</param>
/// <param name="UserId">Owning Lyo user.</param>
/// <param name="Name">Scope name written onto issued access JWTs (space-delimited <c>scope</c> claim).</param>
/// <param name="CreatedAt">When the row was first written.</param>
/// <param name="UpdatedAt">When the row was last updated.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record LyoUserScope(Guid Id, Guid UserId, string Name, DateTime CreatedAt, DateTime? UpdatedAt)
{
    public override string ToString() => $"LyoUserScope: user={UserId}, name={Name}";
}
