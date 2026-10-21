using System.Collections.Concurrent;

namespace Lyo.Media.Models;

/// <summary>VAAPI render node. Lookups are thread-safe. Use <see cref="Custom" /> for another device path.</summary>
public sealed record HardwareDevice
{
    private static readonly ConcurrentDictionary<string, HardwareDevice> ById = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, HardwareDevice> ByName = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Default DRM render node (<c>/dev/dri/renderD128</c>).</summary>
    public static readonly HardwareDevice RenderD128 = Create("RenderD128", "/dev/dri/renderD128");

    /// <summary>Every built-in device path. Touching this list registers the static instances for lookup.</summary>
    public static IReadOnlyList<HardwareDevice> BuiltIns { get; } = [RenderD128];

    /// <summary>Stable display name.</summary>
    public string Name { get; }

    /// <summary>Device path passed to <c>-vaapi_device</c>.</summary>
    public string Id { get; }

    /// <summary>Device path. Same as <see cref="Id" />.</summary>
    public string Path => Id;

    private HardwareDevice(string name, string id)
    {
        Name = name;
        Id = id.Trim();
        NamedCatalog.Register(ByName, ById, this, Name, Id);
    }

    /// <summary>Looks up a device by path.</summary>
    public static HardwareDevice? TryFromId(string? id)
    {
        _ = BuiltIns;
        return NamedCatalog.TryFromId(ById, id);
    }

    /// <summary>Looks up a device by display name.</summary>
    public static HardwareDevice? TryFromName(string? name)
    {
        _ = BuiltIns;
        return NamedCatalog.TryFromName(ByName, name);
    }

    /// <summary>Returns a device for an arbitrary path. Reuses a registered instance when the path is already known.</summary>
    public static HardwareDevice Custom(string path)
    {
        _ = BuiltIns;
        return NamedCatalog.GetOrCustom(ById, path, key => new(key, key));
    }

    /// <inheritdoc />
    public override string ToString() => Name;

    private static HardwareDevice Create(string name, string id) => new(name, id);
}