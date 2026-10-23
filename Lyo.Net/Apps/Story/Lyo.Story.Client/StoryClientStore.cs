using Lyo.Exceptions;
using Lyo.Story.Models.Request;
using Lyo.Story.Models.Response;

namespace Lyo.Story.Client;

/// <summary>
/// <see cref="IStoryStore" /> over the Story HTTP API, so game code written against the store runs unchanged against a remote server. Register an <see cref="IStoryClient" />
/// first (see <c>AddStoryClient</c>). Missing stories and playthroughs return null, matching the local stores.
/// </summary>
public sealed class StoryClientStore : IStoryStore
{
    private readonly IStoryClient _client;

    public StoryClientStore(IStoryClient client)
    {
        ArgumentHelpers.ThrowIfNull(client);
        _client = client;
    }

    /// <inheritdoc />
    public async Task SaveStoryGraphAsync(Story story, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(story);
        var saved = await _client.SaveGraphAsync(ToReq(story), ct).ConfigureAwait(false);
        story.Id = saved.Id;
    }

    /// <inheritdoc />
    public async Task<Story?> GetStoryByIdAsync(Guid id, CancellationToken ct = default)
    {
        var graph = await _client.GetGraphAsync(id, ct).ConfigureAwait(false);
        return graph is null ? null : ToDomain(graph);
    }

