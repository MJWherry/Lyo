using Lyo.Authentication.Services.Jwt;
using Lyo.Exceptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Lyo.Authentication.AspNetCore.Endpoints;

/// <summary>Maps the <c>/.well-known/jwks.json</c> endpoint that publishes Lyo's Ed25519 signing key(s).</summary>
public static class JwksEndpointBuilder
{
    /// <summary>The default JWKS path.</summary>
    public const string DefaultPath = "/.well-known/jwks.json";

    /// <summary>
    /// Maps the JWKS endpoint at <paramref name="path" /> (default <c>/.well-known/jwks.json</c>). The endpoint is anonymous, returns JSON, and is safe to cache for short
    /// windows.
    /// </summary>
    public static IEndpointConventionBuilder MapLyoJwks(this IEndpointRouteBuilder endpoints, string path = DefaultPath)
    {
        ArgumentHelpers.ThrowIfNull(endpoints);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(path);
        return endpoints.MapGet(
                path, async (HttpContext ctx, JwkSetBuilder builder) => {
                    var jwks = await builder.BuildAsync(ctx.RequestAborted).ConfigureAwait(false);
                    ctx.Response.Headers.CacheControl = "public, max-age=300";
                    await ctx.Response.WriteAsJsonAsync(jwks, ctx.RequestAborted).ConfigureAwait(false);
                })
            .WithName("LyoJwks")
            .AllowAnonymous();
    }
}