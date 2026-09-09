using Microsoft.AspNetCore.Builder;

namespace Lyo.Api.ApiEndpoint;

/// <summary>
/// Translates an <see cref="EndpointAuth" /> into route conventions. Shared by the typed <see cref="ApiEndpointBuilder{TDbContext,TDbEntity,TRequest,TResponse,TKey}" /> and the
/// dynamic CRUD builder so both interpret the same settings the same way.
/// </summary>
public static class EndpointAuthorizationApplier
{
    /// <summary>
    /// Applies <paramref name="auth" /> to <paramref name="builder" />. A null <paramref name="auth" /> leaves the route untouched, inheriting whatever the host applied globally.
    /// </summary>
    public static TBuilder Apply<TBuilder>(TBuilder builder, EndpointAuth? auth)
        where TBuilder : IEndpointConventionBuilder
    {
        if (auth is null)
            return builder;

        EndpointAuth.Validate(auth);
        if (auth.AllowAnonymous)
            builder.AllowAnonymous();
        else if (auth.AuthorizationPolicy != null)
            builder.RequireAuthorization(auth.AuthorizationPolicy);
        else if (auth.AuthorizationPolicies is { Length: > 0 } policies)
            builder.RequireAuthorization(policies);
        else
            builder.RequireAuthorization();

        return builder;
    }
}
