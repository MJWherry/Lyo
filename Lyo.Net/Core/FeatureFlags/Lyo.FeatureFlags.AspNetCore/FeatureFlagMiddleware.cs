using Lyo.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.FeatureFlags.AspNetCore;

/// <summary>
/// Returns 404 when a <see cref="RequireFeatureAttribute" /> flag is off, and 500 when a gated route has no <c>IFeatureFlagService</c>. Register it after <c>UseRouting</c>
/// and authentication, and before <c>UseAuthorization</c>, so a disabled flag does not become 401. Open routes do not resolve the service.
/// </summary>
public sealed class FeatureFlagMiddleware(RequestDelegate next, FeatureFlagEndpointOptions options)
{
    /// <summary>Gates the endpoint when it carries one or more <see cref="RequireFeatureAttribute" /> values.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentHelpers.ThrowIfNull(context);
        var required = context.GetEndpoint()?.Metadata.GetOrderedMetadata<RequireFeatureAttribute>();
        if (required is null || required.Count == 0) {
            await next(context).ConfigureAwait(false);
            return;
        }

        var service = context.RequestServices.GetService<IFeatureFlagService>();
        if (service is null) {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            return;
        }

        var caller = FeatureFlagCaller.Create(context, options);
        foreach (var feature in required) {
            if (service.IsEnabled(feature.Name, caller))
                continue;

            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        await next(context).ConfigureAwait(false);
    }
}