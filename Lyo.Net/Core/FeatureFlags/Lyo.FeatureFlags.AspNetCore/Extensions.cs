using Lyo.Exceptions;
using Lyo.FeatureFlags;

namespace Lyo.FeatureFlags.AspNetCore;

/// <summary>Pipeline and endpoint registration for <see cref="FeatureFlagMiddleware" />. This does not register <c>IFeatureFlagService</c>.</summary>
public static class Extensions
{
    /// <param name="app">Application pipeline.</param>
    extension(IApplicationBuilder app)
    {
        /// <summary>
        /// Adds <see cref="FeatureFlagMiddleware" />. Call this after <c>UseRouting</c> and authentication, and before <c>UseAuthorization</c>.
        /// The host still calls <c>AddFeatureFlags</c> or <c>AddFeatureFlagsFromConfiguration</c>.
        /// </summary>
        /// <param name="configure">Optional tenant claim and context factory. Null keeps the default claim mapping.</param>
        public IApplicationBuilder UseFeatureFlags(Action<FeatureFlagEndpointOptions>? configure = null)
        {
            ArgumentHelpers.ThrowIfNull(app);
            var options = new FeatureFlagEndpointOptions();
            configure?.Invoke(options);
            return app.UseMiddleware<FeatureFlagMiddleware>(options);
        }
    }

    /// <summary>Attaches <see cref="RequireFeatureAttribute" /> so the middleware can see it. Use this on a route group or a lambda.</summary>
    public static TBuilder RequireFeature<TBuilder>(this TBuilder builder, string name)
        where TBuilder : IEndpointConventionBuilder
    {
        ArgumentHelpers.ThrowIfNull(builder);
        builder.WithMetadata(new RequireFeatureAttribute(name));
        return builder;
    }
}
