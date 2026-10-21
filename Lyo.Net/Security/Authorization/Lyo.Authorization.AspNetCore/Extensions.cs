using Lyo.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Lyo.Authorization.AspNetCore;

/// <summary>DI registration for grant policies. Call it after <c>AddLyoApiTokenAuthentication</c>.</summary>
public static class Extensions
{
    /// <param name="services">Collection that already has authentication and authorization.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers <see cref="GrantAuthorizationHandler" /> and a policy provider that forwards non-grant names. A second call does nothing. The existing provider descriptor is
        /// captured here and is not resolved from inside this factory.
        /// </summary>
        public IServiceCollection AddLyoGrantAuthorization()
        {
            ArgumentHelpers.ThrowIfNull(services);
            if (services.Any(d => d.ServiceType == typeof(GrantAuthorizationRegistration)))
                return services;

            var captured = services.LastOrDefault(d => d.ServiceType == typeof(IAuthorizationPolicyProvider));
            services.AddSingleton<GrantAuthorizationRegistration>();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IAuthorizationHandler, GrantAuthorizationHandler>());
            services.AddSingleton<IAuthorizationPolicyProvider>(sp => new GrantAuthorizationPolicyProvider(CreateInner(captured, sp)));
            return services;
        }
    }

    /// <summary>Requires <paramref name="action" /> on <paramref name="resourceType" />. Same policy as <see cref="RequireGrantAttribute" />.</summary>
    public static TBuilder RequireGrant<TBuilder>(this TBuilder builder, string action, string resourceType, string? resourceIdRoute = null, string? tenantRoute = null)
        where TBuilder : IEndpointConventionBuilder
    {
        ArgumentHelpers.ThrowIfNull(builder);
        builder.RequireAuthorization(GrantPolicyNames.For(action, resourceType, resourceIdRoute, tenantRoute));
        return builder;
    }

    private static IAuthorizationPolicyProvider CreateInner(ServiceDescriptor? captured, IServiceProvider services)
    {
        if (captured?.ImplementationInstance is IAuthorizationPolicyProvider instance)
            return instance;

        if (captured?.ImplementationType is not null)
            return (IAuthorizationPolicyProvider)ActivatorUtilities.CreateInstance(services, captured.ImplementationType);

        return new DefaultAuthorizationPolicyProvider(services.GetRequiredService<IOptions<AuthorizationOptions>>());
    }

    private sealed class GrantAuthorizationRegistration;
}