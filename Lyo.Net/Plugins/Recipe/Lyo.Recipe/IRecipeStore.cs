namespace Lyo.Recipe;

/// <summary>
/// Stores household recipes, ingredient catalog rows, categories, ordered lines and steps, and FileStorage attachment links. Listing and search belong in the query layer.
/// Free-text comments and labels go through <c>INoteStore</c> / <c>ITagStore</c>.
/// </summary>
public interface IRecipeStore
{
#region Categories

    /// <summary>Upserts a category. Generates <see cref="RecipeCategoryRecord.Id" /> when empty.</summary>
    Task SaveCategoryAsync(RecipeCategoryRecord category, Guid? tenantId, CancellationToken ct = default);

    /// <summary>One category by id.</summary>
    Task<RecipeCategoryRecord?> GetCategoryByIdAsync(Guid id, Guid? tenantId, CancellationToken ct = default);

    /// <summary>All categories for the tenant.</summary>
    Task<IReadOnlyList<RecipeCategoryRecord>> ListCategoriesAsync(Guid? tenantId, CancellationToken ct = default);

    /// <summary>Deletes a category when it has no children or recipes.</summary>
    Task DeleteCategoryAsync(Guid id, Guid? tenantId, CancellationToken ct = default);

#endregion

#region Ingredients

    /// <summary>Upserts an ingredient. Generates <see cref="IngredientRecord.Id" /> when empty.</summary>
    Task SaveIngredientAsync(IngredientRecord ingredient, Guid? tenantId, CancellationToken ct = default);

    /// <summary>One ingredient by id.</summary>
    Task<IngredientRecord?> GetIngredientByIdAsync(Guid id, Guid? tenantId, CancellationToken ct = default);

    /// <summary>One ingredient by slug.</summary>
    Task<IngredientRecord?> GetIngredientBySlugAsync(string slug, Guid? tenantId, CancellationToken ct = default);

    /// <summary>All ingredients for the tenant, name order.</summary>
    Task<IReadOnlyList<IngredientRecord>> ListIngredientsAsync(Guid? tenantId, CancellationToken ct = default);

    /// <summary>Deletes an ingredient when no line references it.</summary>
    Task DeleteIngredientAsync(Guid id, Guid? tenantId, CancellationToken ct = default);

    /// <summary>Recipes and steps that reference this catalog ingredient. One row per line.</summary>
    Task<IReadOnlyList<IngredientUsageRecord>> ListIngredientUsageAsync(Guid ingredientId, Guid? tenantId, CancellationToken ct = default);

#endregion

#region Units

    /// <summary>Upserts a unit. Generates <see cref="UnitRecord.Id" /> when empty and writes it back.</summary>
    Task SaveUnitAsync(UnitRecord unit, Guid? tenantId, CancellationToken ct = default);

    /// <summary>Units for the tenant, sort order then abbreviation.</summary>
    Task<IReadOnlyList<UnitRecord>> ListUnitsAsync(Guid? tenantId, CancellationToken ct = default);

    /// <summary>Deletes a unit. Lines that stored the abbreviation as text are left unchanged.</summary>
    Task DeleteUnitAsync(Guid id, Guid? tenantId, CancellationToken ct = default);

#endregion

#region Recipes

    /// <summary>Upserts a recipe. Generates <see cref="RecipeRecord.Id" /> when empty.</summary>
    Task SaveRecipeAsync(RecipeRecord recipe, Guid? tenantId, CancellationToken ct = default);

    /// <summary>One recipe by id.</summary>
    Task<RecipeRecord?> GetRecipeByIdAsync(Guid id, Guid? tenantId, CancellationToken ct = default);

    /// <summary>One recipe by slug.</summary>
    Task<RecipeRecord?> GetRecipeBySlugAsync(string slug, Guid? tenantId, CancellationToken ct = default);

    /// <summary>Hard-deletes a recipe (lines, steps, and attachment rows cascade). Stored files stay.</summary>
    Task DeleteRecipeAsync(Guid id, Guid? tenantId, CancellationToken ct = default);

#endregion

#region Lines and steps

    /// <summary>Ingredient lines for a recipe, sort order then id.</summary>
    Task<IReadOnlyList<RecipeIngredientLineRecord>> GetIngredientLinesAsync(Guid recipeId, Guid? tenantId, CancellationToken ct = default);

    /// <summary>
    /// Replaces every line on a recipe in one transaction. A non-empty <see cref="RecipeIngredientLineRecord.Id" /> updates that row. Each line needs
    /// <see cref="RecipeIngredientLineRecord.IngredientId" /> or <see cref="RecipeIngredientLineRecord.IngredientText" />. <see cref="RecipeIngredientLineRecord.StepIds" /> must belong
    /// to this recipe.
    /// </summary>
    Task ReplaceIngredientLinesAsync(Guid recipeId, IReadOnlyList<RecipeIngredientLineRecord> lines, Guid? tenantId, CancellationToken ct = default);

    /// <summary>Steps for a recipe, sort order then id.</summary>
    Task<IReadOnlyList<RecipeStepRecord>> GetStepsAsync(Guid recipeId, Guid? tenantId, CancellationToken ct = default);

    /// <summary>
    /// Replaces every step on a recipe in one transaction. A non-empty <see cref="RecipeStepRecord.Id" /> updates that row. Omitted steps are deleted, along with their
    /// attachment rows and ingredient-line links.
    /// </summary>
    Task ReplaceStepsAsync(Guid recipeId, IReadOnlyList<RecipeStepRecord> steps, Guid? tenantId, CancellationToken ct = default);

#endregion

#region Attachments

    /// <summary>Upserts an attachment link. Generates <see cref="RecipeAttachmentRecord.Id" /> when empty.</summary>
    Task SaveAttachmentAsync(RecipeAttachmentRecord attachment, Guid? tenantId, CancellationToken ct = default);

    /// <summary>One attachment by id.</summary>
    Task<RecipeAttachmentRecord?> GetAttachmentByIdAsync(Guid id, Guid? tenantId, CancellationToken ct = default);

    /// <summary>Attachments for a subject, ordered by <see cref="RecipeAttachmentRecord.SortOrder" /> then created.</summary>
    Task<IReadOnlyList<RecipeAttachmentRecord>> ListAttachmentsAsync(string entityType, Guid entityId, Guid? tenantId, CancellationToken ct = default);

    /// <summary>Removes an attachment link. FileStorage bytes stay unless a FileStorage helper deletes them.</summary>
    Task DeleteAttachmentAsync(Guid id, Guid? tenantId, CancellationToken ct = default);

#endregion
}