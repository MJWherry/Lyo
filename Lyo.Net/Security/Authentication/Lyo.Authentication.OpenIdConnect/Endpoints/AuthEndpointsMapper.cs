using Lyo.Authentication.Sso.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Lyo.Authentication.OpenIdConnect.Endpoints;

/// <summary>Forwards <c>MapLyoAuthEndpoints</c> to <c>Lyo.Authentication.Sso</c>.</summary>
public static class AuthEndpointsMapper
{
    /// <summary>Maps the SSO auth endpoints. Prefer <see cref="Sso.Endpoints.AuthEndpointsMapper.MapLyoAuthEndpoints" />.</summary>
    [Obsolete("Call Lyo.Authentication.Sso.Endpoints.AuthEndpointsMapper.MapLyoAuthEndpoints.")]
    public static IEndpointConventionBuilder MapLyoAuthEndpoints(this IEndpointRouteBuilder endpoints, string prefix = "/auth")
        => Sso.Endpoints.AuthEndpointsMapper.MapLyoAuthEndpoints(endpoints, prefix);
}
