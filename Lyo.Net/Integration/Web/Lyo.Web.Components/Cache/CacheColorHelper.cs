using Lyo.Api.Models.Enums;

namespace Lyo.Web.Components.Cache;

/// <summary>MudBlazor color and icon for cache item Type (Key vs Tag).</summary>
public static class CacheColorHelper
{
    /// <summary>Chip color for a cache row type.</summary>
    public static Color ForType(CacheItemTypeEnum type)
        => type switch {
            CacheItemTypeEnum.Key => Color.Primary,
            CacheItemTypeEnum.Tag => Color.Tertiary,
            var _ => Color.Default
        };

    /// <summary>Chip color for a projected type name; unknown values use <see cref="Color.Default" />.</summary>
    public static Color ForType(string? text)
        => Enum.TryParse<CacheItemTypeEnum>(text, ignoreCase: true, out var type) ? ForType(type) : Color.Default;

    /// <summary>Material icon for a cache row type.</summary>
    public static string TypeIcon(CacheItemTypeEnum type)
        => type switch {
            CacheItemTypeEnum.Key => Icons.Material.Filled.VpnKey,
            CacheItemTypeEnum.Tag => Icons.Material.Filled.LocalOffer,
            var _ => Icons.Material.Filled.Help
        };
}
