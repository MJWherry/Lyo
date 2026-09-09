using System.Collections.Concurrent;

namespace Lyo.Media.Models;

/// <summary>
/// Extensible pixel-layout identifier. Lookups are thread-safe. Use <see cref="Custom" /> for a layout that is not listed.
/// </summary>
public sealed record PixelFormat
{
    private static readonly ConcurrentDictionary<string, PixelFormat> ById = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, PixelFormat> ByName = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Planar YUV 4:2:0 (<c>yuv420p</c>), the usual H.264/MPEG-4 layout.</summary>
    public static readonly PixelFormat Yuv420p = Create("Yuv420p", "yuv420p");

    /// <summary>Packed RGB 24-bit (<c>rgb24</c>).</summary>
    public static readonly PixelFormat Rgb24 = Create("Rgb24", "rgb24");

    /// <summary>8-bit grayscale (<c>gray</c>).</summary>
    public static readonly PixelFormat Gray = Create("Gray", "gray");

    /// <summary>NV12 (<c>nv12</c>).</summary>
    public static readonly PixelFormat Nv12 = Create("Nv12", "nv12");

    /// <summary>Every built-in pixel format. Touching this list registers the static instances for lookup.</summary>
    public static IReadOnlyList<PixelFormat> BuiltIns { get; } = [Yuv420p, Rgb24, Gray, Nv12];

    /// <summary>Stable display name (e.g. <c>Yuv420p</c>).</summary>
    public string Name { get; }

    /// <summary>Canonical layout id (e.g. <c>yuv420p</c>). Backends use this as the pixel-format name.</summary>
    public string Id { get; }

    private PixelFormat(string name, string id)
    {
        Name = name;
        Id = NamedCatalog.NormalizeId(id);
        NamedCatalog.Register(ByName, ById, this, Name, Id);
    }

    /// <summary>Looks up a built-in or previously created custom format by <see cref="Id" />.</summary>
    public static PixelFormat? TryFromId(string? id)
    {
        _ = BuiltIns;
        return NamedCatalog.TryFromId(ById, id);
    }

    /// <summary>Looks up a format by display <see cref="Name" />.</summary>
    public static PixelFormat? TryFromName(string? name)
    {
        _ = BuiltIns;
        return NamedCatalog.TryFromName(ByName, name);
    }

    /// <summary>Returns a format for an arbitrary layout id. Reuses a registered instance when the id is already known.</summary>
    public static PixelFormat Custom(string id)
    {
        _ = BuiltIns;
        return NamedCatalog.GetOrCustom(ById, id, key => new PixelFormat(key, key));
    }

    /// <inheritdoc />
    public override string ToString() => Name;

    private static PixelFormat Create(string name, string id) => new(name, id);
}
