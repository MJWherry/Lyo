using System.Collections.Concurrent;
using Lyo.Common.Extensions;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;

namespace Lyo.Authentication.OpenIdConnect.Provider;

/// <summary>Registry of <see cref="IOpenIdConnectProvider" /> instances keyed by <see cref="IOpenIdConnectProvider.Name" />.</summary>
public sealed class OpenIdConnectProviderRegistry
{
    private readonly ConcurrentDictionary<string, IOpenIdConnectProvider> _providers = new(StringComparer.Ordinal);

    /// <summary>All registered providers in registration order.</summary>
    public IReadOnlyCollection<IOpenIdConnectProvider> All => _providers.Values.ToArray();

    /// <summary>Creates a registry and seeds it with the supplied providers (typically registered via DI).</summary>
    public OpenIdConnectProviderRegistry(IEnumerable<IOpenIdConnectProvider> providers)
    {
        ArgumentHelpers.ThrowIfNull(providers);
        foreach (var provider in providers)
            Register(provider);
    }

    /// <summary>Registers a provider. Throws when the name collides with an existing entry.</summary>
    /// <exception cref="ConflictException">Thrown when a provider with the same name is already registered.</exception>
    public void Register(IOpenIdConnectProvider provider)
    {
        ArgumentHelpers.ThrowIfNull(provider);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(provider.Name);
        if (!_providers.TryAdd(provider.Name, provider))
            throw new ConflictException($"OIDC provider '{provider.Name}' is already registered.");
    }

    /// <summary>Returns the provider with the given name. Throws when unknown.</summary>
    /// <exception cref="NotFoundException">Thrown when no provider with the given name is registered.</exception>
    public IOpenIdConnectProvider Get(string name)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name);
        if (!_providers.TryGetValue(name, out var provider))
            throw new NotFoundException($"OIDC provider '{name}' is not registered.");

        return provider;
    }

    /// <summary>Returns the provider with the given name, or <c>null</c> if unknown.</summary>
    public IOpenIdConnectProvider? TryGet(string name)
    {
        if (name.IsNullOrWhitespace())
            return null;

        return _providers.TryGetValue(name, out var provider) ? provider : null;
    }
}