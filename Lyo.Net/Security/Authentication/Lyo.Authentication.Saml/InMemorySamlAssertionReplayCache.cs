using System.Collections.Concurrent;

namespace Lyo.Authentication.Saml;

/// <summary>In-memory <see cref="ISamlAssertionReplayCache" />. Expired ids are dropped on the next remember.</summary>
public sealed class InMemorySamlAssertionReplayCache : ISamlAssertionReplayCache
{
    private readonly ConcurrentDictionary<string, DateTime> _seen = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public bool TryRemember(string assertionId, DateTime notOnOrAfter)
    {
        var now = DateTime.UtcNow;
        foreach (var entry in _seen) {
            if (entry.Value <= now)
                _seen.TryRemove(entry.Key, out var _);
        }

        if (_seen.TryGetValue(assertionId, out var until) && until > now)
            return false;

        _seen[assertionId] = notOnOrAfter;
        return true;
    }
}