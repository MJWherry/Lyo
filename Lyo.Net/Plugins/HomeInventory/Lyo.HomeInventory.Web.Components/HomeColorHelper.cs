using Lyo.HomeInventory.Models.Enums;
using Lyo.Web.Primitives;

namespace Lyo.HomeInventory.Web.Components;

/// <summary>MudBlazor colors for inventory status, condition, and attachment role.</summary>
public static class HomeColorHelper
{
    /// <summary>Color for an item lifecycle status.</summary>
    public static Color ForStatus(HomeItemStatus status)
        => status switch {
            HomeItemStatus.Active => Color.Success,
            HomeItemStatus.Discontinued => Color.Warning,
            HomeItemStatus.Archived => Color.Default,
            var _ => Color.Default
        };

    /// <summary>Color for a status enum name or numeric string.</summary>
    public static Color ForStatus(string? text) => Enum.TryParse<HomeItemStatus>(text, true, out var status) ? ForStatus(status) : Color.Default;

    /// <summary>Color for physical/commercial condition.</summary>
    public static Color ForCondition(HomeItemCondition condition)
        => condition switch {
            HomeItemCondition.New => Color.Success,
            HomeItemCondition.Refurbished => Color.Info,
            HomeItemCondition.OpenBox => Color.Info,
            HomeItemCondition.Used => Color.Default,
            HomeItemCondition.Damaged => Color.Error,
            HomeItemCondition.Unknown => Color.Default,
            var _ => Color.Default
        };

    /// <summary>Color for a condition enum name or numeric string.</summary>
    public static Color ForCondition(string? text) => Enum.TryParse<HomeItemCondition>(text, true, out var condition) ? ForCondition(condition) : Color.Default;

    /// <summary>Identity hue for an attachment role. Unknown roles stay uncolored.</summary>
    public static LyoChipHue? ForRole(HomeAttachmentRole role)
        => role switch {
            HomeAttachmentRole.Photo => LyoChipHue.Rose,
            HomeAttachmentRole.Receipt => LyoChipHue.Lime,
            HomeAttachmentRole.Manual => LyoChipHue.Blue,
            HomeAttachmentRole.Warranty => LyoChipHue.Amber,
            HomeAttachmentRole.Document => LyoChipHue.Indigo,
            var _ => null
        };
}