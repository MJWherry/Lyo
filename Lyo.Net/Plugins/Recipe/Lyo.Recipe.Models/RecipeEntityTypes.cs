namespace Lyo.Recipe.Models;

/// <summary>Stable <c>EntityRef.EntityType</c> values for Note/Tag composition. Do not persist CLR type names.</summary>
public static class RecipeEntityTypes
{
    /// <summary>A household recipe.</summary>
    public const string Recipe = "Recipe";

    /// <summary>One instruction step on a recipe. Files attached here are step photos or reference images.</summary>
    public const string RecipeStep = "RecipeStep";

    /// <summary>Reusable ingredient catalog row.</summary>
    public const string Ingredient = "Ingredient";

    /// <summary>Hierarchical recipe grouping.</summary>
    public const string RecipeCategory = "RecipeCategory";

    /// <summary>FileStorage attachment link row.</summary>
    public const string RecipeAttachment = "RecipeAttachment";
}