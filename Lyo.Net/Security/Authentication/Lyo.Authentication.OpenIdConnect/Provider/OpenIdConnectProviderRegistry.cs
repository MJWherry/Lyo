using System.Collections.Concurrent;
using Lyo.Exceptions;

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
    public void Register(IOpenIdConnectProvider provider)
    {
        ArgumentHelpers.ThrowIfNull(provider);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(provider.Name);
        if (!_providers.TryAdd(provider.Name, provider))
            throw new InvalidOperationException($"OIDC provider '{provider.Name}' is already registered.");
    }

    /// <summary>Returns the provider with the given name. Throws when unknown.</summary>
    public IOpenIdConnectProvider Get(string name)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name);
        if (!_providers.TryGetValue(name, out var provider))
            throw new InvalidOperationException($"OIDC provider '{name}' is not registered.");

        return provider;
    }

    /// <summary>Returns the provider with the given name, or <c>null</c> if unknown.</summary>
    public IOpenIdConnectProvider? TryGet(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        return _providers.TryGetValue(name, out var provider) ? provider : null;
    }
}