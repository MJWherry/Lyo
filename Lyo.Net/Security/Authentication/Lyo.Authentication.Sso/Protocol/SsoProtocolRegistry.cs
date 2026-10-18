using Lyo.Exceptions;
using Lyo.Exceptions.Models;

namespace Lyo.Authentication.Sso.Protocol;

/// <summary>Maps a provider name to the single <see cref="ISsoProtocol" /> that owns it.</summary>
public sealed class SsoProtocolRegistry
{
    private readonly Dictionary<string, ISsoProtocol> _byProvider;

    /// <summary>Builds the map from the protocols registered in DI. A repeated provider name throws <see cref="ConflictException" />.</summary>
    public SsoProtocolRegistry(IEnumerable<ISsoProtocol> protocols)
    {
        ArgumentHelpers.ThrowIfNull(protocols);
        _byProvider = new(StringComparer.Ordinal);
        foreach (var protocol in protocols) {
            ArgumentHelpers.ThrowIfNull(protocol);
            foreach (var name in protocol.ProviderNames) {
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                if (!_byProvider.TryAdd(name, protocol))
                    throw new ConflictException($"SSO provider '{name}' is already registered.");
            }
        }
    }

    /// <summary>Returns the protocol that owns <paramref name="name" />.</summary>
    /// <exception cref="NotFoundException">Thrown when no protocol registered that name.</exception>
    public ISsoProtocol Get(string name)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name);
        if (!_byProvider.TryGetValue(name, out var protocol))
            throw new NotFoundException($"Authentication provider '{name}' is not registered.");

        return protocol;
    }
}
