using Lyo.Authentication.Web.Components.Models;

namespace Lyo.Authentication.Web.Components.Abstractions;

/// <summary>
/// Read-only window onto the active session for the debug workbench. Host adapters expose their session bag (cookie plus server store on Server, in-memory plus local-storage
/// on WASM) through this contract so one Razor UI can run on either host.
/// </summary>
public interface IAuthSessionAccessor
{
    /// <summary>Current session snapshot, or <c>null</c> for an anonymous request.</summary>
    Task<AuthSessionSnapshot?> GetCurrentAsync(CancellationToken ct = default);

    /// <summary>Refreshes the access token when a refresh token exists. Returns <c>true</c> if the session rotated.</summary>
    Task<bool> RefreshAsync(CancellationToken ct = default);
}