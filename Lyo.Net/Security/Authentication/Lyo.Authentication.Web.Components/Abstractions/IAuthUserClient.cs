using System;
using System.Threading;
using System.Threading.Tasks;
using Lyo.Authentication.Web.Components.Models;

namespace Lyo.Authentication.Web.Components.Abstractions;

/// <summary>
/// Host-agnostic HTTP wrapper for the <c>/auth/me</c> and <c>/auth/users/{id}</c> endpoints exposed by <c>Lyo.Authentication.OpenIdConnect</c>. The host adapter is responsible for
/// attaching the bearer token (either via the Server-side <c>LyoAuthDelegatingHandler</c> or the WASM-side equivalent).
/// </summary>
public interface IAuthUserClient
{
    /// <summary>Loads the current bearer principal via <c>GET /auth/me</c>. Returns <c>null</c> on any non-2xx (e.g. anonymous request).</summary>
    Task<AuthMeSnapshot?> GetMeAsync(CancellationToken ct = default);

    /// <summary>Loads an arbitrary user by id via <c>GET /auth/users/{id}</c>. Returns <c>null</c> on 404 or insufficient scope.</summary>
    Task<AuthMeSnapshot?> GetUserAsync(Guid userId, CancellationToken ct = default);
}
