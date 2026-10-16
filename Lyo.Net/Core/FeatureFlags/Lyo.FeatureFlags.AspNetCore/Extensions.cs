using Lyo.Exceptions;
using Lyo.FeatureFlags;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
            app.Properties[FeatureFlagCaller.OptionsKey] = options;
            return app.UseMiddleware<FeatureFlagMiddleware>(options);
        }
    }

    /// <param name="endpoints">Endpoint route builder. Call this only when the host wants the enabled-name list.</param>
    extension(IEndpointRouteBuilder endpoints)
    {
        /// <summary>
        /// Maps <c>GET /features</c> to the flag names <see cref="IFeatureFlagService.IsEnabled" /> returns true for the current caller.
        /// The caller is the same one <see cref="FeatureFlagMiddleware" /> builds. Disabled names are omitted. Rules and percentages are not returned.
        /// No registered service returns an empty 500.
        /// </summary>
        public RouteHandlerBuilder MapFeatureFlags()
        {
            ArgumentHelpers.ThrowIfNull(endpoints);
            var options = endpoints is IApplicationBuilder app
                && app.Properties.TryGetValue(FeatureFlagCaller.OptionsKey, out var stored)
                && stored is FeatureFlagEndpointOptions configured
                ? configured
                : new FeatureFlagEndpointOptions();

            return endpoints.MapGet("/features", (HttpContext context) => EnabledNames(context, options));
        }

        /// <summary>
        /// Maps <c>GET /features/manage</c> and <c>PUT /features/manage/{name}</c>.
        /// The list includes every flag's name and mode. The put sets that flag on or off and clears its percentage and rules.
        /// An unknown name is an empty 404. No registered service is an empty 500. The host authorizes these routes.
        /// </summary>
        public RouteGroupBuilder MapFeatureFlagManagement()
        {
            ArgumentHelpers.ThrowIfNull(endpoints);
            var group = endpoints.MapGroup("/features/manage");
            group.MapGet("", ListFlags);
            group.MapPut("/{name}", SetEnabled);
            return group;
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

    private static IResult EnabledNames(HttpContext context, FeatureFlagEndpointOptions options)
    {
        var service = context.RequestServices.GetService<IFeatureFlagService>();
        if (service is null)
            return Results.StatusCode(StatusCodes.Status500InternalServerError);

        var caller = FeatureFlagCaller.Create(context, options);
        var monitor = context.RequestServices.GetService<IOptionsMonitor<FeatureFlagOptions>>();
        var names = new List<string>();
        if (monitor is not null)
        {
            foreach (var pair in monitor.CurrentValue.Flags)
            {
                if (service.IsEnabled(pair.Key, caller))
                    names.Add(pair.Key);
            }
        }

        return Results.Ok(names);
    }

    private static IResult ListFlags(HttpContext context)
    {
        var monitor = CurrentFlags(context);
        if (monitor is null)
            return Results.StatusCode(StatusCodes.Status500InternalServerError);

        var rows = new List<FeatureFlagState>();
        foreach (var pair in monitor.Flags)
            rows.Add(new FeatureFlagState { Name = pair.Key, Mode = pair.Value.Mode.ToString() });

        return Results.Ok(rows);
    }

    private static IResult SetEnabled(HttpContext context, string name, FeatureFlagEnabledRequest body)
    {
        ArgumentHelpers.ThrowIfNull(body);
        var monitor = CurrentFlags(context);
        var updater = context.RequestServices.GetService<IFeatureFlagUpdater>();
        if (monitor is null || updater is null)
            return Results.StatusCode(StatusCodes.Status500InternalServerError);

        var next = FeatureFlagUpdate.WithEnabled(monitor, name, body.Enabled);
        if (next is null)
            return Results.StatusCode(StatusCodes.Status404NotFound);
        if (!updater.TryApply(next))
            return Results.StatusCode(StatusCodes.Status500InternalServerError);

        return Results.Ok(new FeatureFlagState { Name = name, Mode = body.Enabled ? nameof(FeatureFlagMode.On) : nameof(FeatureFlagMode.Off) });
    }

    private static FeatureFlagOptions? CurrentFlags(HttpContext context)
    {
        if (context.RequestServices.GetService<IFeatureFlagService>() is null)
            return null;

        return context.RequestServices.GetService<IOptionsMonitor<FeatureFlagOptions>>()?.CurrentValue;
    }
}
