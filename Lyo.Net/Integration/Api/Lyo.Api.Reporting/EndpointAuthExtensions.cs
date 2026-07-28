using Lyo.Api.ApiEndpoint;
using Microsoft.AspNetCore.Builder;

namespace Lyo.Api.Reporting;

internal static class EndpointAuthExtensions
{
    /// <summary>Mirrors private <c>ApiEndpointBuilder.ApplyAuthorization</c> for custom MapPost routes.</summary>
    public static IEndpointConventionBuilder ApplyEndpointAuth(this IEndpointConventionBuilder builder, EndpointAuth? endpointAuth)
    {
        if (endpointAuth is null)
            return builder;

        EndpointAuth.Validate(endpointAuth);
        if (endpointAuth.AllowAnonymous)
            return builder.AllowAnonymous();

        if (endpointAuth.AuthorizationPolicy != null)
            return builder.RequireAuthorization(endpointAuth.AuthorizationPolicy);

        if (endpointAuth.AuthorizationPolicies is { Length: > 0 })
            return builder.RequireAuthorization(endpointAuth.AuthorizationPolicies);

        return builder.RequireAuthorization();
    }
}