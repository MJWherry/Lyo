using Lyo.Recipe.Models.Enums;
using Lyo.Web.Primitives;

namespace Lyo.Recipe.Web.Components;

/// <summary>MudBlazor colors for recipe status, difficulty, and attachment role.</summary>
public static class RecipeColorHelper
{
    /// <summary>Color for a recipe lifecycle status.</summary>
    public static Color ForStatus(RecipeStatus status)
        => status switch {
            RecipeStatus.Active => Color.Success,
            RecipeStatus.Draft => Color.Default,
            RecipeStatus.Archived => Color.Warning,
            var _ => Color.Default
        };

    /// <summary>Color for a status enum name or numeric string.</summary>
    public static Color ForStatus(string? text)
        => Enum.TryParse<RecipeStatus>(text, true, out var status) ? ForStatus(status) : Color.Default;

    /// <summary>Color for cook difficulty.</summary>
    public static Color ForDifficulty(RecipeDifficulty difficulty)
        => difficulty switch {
            RecipeDifficulty.Easy => Color.Success,
            RecipeDifficulty.Medium => Color.Warning,
            RecipeDifficulty.Hard => Color.Error,
            var _ => Color.Default
        };

    /// <summary>Color for a difficulty enum name or numeric string.</summary>
    public static Color ForDifficulty(string? text)
        => Enum.TryParse<RecipeDifficulty>(text, true, out var difficulty) ? ForDifficulty(difficulty) : Color.Default;

    /// <summary>Identity hue for an attachment role. Unknown roles stay uncolored.</summary>
    public static LyoChipHue? ForRole(RecipeAttachmentRole role)
        => role switch {
            RecipeAttachmentRole.Document => LyoChipHue.Indigo,
            RecipeAttachmentRole.Scan => LyoChipHue.Lime,
            RecipeAttachmentRole.Card => LyoChipHue.Amber,
            RecipeAttachmentRole.Image => LyoChipHue.Rose,
            var _ => null
        };
}
