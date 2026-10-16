using System.Security.Claims;
using Lyo.FeatureFlags;

namespace Lyo.FeatureFlags.AspNetCore;

internal static class FeatureFlagCaller
{
    public const string OptionsKey = "Lyo.FeatureFlags.EndpointOptions";

    public static FeatureFlagContext Create(HttpContext context, FeatureFlagEndpointOptions options)
    {
        if (options.ContextFactory is not null)
            return options.ContextFactory(context) ?? FeatureFlagContext.Create().Build();

        return Map(context, options);
    }

    private static FeatureFlagContext Map(HttpContext context, FeatureFlagEndpointOptions options)
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
