using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Lyo.Authentication.Format;
using Lyo.Authentication.Models.Format;
using Lyo.Exceptions;

namespace Lyo.Authentication.OpenIdConnect.Handoff;

/// <summary>
/// Single-process <see cref="IHandoffCodeStore"/> backed by a <see cref="ConcurrentDictionary{TKey,TValue}"/>. Suitable for single-instance dev/test deployments and for hosts
/// where the consumer exchanges the code in the same process minutes that issued it. For multi-instance production deployments use a distributed implementation (Redis/Postgres).
/// </summary>
public sealed class InMemoryHandoffCodeStore : IHandoffCodeStore
{
    /// <summary>Wire prefix on every issued id (<c>lyoh_</c>).</summary>
    public const string IdPrefix = "lyoh_";

    private readonly ConcurrentDictionary<string, Entry> _entries = new(StringComparer.Ordinal);
    private readonly Func<DateTime> _now;

    /// <summary>Creates a store using <see cref="DateTime.UtcNow"/> for expiry checks.</summary>
    public InMemoryHandoffCodeStore()
        : this(static () => DateTime.UtcNow) { }

    /// <summary>Creates a store with an injectable clock for tests.</summary>
    public InMemoryHandoffCodeStore(Func<DateTime> nowProvider)
    {
        ArgumentHelpers.ThrowIfNull(nowProvider);
        _now = nowProvider;
    }

    /// <summary>Generates a fresh handoff id using a cryptographically secure RNG (16 bytes → ~122 bits of entropy). Wire form <c>lyoh_&lt;base64url&gt;</c>.</summary>
    public static string NewId()
    {
        var bytes = new byte[16];
        RandomNumberGenerator.Fill(bytes);
        return IdPrefix + Base64Url.Encode(bytes);
    }

    /// <inheritdoc/>
    public Task StoreAsync(LyoHandoffCode code, TimeSpan ttl, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(code);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(code.Id);
        if (ttl <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(ttl), "Handoff code TTL must be positive.");

        var expires = _now() + ttl;
        _entries[code.Id] = new(code, expires);
        SweepExpired();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<LyoHandoffCode?> ConsumeAsync(string id, string callerOrigin, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(callerOrigin))
            return Task.FromResult<LyoHandoffCode?>(null);

        if (!_entries.TryRemove(id, out var entry))
            return Task.FromResult<LyoHandoffCode?>(null);

        if (_now() >= entry.ExpiresAt)
            return Task.FromResult<LyoHandoffCode?>(null);

        if (!string.Equals(entry.Code.IssuedTo, callerOrigin, StringComparison.OrdinalIgnoreCase))
            return Task.FromResult<LyoHandoffCode?>(null);

        return Task.FromResult<LyoHandoffCode?>(entry.Code);
    }

    private void SweepExpired()
    {
        var now = _now();
        foreach (var kvp in _entries) {
            if (now >= kvp.Value.ExpiresAt)
                _entries.TryRemove(kvp.Key, out _);
        }
    }

    private readonly record struct Entry(LyoHandoffCode Code, DateTime ExpiresAt);
}
