using Lyo.Exceptions;
using Microsoft.AspNetCore.Builder;

namespace Lyo.Authentication.AspNetCore.Authorization;

/// <summary>Convenience extensions for adding scope policies to minimal-API endpoints.</summary>
public static class EndpointAuthScopeExtensions
{
    /// <summary>Requires the caller to hold the given <paramref name="scope"/>. Adds a policy named <c>scope:&lt;scope&gt;</c>.</summary>
    public static TBuilder RequireScope<TBuilder>(this TBuilder builder, string scope)
        where TBuilder : IEndpointConventionBuilder
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(scope);
        builder.RequireAuthorization(ScopeAuthorizationPolicyProvider.PolicyPrefix + scope);
        return builder;
    }
}
