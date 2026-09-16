namespace Lyo.Recipe.Models;

/// <summary>Shared constants for the Recipe HTTP surface.</summary>
public static class Constants
{
    /// <summary>REST API route names.</summary>
    public static class Rest
    {
        /// <summary>Recipe route prefixes used by <c>BuildRecipeGroup</c> and Blazor clients.</summary>
        public static class Recipe
        {
            /// <summary>OpenAPI tag and route root.</summary>
            public const string Route = "Recipe";

            /// <summary>Category CRUD.</summary>
            public const string Categories = $"{Route}/Categories";

            /// <summary>Ingredient catalog CRUD.</summary>
            public const string Ingredients = $"{Route}/Ingredients";

            /// <summary>Cooking-unit catalog.</summary>
            public const string Units = $"{Route}/Units";

            /// <summary>Recipe CRUD.</summary>
            public const string Recipes = $"{Route}/Recipes";

            /// <summary>Attachment link CRUD.</summary>
            public const string Attachments = $"{Route}/Attachments";

            /// <summary>Multipart upload for any file role.</summary>
            public const string AttachmentsUpload = $"{Attachments}/upload";

            /// <summary>Ingredient lines for one recipe.</summary>
            public static string IngredientLines(Guid recipeId) => $"{Recipes}/{recipeId:D}/IngredientLines";

            /// <summary>Steps for one recipe.</summary>
            public static string Steps(Guid recipeId) => $"{Recipes}/{recipeId:D}/Steps";

            /// <summary>One unit row.</summary>
            public static string Unit(Guid unitId) => $"{Units}/{unitId:D}";

            /// <summary>Recipes and steps that use one catalog ingredient.</summary>
            public static string IngredientUsage(Guid ingredientId) => $"{Ingredients}/{ingredientId:D}/Usage";
        }
    }
}
