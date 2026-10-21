using Lyo.Api.Client;
using Lyo.Exceptions;
using Lyo.Note;
using Lyo.Recipe.Models;
using Lyo.Recipe.Models.Enums;
using Lyo.Recipe.Models.Request;
using Lyo.Recipe.Models.Response;
using Lyo.Tag;

namespace Lyo.Recipe.Client;

/// <summary>Typed facade over extra Recipe routes. Sit it on any <see cref="IApiClient" /> implementation.</summary>
public sealed class RecipeClient : IRecipeClient
{
    private readonly IApiClient _api;
    private readonly string? _routePrefix;

    /// <summary>Constructs the client. Null <paramref name="options" /> keeps routes relative to the inner client's base address.</summary>
    public RecipeClient(IApiClient apiClient, RecipeClientOptions? options = null)
    {
        ArgumentHelpers.ThrowIfNull(apiClient);
        _api = apiClient;
        _routePrefix = options?.RoutePrefix;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RecipeIngredientLineRes>> GetIngredientLinesAsync(Guid recipeId, CancellationToken ct = default)
        => await _api.GetAsAsync<List<RecipeIngredientLineRes>>(Route(Constants.Rest.Recipe.IngredientLines(recipeId)), ct: ct).ConfigureAwait(false) ?? [];

    /// <inheritdoc />
    public async Task<IReadOnlyList<RecipeIngredientLineRes>> ReplaceIngredientLinesAsync(
        Guid recipeId,
        IReadOnlyList<RecipeIngredientLineReq> lines,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(lines);
        return await _api.PutAsAsync<IReadOnlyList<RecipeIngredientLineReq>, List<RecipeIngredientLineRes>>(Route(Constants.Rest.Recipe.IngredientLines(recipeId)), lines, ct: ct)
            .ConfigureAwait(false) ?? [];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RecipeStepRes>> GetStepsAsync(Guid recipeId, CancellationToken ct = default)
        => await _api.GetAsAsync<List<RecipeStepRes>>(Route(Constants.Rest.Recipe.Steps(recipeId)), ct: ct).ConfigureAwait(false) ?? [];

    /// <inheritdoc />
    public async Task<IReadOnlyList<RecipeStepRes>> ReplaceStepsAsync(Guid recipeId, IReadOnlyList<RecipeStepReq> steps, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(steps);
        return await _api.PutAsAsync<IReadOnlyList<RecipeStepReq>, List<RecipeStepRes>>(Route(Constants.Rest.Recipe.Steps(recipeId)), steps, ct: ct).ConfigureAwait(false) ?? [];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UnitRes>> ListUnitsAsync(CancellationToken ct = default)
        => await _api.GetAsAsync<List<UnitRes>>(Route(Constants.Rest.Recipe.Units), ct: ct).ConfigureAwait(false) ?? [];

    /// <inheritdoc />
    public Task<UnitRes> SaveUnitAsync(UnitReq request, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(request);
        return _api.PutAsAsync<UnitReq, UnitRes>(Route(Constants.Rest.Recipe.Units), request, ct: ct);
    }

    /// <inheritdoc />
    public async Task DeleteUnitAsync(Guid unitId, CancellationToken ct = default)
        => await _api.DeleteAsAsync<object>(Route(Constants.Rest.Recipe.Unit(unitId)), ct: ct).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<IngredientUsageRes>> ListIngredientUsageAsync(Guid ingredientId, CancellationToken ct = default)
        => await _api.GetAsAsync<List<IngredientUsageRes>>(Route(Constants.Rest.Recipe.IngredientUsage(ingredientId)), ct: ct).ConfigureAwait(false) ?? [];

    /// <inheritdoc />
    public async Task<IReadOnlyList<NoteRecord>> GetNotesAsync(string parentRoute, Guid id, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(parentRoute);
        return await _api.GetAsAsync<List<NoteRecord>>(NotesRoute(parentRoute, id), ct: ct).ConfigureAwait(false) ?? [];
    }

    /// <inheritdoc />
    public Task<NoteRecord> AddNoteAsync(string parentRoute, Guid id, RecipeNoteCreateReq request, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(parentRoute);
        ArgumentHelpers.ThrowIfNull(request);
        return _api.PostAsAsync<RecipeNoteCreateReq, NoteRecord>(NotesRoute(parentRoute, id), request, ct: ct);
    }

    /// <inheritdoc />
    public async Task DeleteNoteAsync(string parentRoute, Guid id, Guid noteId, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(parentRoute);
        await _api.DeleteAsAsync<object>($"{NotesRoute(parentRoute, id)}/{noteId:D}", ct: ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TagRecord>> GetTagsAsync(string parentRoute, Guid id, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(parentRoute);
        return await _api.GetAsAsync<List<TagRecord>>(TagsRoute(parentRoute, id), ct: ct).ConfigureAwait(false) ?? [];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TagRecord>> AddTagAsync(string parentRoute, Guid id, RecipeTagAddReq request, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(parentRoute);
        ArgumentHelpers.ThrowIfNull(request);
        return await _api.PostAsAsync<RecipeTagAddReq, List<TagRecord>>(TagsRoute(parentRoute, id), request, ct: ct).ConfigureAwait(false) ?? [];
    }

    /// <inheritdoc />
    public async Task RemoveTagAsync(string parentRoute, Guid id, string name, string? tagType = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(parentRoute);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name);
        var uri = $"{TagsRoute(parentRoute, id)}/{Uri.EscapeDataString(name)}";
        if (!string.IsNullOrWhiteSpace(tagType))
            uri += $"?tagType={Uri.EscapeDataString(tagType)}";

        await _api.DeleteAsAsync<object>(uri, ct: ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<RecipeAttachmentRes> UploadAttachmentAsync(
        string entityType,
        Guid entityId,
        Stream content,
        string? fileName,
        RecipeAttachmentRole role,
        string? caption = null,
        bool isPrimary = false,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(entityType);
        ArgumentHelpers.ThrowIfNull(content);
        var name = string.IsNullOrWhiteSpace(fileName) ? "file.bin" : fileName;
        return _api.PostFileAsAsync<RecipeAttachmentRes>(
            Route(Constants.Rest.Recipe.AttachmentsUpload), content, name, req => {
                if (req.Content is not MultipartFormDataContent form)
                    return;

                form.Add(new StringContent(entityType), "entityType");
                form.Add(new StringContent(entityId.ToString("D")), "entityId");
                form.Add(new StringContent(role.ToString()), "role");
                if (caption != null)
                    form.Add(new StringContent(caption), "caption");

                form.Add(new StringContent(isPrimary ? "true" : "false"), "isPrimary");
            }, ct);
    }

    private string NotesRoute(string parentRoute, Guid id) => Route($"{parentRoute.TrimEnd('/')}/{id:D}/Notes");

    private string TagsRoute(string parentRoute, Guid id) => Route($"{parentRoute.TrimEnd('/')}/{id:D}/Tags");

    private string Route(string relativePath) => ApiRouteBuilder.Build(_routePrefix, relativePath);
}