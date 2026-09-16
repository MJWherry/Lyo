using Lyo.Exceptions;

namespace Lyo.Recipe.Models;

/// <summary>FileStorage <c>PathPrefix</c> conventions for recipe attachments.</summary>
public static class RecipeFilePaths
{
    /// <summary>Root prefix for all recipe files.</summary>
    public const string Root = "recipe";

    /// <summary>Folder for files attached to a recipe.</summary>
    public static string ForRecipe(Guid recipeId)
    {
        if (recipeId == Guid.Empty)
            throw new ArgumentException("Recipe id is required.", nameof(recipeId));

        return $"{Root}/recipes/{recipeId:D}";
    }

    /// <summary>Folder for files attached to an ingredient.</summary>
    public static string ForIngredient(Guid ingredientId)
    {
        if (ingredientId == Guid.Empty)
            throw new ArgumentException("Ingredient id is required.", nameof(ingredientId));

        return $"{Root}/ingredients/{ingredientId:D}";
    }

    /// <summary>Folder for files attached to one recipe step.</summary>
    public static string ForStep(Guid stepId)
    {
        if (stepId == Guid.Empty)
            throw new ArgumentException("Step id is required.", nameof(stepId));

        return $"{Root}/steps/{stepId:D}";
    }

    /// <summary>Folder for files attached to a category.</summary>
    public static string ForCategory(Guid categoryId)
    {
        if (categoryId == Guid.Empty)
            throw new ArgumentException("Category id is required.", nameof(categoryId));

        return $"{Root}/categories/{categoryId:D}";
    }

    /// <summary>Resolves the prefix for a Note/Tag subject type and id.</summary>
    public static string ForEntity(string entityType, Guid entityId)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(entityType);
        return entityType switch {
            RecipeEntityTypes.Recipe => ForRecipe(entityId),
            RecipeEntityTypes.RecipeStep => ForStep(entityId),
            RecipeEntityTypes.Ingredient => ForIngredient(entityId),
            RecipeEntityTypes.RecipeCategory => ForCategory(entityId),
            var _ => throw new ArgumentException($"Unsupported recipe entity type '{entityType}'.", nameof(entityType))
        };
    }
}
