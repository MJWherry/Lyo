using System;
using System.Threading;
using System.Threading.Tasks;
using Lyo.Authentication.Records;

namespace Lyo.Authentication.Services.Refresh;

/// <summary>Issues a Format-B refresh token of kind <c>internal</c>, scoped to <c>lyo:refresh</c>, and metadata-tagged with the access JWT's <c>jti</c> and the originating provider/subject (for scope re-resolution on rotation).</summary>
public interface ILyoRefreshTokenIssuer
{
    /// <summary>Issues a fresh refresh token bound to the given user, access-JWT <paramref name="parentJti"/>, and originating provider/subject.</summary>
    /// <param name="userId">Owning user.</param>
    /// <param name="parentJti">The <c>jti</c> of the access JWT this refresh token is paired with.</param>
    /// <param name="lifetime">How long the refresh token lives. Use <see cref="TimeSpan.Zero"/> for no expiry.</param>
    /// <param name="provider">Name of the IdP that minted the parent session (e.g. <c>google</c>, <c>keycloak:my-realm</c>, or <c>local</c> for refresh-on-refresh).</param>
    /// <param name="externalSubject">Provider's <c>sub</c> claim, when one is known. Used on rotation to re-resolve the <see cref="LinkedIdentity"/> and pick up scope grants/revocations.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<IssuedApiToken> IssueAsync(Guid userId, string parentJti, TimeSpan lifetime, string provider, string? externalSubject, CancellationToken ct = default);
}

/// <summary>Well-known scope names used by the refresh-token flow.</summary>
public static class LyoRefreshTokenScopes
{
    /// <summary>The scope used to identify refresh tokens.</summary>
    public const string Refresh = "lyo:refresh";
}
