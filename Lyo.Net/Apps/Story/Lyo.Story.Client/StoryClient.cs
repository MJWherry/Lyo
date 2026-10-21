using Lyo.Api.Client;
using Lyo.Exceptions;
using Lyo.Story.Models;
using Lyo.Story.Models.Request;
using Lyo.Story.Models.Response;

namespace Lyo.Story.Client;

/// <summary>Typed facade over the Lyo Story graph and play routes. Sit it on any <see cref="IApiClient" /> implementation.</summary>
public sealed class StoryClient : IStoryClient
{
    private readonly IApiClient _api;
    private readonly string? _routePrefix;

    /// <summary>Constructs the client. Null <paramref name="options" /> keeps routes relative to the inner client's base address.</summary>
    public StoryClient(IApiClient apiClient, StoryClientOptions? options = null)
    {
        ArgumentHelpers.ThrowIfNull(apiClient);
        _api = apiClient;
        _routePrefix = options?.RoutePrefix;
    }

    /// <inheritdoc />
    public Task<StoryGraphRes> SaveGraphAsync(StoryGraphReq request, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(request);
        return _api.PostAsAsync<StoryGraphReq, StoryGraphRes>(Route(Constants.Rest.Story.Graph), request, ct: ct);
    }

    /// <inheritdoc />
    public Task<StoryGraphRes?> GetGraphAsync(Guid id, CancellationToken ct = default) => _api.GetAsAsync<StoryGraphRes>(Route(Constants.Rest.Story.GraphById(id)), ct: ct);

    /// <inheritdoc />
    public Task<StoryGraphRes?> GetGraphBySlugAsync(string slug, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(slug);
        return _api.GetAsAsync<StoryGraphRes>(Route(Constants.Rest.Story.GraphBySlug(slug)), ct: ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StoryGraphRes>> ListGraphsAsync(StoryListReq request, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(request);
        return await _api.PostAsAsync<StoryListReq, StoryGraphRes[]>(Route(Constants.Rest.Story.GraphList), request, ct: ct).ConfigureAwait(false) ?? [];
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        => await _api.DeleteAsAsync<object>(Route($"{Constants.Rest.Story.Stories}/{id:D}"), ct: ct).ConfigureAwait(false);

    /// <inheritdoc />
    public Task<StoryPlayStateRes> StartAsync(StoryStartPlaythroughReq request, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(request);
        return _api.PostAsAsync<StoryStartPlaythroughReq, StoryPlayStateRes>(Route(Constants.Rest.Story.PlayStart), request, ct: ct);
    }

    /// <inheritdoc />
    public Task<StoryPlayStateRes?> GetPlaythroughAsync(Guid playthroughId, CancellationToken ct = default)
        => _api.GetAsAsync<StoryPlayStateRes>(Route(Constants.Rest.Story.Play(playthroughId)), ct: ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<StoryMoveRes>> GetMovesAsync(Guid playthroughId, CancellationToken ct = default)
    {
        var rows = await _api.GetAsAsync<List<StoryMoveRes>>(Route(Constants.Rest.Story.Moves(playthroughId)), ct: ct).ConfigureAwait(false);
        return rows ?? [];
    }

    /// <inheritdoc />
    public Task<StoryPlayStateRes> ChooseAsync(Guid playthroughId, StoryChooseReq request, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(request);
        return _api.PostAsAsync<StoryChooseReq, StoryPlayStateRes>(Route(Constants.Rest.Story.Choose(playthroughId)), request, ct: ct);
    }

    /// <inheritdoc />
    public Task<StoryPlayStateRes> AdvanceAsync(Guid playthroughId, CancellationToken ct = default)
        => _api.PostAsAsync<StoryPlayStateRes>(Route(Constants.Rest.Story.Advance(playthroughId)), ct: ct);

    /// <inheritdoc />
    public Task<StoryPlayStateRes> SetVariablesAsync(Guid playthroughId, StorySetVariablesReq request, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(request);
        return _api.PostAsAsync<StorySetVariablesReq, StoryPlayStateRes>(Route(Constants.Rest.Story.SetVariables(playthroughId)), request, ct: ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StoryPlaythroughEventRes>> GetEventsAsync(Guid playthroughId, CancellationToken ct = default)
    {
        var rows = await _api.GetAsAsync<List<StoryPlaythroughEventRes>>(Route(Constants.Rest.Story.Events(playthroughId)), ct: ct).ConfigureAwait(false);
        return rows ?? [];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StoryPlaythroughEventRes>> GetCausesAsync(Guid playthroughId, string variableKey, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(variableKey);
        var rows = await _api.GetAsAsync<List<StoryPlaythroughEventRes>>(Route(Constants.Rest.Story.Causes(playthroughId, variableKey)), ct: ct).ConfigureAwait(false);
        return rows ?? [];
    }

    private string Route(string relativePath) => ApiRouteBuilder.Build(_routePrefix, relativePath);
}