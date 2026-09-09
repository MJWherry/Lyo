using Lyo.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Authentication.Scopes;

/// <summary>Container helpers for registering scopes.</summary>
public static class ScopeRegistrationExtensions
{
    /// <summary>Registers one scope. Resolves <see cref="ScopeRegistry" /> from the collection — call <c>AddLyoAuthentication</c> first.</summary>
    /// <param name="services">Collection that receives the scope registration.</param>
    /// <param name="name">Scope name (lowercase dot-notation).</param>
    /// <param name="description">Human-readable description.</param>
    /// <param name="implies">Other scopes this scope grants transitively.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddScope(this IServiceCollection services, string name, string description, params string[] implies)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name);
        ArgumentHelpers.ThrowIfNull(description);
        ArgumentHelpers.ThrowIfNull(implies);
        var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ScopeRegistry));
        OperationHelpers.ThrowIfNull(descriptor, "ScopeRegistry is not registered. Call AddLyoAuthentication before adding scopes.");
        var registry = descriptor!.ImplementationInstance as ScopeRegistry ??
            throw new InvalidOperationException("ScopeRegistry is registered but has no implementation instance.");

        registry.Register(name, description, implies);
        return services;
    }
}