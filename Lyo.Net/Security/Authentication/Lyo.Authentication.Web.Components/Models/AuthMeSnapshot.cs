using Lyo.Authentication.Models.Records;

namespace Lyo.Authentication.Web.Components.Models;

/// <summary>
/// Mirror of the JSON returned by <c>GET /auth/me</c> and <c>GET /auth/users/{id}</c> in <c>Lyo.Authentication.OpenIdConnect</c>. Re-declared here so the components library
/// does not have to pull in the OIDC API project — the API ships the same shape.
/// </summary>
/// <param name="User">The Lyo user record.</param>
/// <param name="Scopes">Scopes for this user. For <c>/auth/me</c> these come from the bearer's claims; for <c>/auth/users/{id}</c> they come from <see cref="LyoUser.Scopes" />.</param>
/// <param name="LinkedIdentities">All external identities currently linked to the user.</param>
public sealed record AuthMeSnapshot(LyoUser User, string[] Scopes, LinkedIdentity[] LinkedIdentities);