using Lyo.Common.Core.Identifiers;

namespace Lyo.Recipe.FileStorage.Tests;

internal sealed class MemoryRecipeStore : IRecipeStore
{
    public List<RecipeAttachmentRecord> Attachments { get; } = [];

    public Task SaveAttachmentAsync(RecipeAttachmentRecord attachment, Guid? tenantId, CancellationToken ct = default)
    {
        if (attachment.Id == Guid.Empty)
            attachment.Id = LyoGuid.CreateV7();

        if (attachment.CreatedTimestamp == default)
            attachment.CreatedTimestamp = DateTime.UtcNow;

        var existing = Attachments.FindIndex(a => a.Id == attachment.Id);
        if (existing >= 0)
            Attachments[existing] = attachment;
        else
            Attachments.Add(attachment);

        return Task.CompletedTask;
    }

    public Task<RecipeAttachmentRecord?> GetAttachmentByIdAsync(Guid id, Guid? tenantId, CancellationToken ct = default)
        => Task.FromResult(Attachments.FirstOrDefault(a => a.Id == id));

    public Task<IReadOnlyList<RecipeAttachmentRecord>> ListAttachmentsAsync(string entityType, Guid entityId, Guid? tenantId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<RecipeAttachmentRecord>>(Attachments.Where(a => a.EntityType == entityType && a.EntityId == entityId).ToList());

    public Task DeleteAttachmentAsync(Guid id, Guid? tenantId, CancellationToken ct = default)
    {
        Attachments.RemoveAll(a => a.Id == id);
        return Task.CompletedTask;
    }

    public Task SaveCategoryAsync(RecipeCategoryRecord category, Guid? tenantId, CancellationToken ct = default) => throw new NotSupportedException();

    public Task<RecipeCategoryRecord?> GetCategoryByIdAsync(Guid id, Guid? tenantId, CancellationToken ct = default) => throw new NotSupportedException();

    public Task<IReadOnlyList<RecipeCategoryRecord>> ListCategoriesAsync(Guid? tenantId, CancellationToken ct = default) => throw new NotSupportedException();

    public Task DeleteCategoryAsync(Guid id, Guid? tenantId, CancellationToken ct = default) => throw new NotSupportedException();

    public Task SaveIngredientAsync(IngredientRecord ingredient, Guid? tenantId, CancellationToken ct = default) => throw new NotSupportedException();

    public Task<IngredientRecord?> GetIngredientByIdAsync(Guid id, Guid? tenantId, CancellationToken ct = default) => throw new NotSupportedException();

    public Task<IngredientRecord?> GetIngredientBySlugAsync(string slug, Guid? tenantId, CancellationToken ct = default) => throw new NotSupportedException();

    public Task<IReadOnlyList<IngredientRecord>> ListIngredientsAsync(Guid? tenantId, CancellationToken ct = default) => throw new NotSupportedException();

    public Task DeleteIngredientAsync(Guid id, Guid? tenantId, CancellationToken ct = default) => throw new NotSupportedException();

    public Task SaveRecipeAsync(RecipeRecord recipe, Guid? tenantId, CancellationToken ct = default) => throw new NotSupportedException();

    public Task<RecipeRecord?> GetRecipeByIdAsync(Guid id, Guid? tenantId, CancellationToken ct = default) => throw new NotSupportedException();

    public Task<RecipeRecord?> GetRecipeBySlugAsync(string slug, Guid? tenantId, CancellationToken ct = default) => throw new NotSupportedException();

    public Task DeleteRecipeAsync(Guid id, Guid? tenantId, CancellationToken ct = default) => throw new NotSupportedException();

    public Task<IReadOnlyList<RecipeIngredientLineRecord>> GetIngredientLinesAsync(Guid recipeId, Guid? tenantId, CancellationToken ct = default)
        => throw new NotSupportedException();

    public Task ReplaceIngredientLinesAsync(Guid recipeId, IReadOnlyList<RecipeIngredientLineRecord> lines, Guid? tenantId, CancellationToken ct = default)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<RecipeStepRecord>> GetStepsAsync(Guid recipeId, Guid? tenantId, CancellationToken ct = default) => throw new NotSupportedException();

    public Task ReplaceStepsAsync(Guid recipeId, IReadOnlyList<RecipeStepRecord> steps, Guid? tenantId, CancellationToken ct = default) => throw new NotSupportedException();

    public Task<IReadOnlyList<IngredientUsageRecord>> ListIngredientUsageAsync(Guid ingredientId, Guid? tenantId, CancellationToken ct = default)
        => throw new NotSupportedException();

    public Task SaveUnitAsync(UnitRecord unit, Guid? tenantId, CancellationToken ct = default) => throw new NotSupportedException();

    public Task<IReadOnlyList<UnitRecord>> ListUnitsAsync(Guid? tenantId, CancellationToken ct = default) => throw new NotSupportedException();

    public Task DeleteUnitAsync(Guid id, Guid? tenantId, CancellationToken ct = default) => throw new NotSupportedException();
}