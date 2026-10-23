using Lyo.Note;
using Lyo.Recipe.Models.Enums;
using Lyo.Recipe.Models.Request;
using Lyo.Recipe.Models.Response;
using Lyo.Tag;

namespace Lyo.Recipe.Client;

/// <summary>Typed HTTP client for extra Recipe routes. Category, ingredient, recipe, and attachment CRUD stay on <see cref="Lyo.Api.Client.IApiClient" />.</summary>
public interface IRecipeClient
{
    /// <summary>Ingredient lines for one recipe.</summary>
    Task<IReadOnlyList<RecipeIngredientLineRes>> GetIngredientLinesAsync(Guid recipeId, CancellationToken ct = default);

    /// <summary>Replaces every ingredient line on a recipe.</summary>
    Task<IReadOnlyList<RecipeIngredientLineRes>> ReplaceIngredientLinesAsync(Guid recipeId, IReadOnlyList<RecipeIngredientLineReq> lines, CancellationToken ct = default);

    /// <summary>Steps for one recipe.</summary>
    Task<IReadOnlyList<RecipeStepRes>> GetStepsAsync(Guid recipeId, CancellationToken ct = default);

    /// <summary>Replaces every step on a recipe.</summary>
    Task<IReadOnlyList<RecipeStepRes>> ReplaceStepsAsync(Guid recipeId, IReadOnlyList<RecipeStepReq> steps, CancellationToken ct = default);

    /// <summary>Cooking units, sort order then abbreviation.</summary>
    Task<IReadOnlyList<UnitRes>> ListUnitsAsync(CancellationToken ct = default);

    /// <summary>Creates or updates a unit. An empty id inserts a row.</summary>
    Task<UnitRes> SaveUnitAsync(UnitReq request, CancellationToken ct = default);

    /// <summary>Deletes a unit. Lines that stored the abbreviation as text stay as they are.</summary>
    Task DeleteUnitAsync(Guid unitId, CancellationToken ct = default);

    /// <summary>Recipes and steps that use one catalog ingredient.</summary>
    Task<IReadOnlyList<IngredientUsageRes>> ListIngredientUsageAsync(Guid ingredientId, CancellationToken ct = default);

    /// <summary>Notes on a category, ingredient, or recipe. <paramref name="parentRoute" /> is <c>Recipe/Categories</c>, <c>Recipe/Ingredients</c>, or <c>Recipe/Recipes</c>.</summary>
    Task<IReadOnlyList<NoteRecord>> GetNotesAsync(string parentRoute, Guid id, CancellationToken ct = default);

    /// <summary>Adds a note on a category, ingredient, or recipe.</summary>
    Task<NoteRecord> AddNoteAsync(string parentRoute, Guid id, RecipeNoteCreateReq request, CancellationToken ct = default);

    /// <summary>Deletes one note.</summary>
    Task DeleteNoteAsync(string parentRoute, Guid id, Guid noteId, CancellationToken ct = default);

    /// <summary>Tags on a category, ingredient, or recipe.</summary>
    Task<IReadOnlyList<TagRecord>> GetTagsAsync(string parentRoute, Guid id, CancellationToken ct = default);

    /// <summary>Adds a tag and returns the subject's tags.</summary>
    Task<IReadOnlyList<TagRecord>> AddTagAsync(string parentRoute, Guid id, RecipeTagAddReq request, CancellationToken ct = default);

    /// <summary>Removes a tag by name.</summary>
    Task RemoveTagAsync(string parentRoute, Guid id, string name, string? tagType = null, CancellationToken ct = default);

    /// <summary>Multipart upload to <c>Recipe/Attachments/upload</c>. Any content type is accepted.</summary>
    Task<RecipeAttachmentRes> UploadAttachmentAsync(
        string entityType,
        Guid entityId,
        Stream content,
        string? fileName,
        RecipeAttachmentRole role,
        string? caption = null,
        bool isPrimary = false,
        CancellationToken ct = default);
}