using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Lyo.Exceptions;

namespace Lyo.Common.Core.Caching;

/// <summary>
/// Thread-safe memoization table with a hard entry cap and approximate least-recently-used eviction.
/// </summary>
/// <remarks>
/// Prefer this over a bare <see cref="ConcurrentDictionary{TKey,TValue}" /> when the key is derived from caller-supplied input — a filter value, a route segment, a
/// projection shape — because such a dictionary is an unbounded memory leak that any client can drive. Eviction is approximate: reads only stamp a counter, so nothing is
/// reordered on the hot path, and a trim removes the coldest quarter in one pass rather than a single entry per insert. Values are never disposed on eviction, so do not store
/// resources that need deterministic cleanup.
/// </remarks>
/// <typeparam name="TKey">Cache key. Needs a stable hash code and equality.</typeparam>
/// <typeparam name="TValue">Cached value.</typeparam>
public sealed class BoundedCache<TKey, TValue> where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, Entry> _entries;
    private readonly int _capacity;
    private readonly int _trimTo;
    private long _tick;
    private int _trimming;

    /// <summary>Builds a <see cref="BoundedCache{TKey,TValue}" /> with the given capacity.</summary>
    /// <param name="capacity">Maximum entries retained before a trim runs. Must be positive.</param>
    /// <param name="comparer">Optional key comparer; defaults to <see cref="EqualityComparer{T}.Default" />.</param>
    public BoundedCache(int capacity, IEqualityComparer<TKey>? comparer = null)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Capacity must be positive.");

        _capacity = capacity;
        _trimTo = Math.Max(1, capacity - (capacity / 4));
        _entries = comparer is null ? new() : new(comparer);
    }

    /// <summary>Current entry count. Can briefly exceed capacity while a trim is in flight.</summary>
    public int Count => _entries.Count;

    /// <summary>
    /// Returns the cached value for <paramref name="key" />, calling <paramref name="factory" /> on a miss.
    /// </summary>
    /// <remarks>
    /// The factory may run concurrently for the same key under contention, exactly as <see cref="ConcurrentDictionary{TKey,TValue}.GetOrAdd(TKey, Func{TKey, TValue})" /> does;
    /// only one result is retained. Keep the factory pure and side-effect free.
    /// </remarks>
    /// <param name="key">Cache key.</param>
    /// <param name="factory">Computes the value on a miss.</param>
    public TValue GetOrAdd(TKey key, Func<TKey, TValue> factory)
    {
        ArgumentHelpers.ThrowIfNull(key);
        ArgumentHelpers.ThrowIfNull(factory);

        if (_entries.TryGetValue(key, out var existing)) {
            existing.Touch(NextTick());
            return existing.Value;
        }

        var entry = _entries.GetOrAdd(key, k => new(factory(k), NextTick()));
        TrimIfNeeded();
        return entry.Value;
    }

    /// <summary>Drops every entry. Intended for tests and for hosts that rebuild their metadata at runtime.</summary>
    public void Clear() => _entries.Clear();

    private long NextTick() => Interlocked.Increment(ref _tick);

    private void TrimIfNeeded()
    {
        if (_entries.Count <= _capacity)
            return;

        // One trimmer at a time; everyone else keeps serving reads against a briefly oversized table rather than queuing on a lock.
        if (Interlocked.CompareExchange(ref _trimming, 1, 0) != 0)
            return;

        try {
            var excess = _entries.Count - _trimTo;
            if (excess <= 0)
                return;

            foreach (var key in _entries.ToArray().OrderBy(p => Interlocked.Read(ref p.Value.LastUsed)).Take(excess).Select(p => p.Key))
                _entries.TryRemove(key, out _);
        }
        finally {
            Interlocked.Exchange(ref _trimming, 0);
        }
    }

    private sealed class Entry(TValue value, long lastUsed)
    {
        public readonly TValue Value = value;
        public long LastUsed = lastUsed;

        public void Touch(long tick) => Interlocked.Exchange(ref LastUsed, tick);
    }
}
