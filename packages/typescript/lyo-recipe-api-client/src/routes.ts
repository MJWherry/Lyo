/** REST paths from `Lyo.Recipe.Models.Constants.Rest.Recipe` (no leading slash). */
export const RecipeRoutes = {
    route: "Recipe",
    categories: "Recipe/Categories",
    ingredients: "Recipe/Ingredients",
    recipes: "Recipe/Recipes",
    attachments: "Recipe/Attachments",
    attachmentsUpload: "Recipe/Attachments/upload",
} as const;

export function recipeIngredientLinesPath(recipeId: string): string {
    return `${RecipeRoutes.recipes}/${recipeId}/IngredientLines`;
}

export function recipeStepsPath(recipeId: string): string {
    return `${RecipeRoutes.recipes}/${recipeId}/Steps`;
}

export function recipeNotesPath(parentRoute: string, id: string): string {
    return `${parentRoute.replace(/\/+$/, "")}/${id}/Notes`;
}

export function recipeTagsPath(parentRoute: string, id: string): string {
    return `${parentRoute.replace(/\/+$/, "")}/${id}/Tags`;
}

export function joinRoutePrefix(routePrefix: string | undefined, relativePath: string): string {
    const relative = relativePath.replace(/^\/+/, "");
    const prefix = (routePrefix ?? "").replace(/\/+$/, "").replace(/^\/+/, "");
    return prefix ? `/${prefix}/${relative}` : `/${relative}`;
}
