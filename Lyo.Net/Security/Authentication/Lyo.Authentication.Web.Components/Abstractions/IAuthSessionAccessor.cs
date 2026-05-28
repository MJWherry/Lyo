using Lyo.Authentication.Web.Components.Models;

namespace Lyo.Authentication.Web.Components.Abstractions;

/// <summary>
/// Read-side view of the active session for the debug workbench. Host adapters surface their internal session bag (cookie + server store on Server, in-memory + local-storage
/// on WASM) through this contract so the same Razor UI works on either host.
/// </summary>
public interface IAuthSessionAccessor
{
    /// <summary>Returns the current session snapshot, or <c>null</c> when the request is anonymous.</summary>
    Task<AuthSessionSnapshot?> GetCurrentAsync(CancellationToken ct = default);

    /// <summary>Forces a refresh of the access token (if a refresh token is available). Returns <c>true</c> if the session was rotated.</summary>
    Task<bool> RefreshAsync(CancellationToken ct = default);
}