using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lyo.Authentication.Models.Records;

namespace Lyo.Authentication.Services.Jwt;

/// <summary>Mints Lyo-signed JWTs (EdDSA / Ed25519) for an authenticated user. Optionally also issues a paired Format-B refresh token.</summary>
public interface ILyoJwtIssuer
{
    /// <summary>Issues an access JWT (and refresh token unless <paramref name="includeRefresh"/> is false) for the given user.</summary>
    /// <param name="user">The authenticated Lyo user. Must not be disabled.</param>
    /// <param name="scopes">Snapshotted scopes for this session.</param>
    /// <param name="provider">Originating provider name (e.g. <c>google</c>, <c>keycloak:my-realm</c>, <c>local</c>).</param>
    /// <param name="externalSubject">Provider's <c>sub</c> claim. Audit-only; emitted into <c>lyo:external_sub</c>.</param>
    /// <param name="includeRefresh">When <c>true</c> (the default), also mints a refresh token. Set to <c>false</c> for machine-to-machine flows that exchange a stale JWT.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<IssuedLyoJwt> IssueAsync(
        LyoUser user,
        IReadOnlyList<string> scopes,
        string provider,
        string? externalSubject,
        bool includeRefresh = true,
        CancellationToken ct = default);
}
