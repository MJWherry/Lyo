using System.Collections.Concurrent;
using System.Diagnostics;

namespace Lyo.Media.Models;

/// <summary>
/// Hardware encoder device. Lookups are thread-safe. <see cref="None" /> forces the CPU. A null option inherits the host setting. <see cref="Custom" /> does not invent
/// encoder names such as <c>h264_amf</c>.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record HardwareAccel
{
    private static readonly ConcurrentDictionary<string, HardwareAccel> ById = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, HardwareAccel> ByName = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Software encode. Overrides a host GPU setting.</summary>
    public static readonly HardwareAccel None = Create("None", "none");

    /// <summary>NVIDIA NVENC. Decode flag is <c>cuda</c>.</summary>
    public static readonly HardwareAccel Nvenc = Create("Nvenc", "nvenc");

    /// <summary>Intel Quick Sync.</summary>
    public static readonly HardwareAccel Qsv = Create("Qsv", "qsv");

    /// <summary>VAAPI.</summary>
    public static readonly HardwareAccel Vaapi = Create("Vaapi", "vaapi");

    /// <summary>Apple VideoToolbox.</summary>
    public static readonly HardwareAccel VideoToolbox = Create("VideoToolbox", "videotoolbox");

    /// <summary>Every built-in device. Touching this list registers the static instances for lookup.</summary>
    public static IReadOnlyList<HardwareAccel> BuiltIns { get; } = [None, Nvenc, Qsv, Vaapi, VideoToolbox];

    /// <summary>Stable display name (e.g. <c>Nvenc</c>).</summary>
    public string Name { get; }

    /// <summary>Canonical id (e.g. <c>nvenc</c>).</summary>
    public string Id { get; }

    private HardwareAccel(string name, string id)
    {
        Name = name;
        Id = NamedCatalog.NormalizeId(id);
        NamedCatalog.Register(ByName, ById, this, Name, Id);
    }

    /// <summary>Looks up a device by <see cref="Id" />.</summary>
    public static HardwareAccel? TryFromId(string? id)
    {
        _ = BuiltIns;
        return NamedCatalog.TryFromId(ById, id);
    }

    /// <summary>Looks up a device by display <see cref="Name" />.</summary>
    public static HardwareAccel? TryFromName(string? name)
    {
        _ = BuiltIns;
        return NamedCatalog.TryFromName(ByName, name);
    }

    /// <summary>Resolves a config value by name or id. Returns null when <paramref name="text" /> is blank or unknown.</summary>
    public static HardwareAccel? TryParse(string? text) => TryFromName(text) ?? TryFromId(text);

    /// <summary>Returns a device for an arbitrary id. Reuses a registered instance when the id is already known.</summary>
    public static HardwareAccel Custom(string id)
    {
        _ = BuiltIns;
        return NamedCatalog.GetOrCustom(ById, id, key => new(key, key));
    }

    /// <inheritdoc />
    public override string ToString() => Name;

    private static HardwareAccel Create(string name, string id) => new(name, id);
}