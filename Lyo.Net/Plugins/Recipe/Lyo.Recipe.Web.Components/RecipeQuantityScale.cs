namespace Lyo.Recipe.Web.Components;

/// <summary>Scales a line quantity from the recipe's servings to the number of people being cooked for.</summary>
public static class RecipeQuantityScale
{
    /// <summary>
    /// Returns <paramref name="quantity" /> adjusted by <paramref name="cookingFor" /> / <paramref name="recipeServings" />. A null quantity stays null. Missing or non-positive
    /// servings leave the quantity unchanged.
    /// </summary>
    public static decimal? Scale(decimal? quantity, int? recipeServings, int? cookingFor)
    {
        if (quantity is null)
            return null;

        if (recipeServings is null or <= 0 || cookingFor is null or <= 0 || recipeServings == cookingFor)
            return quantity;

        return quantity.Value * cookingFor.Value / recipeServings.Value;
    }
}