using System.Collections.Concurrent;
using System.Globalization;
using Lyo.Exceptions;

namespace Lyo.Media.Models;

/// <summary>Output frame rate. Lookups are thread-safe. Use <see cref="FromDouble" /> for a rate that is not listed.</summary>
public sealed record VideoFrameRate
{
    private static readonly ConcurrentDictionary<string, VideoFrameRate> ById = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, VideoFrameRate> ByName = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>12 fps, the GIF default.</summary>
    public static readonly VideoFrameRate Fps12 = Create("Fps12", 12);

    /// <summary>24 fps.</summary>
    public static readonly VideoFrameRate Fps24 = Create("Fps24", 24);

    /// <summary>25 fps.</summary>
    public static readonly VideoFrameRate Fps25 = Create("Fps25", 25);

    /// <summary>30 fps.</summary>
    public static readonly VideoFrameRate Fps30 = Create("Fps30", 30);

    /// <summary>60 fps.</summary>
    public static readonly VideoFrameRate Fps60 = Create("Fps60", 60);

    /// <summary>Every built-in rate. Touching this list registers the static instances for lookup.</summary>
    public static IReadOnlyList<VideoFrameRate> BuiltIns { get; } = [Fps12, Fps24, Fps25, Fps30, Fps60];

    /// <summary>Stable display name (e.g. <c>Fps30</c>).</summary>
    public string Name { get; }

    /// <summary>Decimal frame-rate token.</summary>
    public string Id { get; }

    /// <summary>Frames per second.</summary>
    public double Value { get; }

    private VideoFrameRate(string name, double value)
    {
        ArgumentHelpers.ThrowIfLessThanOrEqual(value, 0, message: "Frame rate must be positive.");
        Name = name;
        Value = value;
        Id = value.ToString("G", CultureInfo.InvariantCulture);
        NamedCatalog.Register(ByName, ById, this, Name, Id);
    }

    /// <summary>Looks up a rate by numeric token.</summary>
    public static VideoFrameRate? TryFromId(string? id)
    {
        _ = BuiltIns;
        return NamedCatalog.TryFromId(ById, id);
    }

    /// <summary>Looks up a rate by display name.</summary>
    public static VideoFrameRate? TryFromName(string? name)
    {
        _ = BuiltIns;
        return NamedCatalog.TryFromName(ByName, name);
    }

    /// <summary>Returns a rate for <paramref name="value" />. Reuses a built-in when the value is already known.</summary>
    public static VideoFrameRate FromDouble(double value)
    {
        _ = BuiltIns;
        ArgumentHelpers.ThrowIfLessThanOrEqual(value, 0, message: "Frame rate must be positive.");
        var key = value.ToString("G", CultureInfo.InvariantCulture);
        return NamedCatalog.GetOrCustom(ById, key, _ => new(key, value));
    }

    /// <inheritdoc />
    public override string ToString() => Name;

    private static VideoFrameRate Create(string name, double value) => new(name, value);
}