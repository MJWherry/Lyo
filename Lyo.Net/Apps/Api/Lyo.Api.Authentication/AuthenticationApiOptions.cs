using Lyo.Api.ApiEndpoint;

namespace Lyo.Api.Authentication;

/// <summary>
/// Per-surface auth for authentication admin endpoints. Every surface starts as <see cref="EndpointAuth.RequireAuthorization()" /> (signed-in user). Hosts must choose
/// <see cref="EndpointAuth.Anonymous()" /> on purpose — these routes QueryProject every user and token.
/// </summary>
public sealed class AuthenticationApiOptions
{
    /// <summary>Auth on User Query/Get/Patch (and Export when that feature is on). Defaults to requiring authorization.</summary>
    public EndpointAuth? UserAuth { get; init; } = EndpointAuth.RequireAuthorization();

    /// <summary>Auth on Token Query/Get/Patch/Delete. Defaults to requiring authorization.</summary>
    public EndpointAuth? TokenAuth { get; init; } = EndpointAuth.RequireAuthorization();

    /// <summary>Auth on Claim CRUD. Defaults to requiring authorization.</summary>
    public EndpointAuth? ClaimAuth { get; init; } = EndpointAuth.RequireAuthorization();

    /// <summary>Auth on Scope CRUD. Defaults to requiring authorization.</summary>
    public EndpointAuth? ScopeAuth { get; init; } = EndpointAuth.RequireAuthorization();

    /// <summary>Auth on LinkedIdentity Query/Get. Defaults to requiring authorization.</summary>
    public EndpointAuth? LinkedIdentityAuth { get; init; } = EndpointAuth.RequireAuthorization();

    /// <summary>Auth on Event Query/Get. Defaults to requiring authorization.</summary>
    public EndpointAuth? EventAuth { get; init; } = EndpointAuth.RequireAuthorization();

    /// <summary>Builds options that apply the same <paramref name="auth" /> to every surface.</summary>
    public static AuthenticationApiOptions WithAuth(EndpointAuth? auth)
        => new() {
            UserAuth = auth,
            TokenAuth = auth,
            ClaimAuth = auth,
            ScopeAuth = auth,
            LinkedIdentityAuth = auth,
            EventAuth = auth
        };
}
