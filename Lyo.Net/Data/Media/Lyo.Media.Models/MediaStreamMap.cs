using System.Collections.Concurrent;
using Lyo.Exceptions;

namespace Lyo.Media.Models;

/// <summary>One <c>-map</c> selector. Lookups are thread-safe. Use <see cref="Custom" /> for a selector that is not a simple stream.</summary>
public sealed record MediaStreamMap
{
    private static readonly ConcurrentDictionary<string, MediaStreamMap> ById = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>FFmpeg map selector (e.g. <c>0:v:0</c>).</summary>
    public string Spec { get; }

    /// <summary>Same as <see cref="Spec" />.</summary>
    public string Id => Spec;

    private MediaStreamMap(string spec)
    {
        Spec = spec;
        ById.TryAdd(spec, this);
    }

    /// <summary>Video stream <paramref name="stream" /> on input <paramref name="input" />.</summary>
    public static MediaStreamMap Video(int input = 0, int stream = 0) => Of(input, "v", stream);

    /// <summary>Audio stream <paramref name="stream" /> on input <paramref name="input" />.</summary>
    public static MediaStreamMap Audio(int input = 0, int stream = 0) => Of(input, "a", stream);

    /// <summary>Subtitle stream <paramref name="stream" /> on input <paramref name="input" />.</summary>
    public static MediaStreamMap Subtitle(int input = 0, int stream = 0) => Of(input, "s", stream);

    /// <summary>Returns a map for an arbitrary selector. Reuses a registered instance when the selector is already known.</summary>
    public static MediaStreamMap Custom(string spec)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(spec);
        var key = spec.Trim();
        if (ById.TryGetValue(key, out var found))
            return found;

        return ById.GetOrAdd(key, static value => new(value));
    }

    /// <inheritdoc />
    public override string ToString() => Spec;

    private static MediaStreamMap Of(int input, string kind, int stream)
    {
        ArgumentHelpers.ThrowIfLessThan(input, 0);
        ArgumentHelpers.ThrowIfLessThan(stream, 0);
        return Custom($"{input}:{kind}:{stream}");
    }
}