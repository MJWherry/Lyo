using System.Security.Claims;
using Lyo.Exceptions;
using Lyo.FeatureFlags;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.FeatureFlags.AspNetCore;

/// <summary>
/// Returns 404 when a <see cref="RequireFeatureAttribute" /> flag is off, and 500 when a gated route has no <c>IFeatureFlagService</c>.
/// Register it after <c>UseRouting</c> and authentication, and before <c>UseAuthorization</c>, so a disabled flag does not become 401.
/// Open routes do not resolve the service.
/// </summary>
public sealed class FeatureFlagMiddleware(RequestDelegate next, FeatureFlagEndpointOptions options)
{
    /// <summary>Gates the endpoint when it carries one or more <see cref="RequireFeatureAttribute" /> values.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentHelpers.ThrowIfNull(context);
        var required = context.GetEndpoint()?.Metadata.GetOrderedMetadata<RequireFeatureAttribute>();
        if (required is null || required.Count == 0)
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        var service = context.RequestServices.GetService<IFeatureFlagService>();
        if (service is null)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            return;
        }

        var caller = options.ContextFactory is null ? MapCaller(context) : options.ContextFactory(context) ?? FeatureFlagContext.Create().Build();
        foreach (var feature in required)
        {
            if (service.IsEnabled(feature.Name, caller))
                continue;

            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        await next(context).ConfigureAwait(false);
    }

    private FeatureFlagContext MapCaller(HttpContext context)
    {
        var user = context.User;
        var builder = FeatureFlagContext.Create();
        var userId = First(user, "lyo:user", "sub", ClaimTypes.NameIdentifier) ?? Named(user);
        if (userId is not null)
            builder.WithUser(userId);

        if (!string.IsNullOrWhiteSpace(options.TenantClaimType))
        {
            var tenant = user.FindFirst(options.TenantClaimType)?.Value;
            if (!string.IsNullOrWhiteSpace(tenant))
                builder.WithTenant(tenant);
        }

        foreach (var claim in user.Claims)
        {
            if (string.IsNullOrWhiteSpace(claim.Type) || string.IsNullOrWhiteSpace(claim.Value))
                continue;

            if (claim.Type == "scope" && claim.Value.Contains(' '))
            {
                foreach (var scope in claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    builder.WithClaim("scope", scope);
                continue;
            }

            builder.WithClaim(claim.Type, claim.Value);
        }

        return builder.Build();
    }

    private static string? First(ClaimsPrincipal user, params string[] types)
    {
        foreach (var type in types)
        {
            var value = user.FindFirst(type)?.Value;
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return null;
    }

    private static string? Named(ClaimsPrincipal user)
    {
        var name = user.Identity?.Name;
        return string.IsNullOrWhiteSpace(name) ? null : name;
    }
}
