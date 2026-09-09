using Lyo.Authentication.Models.Records;

namespace Lyo.Authentication.Web.Components.Models;

/// <summary>
/// Mirror of the JSON from <c>GET /auth/me</c> and <c>GET /auth/users/{id}</c> in <c>Lyo.Authentication.OpenIdConnect</c>. Re-declared here so the components library
/// does not have to pull in the OIDC API project — the API ships the same shape.
/// </summary>
/// <param name="User">Lyo user record.</param>
/// <param name="Scopes">User scopes. <c>/auth/me</c> uses bearer claims; <c>/auth/users/{id}</c> uses <see cref="LyoUser.Scopes" />.</param>
/// <param name="LinkedIdentities">All external identities currently linked to the user.</param>
public sealed record AuthMeSnapshot(LyoUser User, string[] Scopes, LinkedIdentity[] LinkedIdentities);