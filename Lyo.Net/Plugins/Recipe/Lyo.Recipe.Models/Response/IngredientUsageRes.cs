namespace Lyo.Recipe.Models.Response;

/// <summary>One recipe line that uses a catalog ingredient.</summary>
public sealed class IngredientUsageRes
{
    /// <summary>Recipe that owns the line.</summary>
    public Guid RecipeId { get; set; }

    /// <summary>Recipe title.</summary>
    public string RecipeTitle { get; set; } = string.Empty;

    /// <summary>Ingredient line id.</summary>
    public Guid LineId { get; set; }

    /// <summary>Linked step ids, ordered by step sort order.</summary>
    public List<Guid> StepIds { get; set; } = [];

    /// <summary>1-based step numbers for <see cref="StepIds"/>, same order.</summary>
    public List<int> StepNumbers { get; set; } = [];
}
