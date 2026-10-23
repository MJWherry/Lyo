namespace Lyo.Story;

/// <summary>Persistence and play facade for story graphs and playthroughs.</summary>
public interface IStoryStore
{
    /// <summary>Upserts a full story aggregate (scenes, nodes, choices, variables, actions, reactions, effects) in one transaction. Resolves keys to ids.</summary>
    /// <exception cref="Lyo.Exceptions.Models.ConflictException">The slug is taken by another story, or the story already has playthroughs.</exception>
    Task SaveStoryGraphAsync(Story story, CancellationToken ct = default);

    /// <summary>Loads a story by id including the full graph. Returns null if not found.</summary>
    Task<Story?> GetStoryByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Loads a story by slug including the full graph. Returns null if not found.</summary>
    Task<Story?> GetStoryBySlugAsync(string slug, CancellationToken ct = default);

    /// <summary>
    /// Lists story headers (no scenes, nodes, variables, actions, or reactions) in <paramref name="tenantId" />; a null tenant lists only stories with no tenant. When
    /// <paramref name="gameKey" /> is set, only that game's stories are listed. Ordered by title.
    /// </summary>
    Task<IReadOnlyList<Story>> ListStoriesAsync(Guid? tenantId, string? gameKey, CancellationToken ct = default);

    /// <summary>Deletes a story and all dependents including playthroughs. Missing ids are a no-op. List and filter go through Lyo.Query, not this store.</summary>
    Task DeleteStoryAsync(Guid id, CancellationToken ct = default);

    /// <summary>Starts a playthrough at the story's start node with variable defaults applied.</summary>
    Task<StoryPlayState> StartPlaythroughAsync(Guid storyId, string? playerRef = null, CancellationToken ct = default);

    /// <summary>Loads a playthrough, current node, and available moves. Returns null if not found.</summary>
    Task<StoryPlayState?> GetPlaythroughAsync(Guid playthroughId, CancellationToken ct = default);

    /// <summary>Takes an enabled choice on the current node.</summary>
    Task<StoryPlayState> TakeChoiceAsync(Guid playthroughId, string choiceKey, CancellationToken ct = default);

    /// <summary>Advances a cutscene or silent beat that has no player choice.</summary>
    Task<StoryPlayState> AdvanceAsync(Guid playthroughId, CancellationToken ct = default);

    /// <summary>
    /// Writes externally owned values, such as game facts, into the playthrough's variable bag. Changed values are logged and fire VariableChanged reactions (see
    /// <see cref="StoryPlayEngine.SetVariables" />).
    /// </summary>
    /// <exception cref="Lyo.Exceptions.Models.NotFoundException">The playthrough does not exist.</exception>
    Task<StoryPlayState> SetVariablesAsync(Guid playthroughId, IReadOnlyDictionary<string, string?> values, CancellationToken ct = default);

    /// <summary>Append-only event log for a playthrough, ordered by sequence.</summary>
    Task<IReadOnlyList<StoryPlaythroughEvent>> GetEventsAsync(Guid playthroughId, CancellationToken ct = default);

    /// <summary>Walks <see cref="StoryPlaythroughEvent.CausedByEventId" /> for the given variable.</summary>
    Task<IReadOnlyList<StoryPlaythroughEvent>> GetCausesAsync(Guid playthroughId, string variableKey, CancellationToken ct = default);
}