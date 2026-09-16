namespace Lyo.Recipe.Models.Response;

/// <summary>One ingredient line as returned by the API.</summary>
public sealed class RecipeIngredientLineRes
{
    /// <summary>Row id.</summary>
    public Guid Id { get; set; }

    /// <summary>Owning recipe.</summary>
    public Guid RecipeId { get; set; }

    /// <summary>Optional catalog ingredient.</summary>
    public Guid? IngredientId { get; set; }

    /// <summary>Free-text name when no catalog row is linked.</summary>
    public string? IngredientText { get; set; }

    /// <summary>Display order, lowest first.</summary>
    public int SortOrder { get; set; }

    /// <summary>Amount. Null for "to taste" lines.</summary>
    public decimal? Quantity { get; set; }

    /// <summary>Unit for <see cref="Quantity"/>.</summary>
    public string? Unit { get; set; }

    /// <summary>How the ingredient is prepared.</summary>
    public string? Preparation { get; set; }

    /// <summary>When true the line is optional.</summary>
    public bool IsOptional { get; set; }

    /// <summary>Section heading such as "For the sauce".</summary>
    public string? GroupLabel { get; set; }

    /// <summary>Steps on this recipe that use the line.</summary>
    public List<Guid> StepIds { get; set; } = [];
}
