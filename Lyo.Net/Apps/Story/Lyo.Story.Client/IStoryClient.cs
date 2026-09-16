using Lyo.Story.Models.Request;
using Lyo.Story.Models.Response;

namespace Lyo.Story.Client;

/// <summary>Typed HTTP client for story graph save/load/delete and play. Row list and filter use <see cref="Lyo.Api.Client.IApiClient.QueryConcreteAsync{TResult}" /> on <c>Story/Stories</c>.</summary>
public interface IStoryClient
{
    /// <summary>Upserts a play graph.</summary>
    Task<StoryGraphRes> SaveGraphAsync(StoryGraphReq request, CancellationToken ct = default);

    /// <summary>Loads a play graph by id, or null.</summary>
    Task<StoryGraphRes?> GetGraphAsync(Guid id, CancellationToken ct = default);

    /// <summary>Loads a play graph by slug, or null.</summary>
    Task<StoryGraphRes?> GetGraphBySlugAsync(string slug, CancellationToken ct = default);

    /// <summary>Lists story headers (no graph children) in a tenant, optionally for one game.</summary>
    Task<IReadOnlyList<StoryGraphRes>> ListGraphsAsync(StoryListReq request, CancellationToken ct = default);

    /// <summary>Deletes a story and its dependents.</summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>Starts a playthrough.</summary>
    Task<StoryPlayStateRes> StartAsync(StoryStartPlaythroughReq request, CancellationToken ct = default);

    /// <summary>Loads a play snapshot, or null.</summary>
    Task<StoryPlayStateRes?> GetPlaythroughAsync(Guid playthroughId, CancellationToken ct = default);

    /// <summary>Moves available on a playthrough.</summary>
    Task<IReadOnlyList<StoryMoveRes>> GetMovesAsync(Guid playthroughId, CancellationToken ct = default);

    /// <summary>Takes a choice.</summary>
    Task<StoryPlayStateRes> ChooseAsync(Guid playthroughId, StoryChooseReq request, CancellationToken ct = default);

    /// <summary>Advances a cutscene or silent beat.</summary>
    Task<StoryPlayStateRes> AdvanceAsync(Guid playthroughId, CancellationToken ct = default);

    /// <summary>Writes externally owned values, such as game facts, into the playthrough. Changed values fire VariableChanged reactions.</summary>
    Task<StoryPlayStateRes> SetVariablesAsync(Guid playthroughId, StorySetVariablesReq request, CancellationToken ct = default);

    /// <summary>Play log, ordered by sequence.</summary>
    Task<IReadOnlyList<StoryPlaythroughEventRes>> GetEventsAsync(Guid playthroughId, CancellationToken ct = default);

    /// <summary>Events that caused a variable's current value.</summary>
    Task<IReadOnlyList<StoryPlaythroughEventRes>> GetCausesAsync(Guid playthroughId, string variableKey, CancellationToken ct = default);
}
