namespace Lyo.Authentication.Models.Records;

/// <summary>The successfully-validated form of a Format-B token. Consumed by the ASP.NET Core handler to project into a <see cref="System.Security.Claims.ClaimsPrincipal"/>.</summary>
/// <param name="TokenId">The 11-char token id (becomes the <c>lyo:token_id</c> claim).</param>
/// <param name="Subject">The synthesized JWT subject (<c>lyo_token:&lt;id&gt;</c>).</param>
/// <param name="OwnerUserId">The owning Lyo user, or <c>null</c> for unowned tokens.</param>
/// <param name="Kind">Token kind (e.g. <c>pat</c>).</param>
/// <param name="Ring">Token ring (e.g. <c>live</c>).</param>
/// <param name="Scopes">Snapshotted scopes from the token.</param>
/// <param name="ValidatedAt">When the validator produced this result.</param>
public sealed record ApiTokenPrincipal(
    string TokenId,
    string Subject,
    Guid? OwnerUserId,
    string Kind,
    string Ring,
    IReadOnlyList<string> Scopes,
    DateTime ValidatedAt);
