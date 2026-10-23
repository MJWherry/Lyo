namespace Lyo.Recipe;

/// <summary>One recipe line that references a catalog ingredient, and the steps that use it.</summary>
public sealed class IngredientUsageRecord
{
    /// <summary>Recipe that owns the line.</summary>
    public Guid RecipeId { get; set; }

    /// <summary>Recipe title at the time of the query.</summary>
    public string RecipeTitle { get; set; } = string.Empty;

    /// <summary>Ingredient line id.</summary>
    public Guid LineId { get; set; }

    /// <summary>Steps on that recipe linked to the line, ordered by step sort order.</summary>
    public List<Guid> StepIds { get; set; } = [];

    /// <summary>1-based step numbers for <see cref="StepIds" />, same order.</summary>
    public List<int> StepNumbers { get; set; } = [];
}