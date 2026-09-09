using Lyo.Common.Core.Net;
using Lyo.Common.Metadata.Records;
using Lyo.Exceptions;

namespace Lyo.Http.Client.Session;

/// <summary>Ordered header set used by plans and sessions. Never overwrites a correlation header that is already set.</summary>
public sealed class LyoHttpHeaderBag
{
    private readonly List<KeyValuePair<string, string>> _pairs = [];

    /// <summary>Current headers in insertion order. Later <see cref="Set" /> updates replace the previous value for the same name (ordinal ignore case).</summary>
    public IReadOnlyList<KeyValuePair<string, string>> Items => _pairs;

    /// <summary>Sets or replaces <paramref name="name" />. Correlation headers already present are left unchanged.</summary>
    public LyoHttpHeaderBag Set(string name, string value)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name);
        ArgumentHelpers.ThrowIfNull(value);
        if (IsCorrelation(name) && TryGet(name, out _))
            return this;

        Remove(name);
        _pairs.Add(new(name, value));
        return this;
    }

    /// <summary>Copies all pairs from <paramref name="headers" /> through <see cref="Set" />.</summary>
    public LyoHttpHeaderBag Merge(IEnumerable<KeyValuePair<string, string>> headers)
    {
        ArgumentHelpers.ThrowIfNull(headers);
        foreach (var pair in headers)
            Set(pair.Key, pair.Value);

        return this;
    }

    /// <summary>Removes every header whose name matches <paramref name="name" />.</summary>
    public bool Remove(string name)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name);
        var removed = _pairs.RemoveAll(p => string.Equals(p.Key, name, StringComparison.OrdinalIgnoreCase));
        return removed > 0;
    }

    /// <summary>Clears all headers.</summary>
    public void Clear() => _pairs.Clear();

    /// <summary>Looks up the first value for <paramref name="name" />.</summary>
    public bool TryGet(string name, out string value)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name);
        foreach (var pair in _pairs) {
            if (!string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase))
                continue;

            value = pair.Value;
            return true;
        }

        value = "";
        return false;
    }

    /// <summary>Applies headers onto <paramref name="request" />, skipping names in <paramref name="omit" />.</summary>
    public void ApplyTo(HttpRequestMessage request, IReadOnlyCollection<string>? omit = null)
    {
        ArgumentHelpers.ThrowIfNull(request);
        foreach (var pair in _pairs) {
            if (omit != null && omit.Any(n => string.Equals(n, pair.Key, StringComparison.OrdinalIgnoreCase)))
                continue;

            if (string.Equals(pair.Key, HttpHeaderInfo.Cookie, StringComparison.OrdinalIgnoreCase)
                && request.Headers.Contains(HttpHeaderInfo.Cookie))
                continue;

            request.Headers.TryAddWithoutValidation(pair.Key, pair.Value);
        }
    }

    private static bool IsCorrelation(string name)
        => LyoHttpHeaders.CorrelationIds.Any(h => string.Equals(h, name, StringComparison.OrdinalIgnoreCase));
}
