using Lyo.ContentThreatScan;

namespace Lyo.ContentThreatScan.Intel;

/// <summary>Simple TTL-backed digest cache keyed by lowercase hex SHA256.</summary>
public sealed class ReputationDigestLookupCache
{
    private sealed class Holder
    {
        public Holder(ExternalReputationEnvelope envelope, DateTime expiryUtc)
        {
            Envelope = envelope;
            ExpiryUtc = expiryUtc;
        }

        public ExternalReputationEnvelope Envelope { get; }
        public DateTime ExpiryUtc { get; }
    }

    private readonly Dictionary<string, Holder> _map = new(StringComparer.Ordinal);
    private readonly object _gate = new();
    private readonly int _cap;

    public ReputationDigestLookupCache(int capacity)
    {
        if (capacity <= 1)
            throw new ArgumentOutOfRangeException(nameof(capacity));
        _cap = capacity;
    }

    public bool TryGet(string hexLower, DateTime utcNow, out ExternalReputationEnvelope envelope)
    {
        lock (_gate) {
            if (!_map.TryGetValue(hexLower, out var holder) || holder.ExpiryUtc <= utcNow) {
                _map.Remove(hexLower);
                envelope = ExternalReputationEnvelope.Empty;
                return false;
            }

            envelope = holder.Envelope;
            return true;
        }
    }

    public void Put(string hexLower, ExternalReputationEnvelope envelope, DateTime utcNow, TimeSpan ttl)
    {
        if (ttl <= TimeSpan.Zero)
            return;

        lock (_gate) {
            if (_map.Count >= _cap)
                EvictStaleOrRandom(utcNow);

            _map[hexLower] = new(envelope, utcNow + ttl);
        }
    }

    void EvictStaleOrRandom(DateTime utcNow)
    {
        var staleKeys = _map.Where(kv => kv.Value.ExpiryUtc <= utcNow).Select(kv => kv.Key).ToList();
        foreach (var key in staleKeys)
            _map.Remove(key);

        if (_map.Count < _cap)
            return;

        foreach (var key in _map.Keys.Take((_cap / 2) + 1))
            _map.Remove(key);
    }
}
