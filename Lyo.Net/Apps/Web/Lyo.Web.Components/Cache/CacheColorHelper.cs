using Lyo.Api.Models.Enums;

namespace Lyo.Web.Components.Cache;

/// <summary>MudBlazor color and icon for cache item Type (Key versus Tag).</summary>
public static class CacheColorHelper
{
    /// <summary>Identity hue for a cache row type. Unknown types stay uncolored.</summary>
    public static LyoChipHue? ForType(CacheItemTypeEnum type)
        => type switch {
            CacheItemTypeEnum.Key => LyoChipHue.Blue,
            CacheItemTypeEnum.Tag => LyoChipHue.Amber,
            var _ => null
        };

    /// <summary>Identity hue for a projected type name. Unknown names return null.</summary>
    public static LyoChipHue? ForType(string? text) => Enum.TryParse<CacheItemTypeEnum>(text, true, out var type) ? ForType(type) : null;

    /// <summary>Material icon for a cache row type.</summary>
    public static string TypeIcon(CacheItemTypeEnum type)
        => type switch {
            CacheItemTypeEnum.Key => Icons.Material.Filled.VpnKey,
            CacheItemTypeEnum.Tag => Icons.Material.Filled.LocalOffer,
            var _ => Icons.Material.Filled.Help
        };

    /// <summary>Lock icon when the payload is encrypted; open lock otherwise.</summary>
    public static string EncryptedIcon(bool? encrypted) => encrypted == true ? Icons.Material.Filled.Lock : Icons.Material.Filled.LockOpen;

    /// <summary>Compress icon when the payload is compressed.</summary>
    public static string CompressedIcon(bool? compressed) => compressed == true ? Icons.Material.Filled.Compress : Icons.Material.Filled.DataObject;
}