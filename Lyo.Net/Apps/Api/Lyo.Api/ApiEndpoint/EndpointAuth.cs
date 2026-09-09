using Lyo.Exceptions;
using Microsoft.AspNetCore.Authorization;

namespace Lyo.Api.ApiEndpoint;

/// <summary>Per-endpoint authorization settings. When null, the builder-level auth is used.</summary>
/// <example>
/// <code>
/// // Via CrudConfiguration
/// var config = new CrudConfiguration&lt;MyDbContext, MyEntity&gt; {
///     GetAuth = EndpointAuth.Anonymous(),
///     UpdateAuth = EndpointAuth.RequireAuthorization("AdminOnly")
/// };
/// 
/// // Via individual With* methods
/// builder.WithGet(auth: EndpointAuth.Anonymous())
///        .WithUpdate(auth: EndpointAuth.RequireAuthorization("AdminOnly"));
/// </code>
/// </example>
public sealed record EndpointAuth
{
    /// <summary>When true, allows anonymous access on this endpoint.</summary>
    public bool AllowAnonymous { get; init; }

    /// <summary>When set, requires the specified authorization policy or policies on this endpoint.</summary>
    public string[]? AuthorizationPolicies { get; init; }

    /// <summary>When set, requires the specified authorization policy on this endpoint.</summary>
    public AuthorizationPolicy? AuthorizationPolicy { get; init; }

    /// <summary>Checks that the auth settings are not conflicting. Throws if AllowAnonymous is combined with any authorization requirement.</summary>
    public static void Validate(EndpointAuth auth)
    {
        if (!auth.AllowAnonymous)
            return;

        OperationHelpers.ThrowIf(
            auth.AuthorizationPolicies is { Length: > 0 },
            "EndpointAuth cannot have both AllowAnonymous and AuthorizationPolicies. Use either Anonymous() or RequireAuthorization(), not both.");

        OperationHelpers.ThrowIf(
            auth.AuthorizationPolicy != null, "EndpointAuth cannot have both AllowAnonymous and AuthorizationPolicy. Use either Anonymous() or RequireAuthorization(), not both.");
    }

    /// <summary>Requires the default authorization policy (authenticated user) on this endpoint.</summary>
    /// <example>
    /// <code>GetAuth = EndpointAuth.RequireAuthorization()</code>
    /// </example>
    public static EndpointAuth RequireAuthorization() => new();

    /// <summary>Requires the specified authorization policy or policies on this endpoint.</summary>
    /// <param name="policyNames">One or more policy names. The user must satisfy all policies.</param>
    /// <example>
    /// <code>UpdateAuth = EndpointAuth.RequireAuthorization("AdminOnly")</code>
    /// </example>
    public static EndpointAuth RequireAuthorization(params string[] policyNames) => new() { AuthorizationPolicies = policyNames };

    /// <summary>Requires an authorization policy built from the given settings.</summary>
    /// <param name="configure">Callback that configures the policy (for example RequireRole, RequireClaim).</param>
    /// <example>
    /// <code>
    /// UpdateAuth = EndpointAuth.RequireAuthorization(p => p.RequireRole("Admin").RequireClaim("scope", "write"))
    /// </code>
    /// </example>
    public static EndpointAuth RequireAuthorization(Action<AuthorizationPolicyBuilder> configure)
    {
        var builder = new AuthorizationPolicyBuilder();
        configure(builder);
        return new() { AuthorizationPolicy = builder.Build() };
    }

    /// <summary>Allows anonymous access on this endpoint.</summary>
    /// <example>
    /// <code>GetAuth = EndpointAuth.Anonymous()</code>
    /// </example>
    public static EndpointAuth Anonymous() => new() { AllowAnonymous = true };

    /// <summary>Requires the user to have one of the given roles.</summary>
    /// <param name="roles">One or more role names. The user must have at least one.</param>
    /// <example>
    /// <code>UpdateAuth = EndpointAuth.RequireRole("Admin", "Editor")</code>
    /// </example>
    public static EndpointAuth RequireRole(params string[] roles) => RequireAuthorization(p => p.RequireRole(roles));

    /// <summary>Requires the user to have the given claim with one of the given values.</summary>
    /// <param name="claimType">Claim type (for example "scope", "permission").</param>
    /// <param name="allowedValues">Allowed claim values. The user must have at least one.</param>
    /// <example>
    /// <code>CreateAuth = EndpointAuth.RequireClaim("scope", "write", "admin")</code>
    /// </example>
    public static EndpointAuth RequireClaim(string claimType, params string[] allowedValues) => RequireAuthorization(p => p.RequireClaim(claimType, allowedValues));

    /// <summary>Requires the user to be authenticated (the default policy).</summary>
    /// <example>
    /// <code>GetAuth = EndpointAuth.RequireAuthenticatedUser()</code>
    /// </example>
    public static EndpointAuth RequireAuthenticatedUser() => RequireAuthorization(p => p.RequireAuthenticatedUser());

    /// <summary>Requires the user to have the given user name.</summary>
    /// <param name="userName">Required user name.</param>
    /// <example>
    /// <code>GetAuth = EndpointAuth.RequireUserName("admin@example.com")</code>
    /// </example>
    public static EndpointAuth RequireUserName(string userName) => RequireAuthorization(p => p.RequireUserName(userName));
}