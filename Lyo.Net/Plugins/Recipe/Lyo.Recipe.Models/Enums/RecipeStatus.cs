namespace Lyo.Recipe.Models.Enums;

/// <summary>Lifecycle of a household recipe.</summary>
public enum RecipeStatus
{
    /// <summary>Not ready to cook from.</summary>
    Draft = 0,

    /// <summary>In the working cookbook.</summary>
    Active = 1,

    /// <summary>Kept but hidden from the default list.</summary>
    Archived = 2
}