    /// <inheritdoc />
    public async Task<Story?> GetStoryBySlugAsync(string slug, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(slug);
        var graph = await _client.GetGraphBySlugAsync(slug, ct).ConfigureAwait(false);
        return graph is null ? null : ToDomain(graph);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Story>> ListStoriesAsync(Guid? tenantId, string? gameKey, CancellationToken ct = default)
        => (await _client.ListGraphsAsync(new() { TenantId = tenantId, GameKey = gameKey }, ct).ConfigureAwait(false)).Select(ToDomain).ToArray();

    /// <inheritdoc />
    public Task DeleteStoryAsync(Guid id, CancellationToken ct = default) => _client.DeleteAsync(id, ct);

    /// <inheritdoc />
    public async Task<StoryPlayState> StartPlaythroughAsync(Guid storyId, string? playerRef = null, CancellationToken ct = default)
        => ToDomain(await _client.StartAsync(new() { StoryId = storyId, PlayerRef = playerRef }, ct).ConfigureAwait(false));

    /// <inheritdoc />
    public async Task<StoryPlayState?> GetPlaythroughAsync(Guid playthroughId, CancellationToken ct = default)
    {
        var state = await _client.GetPlaythroughAsync(playthroughId, ct).ConfigureAwait(false);
        return state is null ? null : ToDomain(state);
    }

    /// <inheritdoc />
    public async Task<StoryPlayState> TakeChoiceAsync(Guid playthroughId, string choiceKey, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(choiceKey);
        return ToDomain(await _client.ChooseAsync(playthroughId, new() { ChoiceKey = choiceKey }, ct).ConfigureAwait(false));
    }

    /// <inheritdoc />
    public async Task<StoryPlayState> AdvanceAsync(Guid playthroughId, CancellationToken ct = default)
        => ToDomain(await _client.AdvanceAsync(playthroughId, ct).ConfigureAwait(false));

    /// <inheritdoc />
    public async Task<StoryPlayState> SetVariablesAsync(Guid playthroughId, IReadOnlyDictionary<string, string?> values, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(values);
        var req = new StorySetVariablesReq();
        foreach (var pair in values)
            req.Values[pair.Key] = pair.Value;

        return ToDomain(await _client.SetVariablesAsync(playthroughId, req, ct).ConfigureAwait(false));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StoryPlaythroughEvent>> GetEventsAsync(Guid playthroughId, CancellationToken ct = default)
        => (await _client.GetEventsAsync(playthroughId, ct).ConfigureAwait(false)).Select(ToDomain).ToArray();

    /// <inheritdoc />
    public async Task<IReadOnlyList<StoryPlaythroughEvent>> GetCausesAsync(Guid playthroughId, string variableKey, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(variableKey);
        return (await _client.GetCausesAsync(playthroughId, variableKey, ct).ConfigureAwait(false)).Select(ToDomain).ToArray();
    }

    private static StoryGraphReq ToReq(Story story)
        => new() {
            Id = story.Id,
            Title = story.Title,
            Slug = story.Slug,
            TenantId = story.TenantId,
            GameKey = story.GameKey,
            Description = story.Description,
            Language = story.Language,
            Status = story.Status,
            StartNodeKey = story.StartNodeKey,
            CoverImageRef = story.CoverImageRef,
            Scenes = story.Scenes.Select(s => new StoryGraphSceneReq { Key = s.Key, Title = s.Title, SortOrder = s.SortOrder }).ToList(),
            Nodes = story.Nodes.Select(n => new StoryGraphNodeReq {
                    Key = n.Key,
                    Title = n.Title,
                    SceneKey = n.SceneKey,
                    IsEnding = n.IsEnding,
                    PresentationKind = n.PresentationKind,
                    Body = n.Body,
                    ContentType = n.ContentType,
                    MediaRef = n.MediaRef,
                    Choices = n.Choices.Select(c => new StoryGraphChoiceReq {
                            Key = c.Key,
                            Prompt = c.Prompt,
                            SortOrder = c.SortOrder,
                            TargetNodeKey = c.TargetNodeKey,
                            VisibleWhen = c.VisibleWhen,
                            EnabledWhen = c.EnabledWhen,
                            Effects = c.Effects.Select(ToReq).ToList()
                        })
                        .ToList()
                })
                .ToList(),
            Variables = story.Variables.Select(v => new StoryGraphVariableReq {
                    Key = v.Key,
                    Name = v.Name,
                    Type = v.Type,
                    DefaultValue = v.DefaultValue,
                    Description = v.Description
                })
                .ToList(),
            Actions = story.Actions.Select(a => new StoryGraphActionReq { Key = a.Key, Name = a.Name, Effects = a.Effects.Select(ToReq).ToList() }).ToList(),
            Reactions = story.Reactions.Select(r => new StoryGraphReactionReq {
                    Key = r.Key,
                    Trigger = r.Trigger,
                    ScopeNodeKey = r.ScopeNodeKey,
                    ScopeVariableKey = r.ScopeVariableKey,
                    Once = r.Once,
                    When = r.When,
                    Effects = r.Effects.Select(ToReq).ToList()
                })
                .ToList()
        };

    private static StoryGraphEffectReq ToReq(StoryEffect effect)
        => new() {
            Kind = effect.Kind,
            SortOrder = effect.SortOrder,
            VariableKey = effect.VariableKey,
            Value = effect.Value,
            TargetNodeKey = effect.TargetNodeKey,
            InvokeActionKey = effect.InvokeActionKey
        };

    private static Story ToDomain(StoryGraphRes graph)
        => new() {
            Id = graph.Id,
            Title = graph.Title,
            Slug = graph.Slug,
            TenantId = graph.TenantId,
            GameKey = graph.GameKey,
            Description = graph.Description,
            Language = graph.Language,
            Status = graph.Status,
            StartNodeKey = graph.StartNodeKey,
            CoverImageRef = graph.CoverImageRef,
            CreatedTimestamp = graph.CreatedTimestamp,
            UpdatedTimestamp = graph.UpdatedTimestamp,
            Scenes = graph.Scenes.Select(s => new StoryScene {
                    Id = s.Id,
                    Key = s.Key,
                    Title = s.Title,
                    SortOrder = s.SortOrder
                })
                .ToArray(),
            Nodes = graph.Nodes.Select(n => new StoryNode {
                    Id = n.Id,
                    Key = n.Key,
                    Title = n.Title,
                    SceneKey = n.SceneKey,
                    IsEnding = n.IsEnding,
                    PresentationKind = n.PresentationKind,
                    Body = n.Body,
                    ContentType = n.ContentType,
                    MediaRef = n.MediaRef,
                    Choices = n.Choices.Select(c => new StoryChoice {
                            Id = c.Id,
                            Key = c.Key,
                            Prompt = c.Prompt,
                            SortOrder = c.SortOrder,
                            TargetNodeKey = c.TargetNodeKey,
                            VisibleWhen = c.VisibleWhen,
                            EnabledWhen = c.EnabledWhen,
                            Effects = c.Effects.Select(ToDomain).ToArray()
                        })
                        .ToArray()
                })
                .ToArray(),
            Variables = graph.Variables.Select(v => new StoryVariable {
                    Id = v.Id,
                    Key = v.Key,
                    Name = v.Name,
                    Type = v.Type,
                    DefaultValue = v.DefaultValue,
                    Description = v.Description
                })
                .ToArray(),
            Actions = graph.Actions.Select(a => new StoryAction {
                    Id = a.Id,
                    Key = a.Key,
                    Name = a.Name,
                    Effects = a.Effects.Select(ToDomain).ToArray()
                })
                .ToArray(),
            Reactions = graph.Reactions.Select(r => new StoryReaction {
                    Id = r.Id,
                    Key = r.Key,
                    Trigger = r.Trigger,
                    ScopeNodeKey = r.ScopeNodeKey,
                    ScopeVariableKey = r.ScopeVariableKey,
                    Once = r.Once,
                    When = r.When,
                    Effects = r.Effects.Select(ToDomain).ToArray()
                })
                .ToArray()
        };

    private static StoryEffect ToDomain(StoryGraphEffectRes effect)
        => new() {
            Id = effect.Id,
            Kind = effect.Kind,
            SortOrder = effect.SortOrder,
            VariableKey = effect.VariableKey,
            Value = effect.Value,
            TargetNodeKey = effect.TargetNodeKey,
            InvokeActionKey = effect.InvokeActionKey
        };

    private static StoryPlayState ToDomain(StoryPlayStateRes state)
        => new() {
            Playthrough = new() {
                Id = state.Playthrough.Id,
                StoryId = state.Playthrough.StoryId,
                PlayerRef = state.Playthrough.PlayerRef,
                Status = state.Playthrough.Status,
                CurrentNodeKey = state.Playthrough.CurrentNodeKey,
                StartedAt = state.Playthrough.StartedAt,
                UpdatedAt = state.Playthrough.UpdatedAt,
                CompletedAt = state.Playthrough.CompletedAt,
                Variables = state.Playthrough.Variables.Select(v => new StoryPlaythroughVariable { VariableKey = v.VariableKey, Value = v.Value, UpdatedAt = v.UpdatedAt })
                    .ToArray()
            },
            CurrentNode = state.CurrentNode is null
                ? null
                : new StoryNode {
                    Key = state.CurrentNode.Key,
                    Title = state.CurrentNode.Title,
                    IsEnding = state.CurrentNode.IsEnding,
                    PresentationKind = state.CurrentNode.PresentationKind,
                    Body = state.CurrentNode.Body,
                    ContentType = state.CurrentNode.ContentType,
                    MediaRef = state.CurrentNode.MediaRef
                },
            Moves = state.Moves.Select(m => new StoryMove {
                    Kind = m.Kind,
                    ChoiceKey = m.ChoiceKey,
                    Prompt = m.Prompt,
                    Enabled = m.Enabled
                })
                .ToArray()
        };

    private static StoryPlaythroughEvent ToDomain(StoryPlaythroughEventRes ev)
        => new() {
            Id = ev.Id,
            Sequence = ev.Sequence,
            Kind = ev.Kind,
            NodeKey = ev.NodeKey,
            ChoiceKey = ev.ChoiceKey,
            ReactionKey = ev.ReactionKey,
            ActionKey = ev.ActionKey,
            VariableKey = ev.VariableKey,
            OldValue = ev.OldValue,
            NewValue = ev.NewValue,
            CausedByEventId = ev.CausedByEventId,
            OccurredAt = ev.OccurredAt
        };
}