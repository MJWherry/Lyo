using System.Text.Json;
using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;
using Lyo.Health;
using Lyo.Json;
using Lyo.Postgres;
using Lyo.Query.Models.Common;
using Lyo.Story.Builders;
using Lyo.Story.Enums;
using Lyo.Story.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Lyo.Story.Postgres;

/// <summary>Postgres-backed <see cref="IStoryStore" />.</summary>
public sealed class PostgresStoryStore : IStoryStore, IHealth
{
    private static readonly JsonSerializerOptions Json = LyoJsonSerializerOptions.Create();
    private readonly IDbContextFactory<StoryDbContext> _contextFactory;

    public PostgresStoryStore(IDbContextFactory<StoryDbContext> contextFactory)
    {
        ArgumentHelpers.ThrowIfNull(contextFactory);
        _contextFactory = contextFactory;
    }

    /// <inheritdoc />
    public string HealthCheckName => "story-postgres";

    /// <inheritdoc />
    public Task<HealthResult> CheckHealthAsync(CancellationToken ct = default) => PostgresHealth.CheckAsync(_contextFactory, PostgresStoryOptions.Schema, ct);

    /// <inheritdoc />
    public async Task SaveStoryGraphAsync(Story story, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(story);
        StoryBuilder.ValidateGraph(story);
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        StoryEntity? existing;
        if (story.Id != Guid.Empty)
            existing = await ctx.Stories.FirstOrDefaultAsync(s => s.Id == story.Id, ct).ConfigureAwait(false);
        else
            existing = await ctx.Stories.FirstOrDefaultAsync(s => s.Slug == story.Slug, ct).ConfigureAwait(false);

        if (existing is not null && await ctx.Playthroughs.AnyAsync(p => p.StoryId == existing.Id, ct).ConfigureAwait(false))
            throw new ConflictException($"Story '{existing.Id}' has playthroughs; saving its graph would delete them.");

        var storyId = existing?.Id ?? (story.Id == Guid.Empty ? LyoGuid.CreateV7() : story.Id);
        if (existing is null) {
            existing = new() { Id = storyId, CreatedTimestamp = DateTime.UtcNow };
            ctx.Stories.Add(existing);
        }

        existing.Title = story.Title;
        existing.Slug = story.Slug;
        existing.TenantId = story.TenantId;
        existing.GameKey = story.GameKey;
        existing.Description = story.Description;
        existing.Language = story.Language;
        existing.Status = story.Status;
        existing.CoverImageRef = story.CoverImageRef;
        existing.StartNodeId = null;
        await SaveCatchingConflictAsync(ctx, "Story", ct).ConfigureAwait(false);
        await DeleteGraphChildrenAsync(ctx, storyId, ct).ConfigureAwait(false);
        var sceneIds = new Dictionary<string, Guid>(StringComparer.Ordinal);
        foreach (var scene in story.Scenes) {
            var id = scene.Id == Guid.Empty ? LyoGuid.CreateV7() : scene.Id;
            sceneIds[scene.Key] = id;
            ctx.Scenes.Add(
                new() {
                    Id = id,
                    StoryId = storyId,
                    Key = scene.Key,
                    Title = scene.Title,
                    SortOrder = scene.SortOrder
                });

            scene.Id = id;
        }

        var nodeIds = new Dictionary<string, Guid>(StringComparer.Ordinal);
        foreach (var node in story.Nodes) {
            var id = node.Id == Guid.Empty ? LyoGuid.CreateV7() : node.Id;
            nodeIds[node.Key] = id;
            Guid? sceneId = null;
            if (!string.IsNullOrWhiteSpace(node.SceneKey) && sceneIds.TryGetValue(node.SceneKey, out var sid))
                sceneId = sid;

            ctx.Nodes.Add(
                new() {
                    Id = id,
                    StoryId = storyId,
                    SceneId = sceneId,
                    Key = node.Key,
                    Title = node.Title,
                    IsEnding = node.IsEnding,
                    PresentationKind = node.PresentationKind,
                    Body = node.Body,
                    ContentType = node.ContentType,
                    MediaRef = node.MediaRef
                });

            node.Id = id;
        }

        foreach (var variable in story.Variables) {
            var id = variable.Id == Guid.Empty ? LyoGuid.CreateV7() : variable.Id;
            ctx.Variables.Add(
                new() {
                    Id = id,
                    StoryId = storyId,
                    Key = variable.Key,
                    Name = variable.Name,
                    Type = variable.Type,
                    DefaultValue = variable.DefaultValue,
                    Description = variable.Description
                });

            variable.Id = id;
        }

        var actionIds = new Dictionary<string, Guid>(StringComparer.Ordinal);
        foreach (var action in story.Actions) {
            var id = action.Id == Guid.Empty ? LyoGuid.CreateV7() : action.Id;
            actionIds[action.Key] = id;
            ctx.Actions.Add(
                new() {
                    Id = id,
                    StoryId = storyId,
                    Key = action.Key,
                    Name = action.Name
                });

            action.Id = id;
        }

        foreach (var action in story.Actions) {
            foreach (var effect in action.Effects)
                ctx.Effects.Add(ToEffectEntity(effect, null, actionIds[action.Key], null, nodeIds, actionIds));
        }

        var choiceIds = new Dictionary<string, Guid>(StringComparer.Ordinal);
        foreach (var node in story.Nodes) {
            foreach (var choice in node.Choices) {
                var id = choice.Id == Guid.Empty ? LyoGuid.CreateV7() : choice.Id;
                choiceIds[choice.Key] = id;
                Guid? targetId = null;
                if (!string.IsNullOrWhiteSpace(choice.TargetNodeKey) && nodeIds.TryGetValue(choice.TargetNodeKey, out var tid))
                    targetId = tid;

                ctx.Choices.Add(
                    new() {
                        Id = id,
                        NodeId = nodeIds[node.Key],
                        Key = choice.Key,
                        Prompt = choice.Prompt,
                        SortOrder = choice.SortOrder,
                        TargetNodeId = targetId,
                        VisibleWhenJson = SerializeWhere(choice.VisibleWhen),
                        EnabledWhenJson = SerializeWhere(choice.EnabledWhen)
                    });

                choice.Id = id;
                foreach (var effect in choice.Effects)
                    ctx.Effects.Add(ToEffectEntity(effect, id, null, null, nodeIds, actionIds));
            }
        }

        var reactionIds = new Dictionary<string, Guid>(StringComparer.Ordinal);
        foreach (var reaction in story.Reactions) {
            var id = reaction.Id == Guid.Empty ? LyoGuid.CreateV7() : reaction.Id;
            reactionIds[reaction.Key] = id;
            Guid? scopeNodeId = null;
            if (!string.IsNullOrWhiteSpace(reaction.ScopeNodeKey) && nodeIds.TryGetValue(reaction.ScopeNodeKey, out var nid))
                scopeNodeId = nid;

            ctx.Reactions.Add(
                new() {
                    Id = id,
                    StoryId = storyId,
                    Key = reaction.Key,
                    Trigger = reaction.Trigger,
                    ScopeNodeId = scopeNodeId,
                    ScopeVariableKey = reaction.ScopeVariableKey,
                    Once = reaction.Once,
                    WhenPredicateJson = SerializeWhere(reaction.When)
                });

            reaction.Id = id;
            foreach (var effect in reaction.Effects)
                ctx.Effects.Add(ToEffectEntity(effect, null, null, id, nodeIds, actionIds));
        }

        if (!string.IsNullOrWhiteSpace(story.StartNodeKey) && nodeIds.TryGetValue(story.StartNodeKey, out var startId))
            existing.StartNodeId = startId;

        await SaveCatchingConflictAsync(ctx, "Story", ct).ConfigureAwait(false);
        story.Id = storyId;
    }

    /// <inheritdoc />
    public async Task<Story?> GetStoryByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await LoadStoryQuery(ctx).FirstOrDefaultAsync(s => s.Id == id, ct).ConfigureAwait(false);
        return entity == null ? null : ToStory(entity);
    }

    /// <inheritdoc />
    public async Task<Story?> GetStoryBySlugAsync(string slug, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(slug);
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await LoadStoryQuery(ctx).FirstOrDefaultAsync(s => s.Slug == slug, ct).ConfigureAwait(false);
        return entity == null ? null : ToStory(entity);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Story>> ListStoriesAsync(Guid? tenantId, string? gameKey, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var query = ctx.Stories.AsNoTracking().Where(s => s.TenantId == tenantId);
        if (gameKey != null)
            query = query.Where(s => s.GameKey == gameKey);

        var rows = await query.OrderBy(s => s.Title).ThenBy(s => s.Id).ToListAsync(ct).ConfigureAwait(false);
        return rows.Select(ToStory).ToArray();
    }

    /// <inheritdoc />
    public async Task DeleteStoryAsync(Guid id, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var story = await ctx.Stories.FirstOrDefaultAsync(s => s.Id == id, ct).ConfigureAwait(false);
        if (story is null)
            return;

        story.StartNodeId = null;
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
        await ctx.Stories.Where(s => s.Id == id).ExecuteDeleteAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<StoryPlayState> StartPlaythroughAsync(Guid storyId, string? playerRef = null, CancellationToken ct = default)
    {
        var story = await GetStoryByIdAsync(storyId, ct).ConfigureAwait(false) ?? throw NotFoundException.ForResource("Story", storyId);
        var snapshot = StoryPlayEngine.Start(story);
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var playthroughId = LyoGuid.CreateV7();
        await PersistSnapshotAsync(ctx, story, playthroughId, playerRef, snapshot, false, ct).ConfigureAwait(false);
        return await GetPlaythroughAsync(playthroughId, ct).ConfigureAwait(false) ?? throw new StoryException("Playthrough was not saved.");
    }

    /// <inheritdoc />
    public async Task<StoryPlayState?> GetPlaythroughAsync(Guid playthroughId, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await LoadPlaythroughQuery(ctx).FirstOrDefaultAsync(p => p.Id == playthroughId, ct).ConfigureAwait(false);
        if (entity is null)
            return null;

        var story = ToStory(await LoadStoryQuery(ctx).FirstAsync(s => s.Id == entity.StoryId, ct).ConfigureAwait(false));
        var snapshot = ToSnapshot(entity, story);
        return ToPlayState(entity, story, snapshot);
    }

    /// <inheritdoc />
    public Task<StoryPlayState> TakeChoiceAsync(Guid playthroughId, string choiceKey, CancellationToken ct = default)
        => MutatePlayAsync(playthroughId, (story, snap) => StoryPlayEngine.TakeChoice(story, snap, choiceKey), ct);

    /// <inheritdoc />
    public Task<StoryPlayState> AdvanceAsync(Guid playthroughId, CancellationToken ct = default) => MutatePlayAsync(playthroughId, StoryPlayEngine.Advance, ct);

    /// <inheritdoc />
    public Task<StoryPlayState> SetVariablesAsync(Guid playthroughId, IReadOnlyDictionary<string, string?> values, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(values);
        return MutatePlayAsync(playthroughId, (story, snap) => StoryPlayEngine.SetVariables(story, snap, values), ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StoryPlaythroughEvent>> GetEventsAsync(Guid playthroughId, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await LoadPlaythroughQuery(ctx).FirstOrDefaultAsync(p => p.Id == playthroughId, ct).ConfigureAwait(false);
        if (entity is null)
            return [];

        var story = ToStory(await LoadStoryQuery(ctx).FirstAsync(s => s.Id == entity.StoryId, ct).ConfigureAwait(false));
        return ToSnapshot(entity, story).Events;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StoryPlaythroughEvent>> GetCausesAsync(Guid playthroughId, string variableKey, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(variableKey);
        var events = await GetEventsAsync(playthroughId, ct).ConfigureAwait(false);
        return StoryPlayEngine.GetCauses(events, variableKey);
    }

    private async Task<StoryPlayState> MutatePlayAsync(Guid playthroughId, Func<Story, StoryPlaySnapshot, StoryPlaySnapshot> mutate, CancellationToken ct)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await LoadPlaythroughQuery(ctx).FirstOrDefaultAsync(p => p.Id == playthroughId, ct).ConfigureAwait(false) ??
            throw NotFoundException.ForResource("StoryPlaythrough", playthroughId);

        var story = ToStory(await LoadStoryQuery(ctx).FirstAsync(s => s.Id == entity.StoryId, ct).ConfigureAwait(false));
        var snapshot = ToSnapshot(entity, story);
        mutate(story, snapshot);
        await PersistSnapshotAsync(ctx, story, playthroughId, entity.PlayerRef, snapshot, true, ct).ConfigureAwait(false);
        return await GetPlaythroughAsync(playthroughId, ct).ConfigureAwait(false) ?? throw new StoryException("Playthrough was not saved.");
    }

    private static async Task DeleteGraphChildrenAsync(StoryDbContext ctx, Guid storyId, CancellationToken ct)
    {
        var nodeIds = await ctx.Nodes.Where(n => n.StoryId == storyId).Select(n => n.Id).ToListAsync(ct).ConfigureAwait(false);
        var choiceIds = await ctx.Choices.Where(c => nodeIds.Contains(c.NodeId)).Select(c => c.Id).ToListAsync(ct).ConfigureAwait(false);
        var actionIds = await ctx.Actions.Where(a => a.StoryId == storyId).Select(a => a.Id).ToListAsync(ct).ConfigureAwait(false);
        var reactionIds = await ctx.Reactions.Where(r => r.StoryId == storyId).Select(r => r.Id).ToListAsync(ct).ConfigureAwait(false);
        await ctx.Effects.Where(e
                => (e.ChoiceId != null && choiceIds.Contains(e.ChoiceId.Value)) || (e.ActionId != null && actionIds.Contains(e.ActionId.Value)) ||
                (e.ReactionId != null && reactionIds.Contains(e.ReactionId.Value)))
            .ExecuteDeleteAsync(ct)
            .ConfigureAwait(false);

        await ctx.Choices.Where(c => nodeIds.Contains(c.NodeId)).ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await ctx.Reactions.Where(r => r.StoryId == storyId).ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await ctx.Actions.Where(a => a.StoryId == storyId).ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await ctx.Variables.Where(v => v.StoryId == storyId).ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await ctx.Nodes.Where(n => n.StoryId == storyId).ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await ctx.Scenes.Where(s => s.StoryId == storyId).ExecuteDeleteAsync(ct).ConfigureAwait(false);
    }

    private async Task PersistSnapshotAsync(StoryDbContext ctx, Story story, Guid playthroughId, string? playerRef, StoryPlaySnapshot snapshot, bool replace, CancellationToken ct)
    {
        var nodeByKey = story.Nodes.ToDictionary(n => n.Key, n => n.Id, StringComparer.Ordinal);
        var choiceByKey = story.Nodes.SelectMany(n => n.Choices).ToDictionary(c => c.Key, c => c.Id, StringComparer.Ordinal);
        var reactionByKey = story.Reactions.ToDictionary(r => r.Key, r => r.Id, StringComparer.Ordinal);
        var actionByKey = story.Actions.ToDictionary(a => a.Key, a => a.Id, StringComparer.Ordinal);
        Guid? currentNodeId = null;
        if (!string.IsNullOrWhiteSpace(snapshot.CurrentNodeKey) && nodeByKey.TryGetValue(snapshot.CurrentNodeKey, out var nid))
            currentNodeId = nid;

        StoryPlaythroughEntity entity;
        if (replace) {
            entity = await ctx.Playthroughs.FirstAsync(p => p.Id == playthroughId, ct).ConfigureAwait(false);
            await ctx.PlaythroughEvents.Where(e => e.PlaythroughId == playthroughId).ExecuteDeleteAsync(ct).ConfigureAwait(false);
            await ctx.PlaythroughVariables.Where(v => v.PlaythroughId == playthroughId).ExecuteDeleteAsync(ct).ConfigureAwait(false);
        }
        else {
            entity = new() {
                Id = playthroughId,
                StoryId = story.Id,
                PlayerRef = playerRef,
                StartedAt = DateTime.UtcNow
            };

            ctx.Playthroughs.Add(entity);
        }

        entity.Status = snapshot.Status;
        entity.CurrentNodeId = currentNodeId;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.CompletedAt = snapshot.Status == StoryPlaythroughStatus.Completed ? DateTime.UtcNow : null;
        var now = DateTime.UtcNow;
        foreach (var pair in snapshot.Variables)
            ctx.PlaythroughVariables.Add(
                new() {
                    PlaythroughId = playthroughId,
                    VariableKey = pair.Key,
                    Value = pair.Value,
                    UpdatedAt = now
                });

        foreach (var evt in snapshot.Events) {
            Guid? nodeId = evt.NodeKey is not null && nodeByKey.TryGetValue(evt.NodeKey, out var n) ? n : null;
            Guid? choiceId = evt.ChoiceKey is not null && choiceByKey.TryGetValue(evt.ChoiceKey, out var c) ? c : null;
            Guid? reactionId = evt.ReactionKey is not null && reactionByKey.TryGetValue(evt.ReactionKey, out var r) ? r : null;
            Guid? actionId = evt.ActionKey is not null && actionByKey.TryGetValue(evt.ActionKey, out var a) ? a : null;
            ctx.PlaythroughEvents.Add(
                new() {
                    Id = evt.Id == Guid.Empty ? LyoGuid.CreateV7() : evt.Id,
                    PlaythroughId = playthroughId,
                    Sequence = evt.Sequence,
                    Kind = evt.Kind,
                    NodeId = nodeId,
                    ChoiceId = choiceId,
                    ReactionId = reactionId,
                    ActionId = actionId,
                    VariableKey = evt.VariableKey,
                    OldValue = evt.OldValue,
                    NewValue = evt.NewValue,
                    CausedByEventId = evt.CausedByEventId,
                    OccurredAt = evt.OccurredAt == default ? now : evt.OccurredAt
                });
        }

        await SaveCatchingConflictAsync(ctx, "StoryPlaythrough", ct).ConfigureAwait(false);
    }

    private static IQueryable<StoryEntity> LoadStoryQuery(StoryDbContext ctx)
        => ctx.Stories.AsNoTracking()
            .Include(s => s.StartNode)
            .Include(s => s.Scenes)
            .Include(s => s.Variables)
            .Include(s => s.Actions)
            .ThenInclude(a => a.Effects)
            .Include(s => s.Reactions)
            .ThenInclude(r => r.Effects)
            .Include(s => s.Reactions)
            .ThenInclude(r => r.ScopeNode)
            .Include(s => s.Nodes)
            .ThenInclude(n => n.Scene)
            .Include(s => s.Nodes)
            .ThenInclude(n => n.Choices)
            .ThenInclude(c => c.Effects)
            .Include(s => s.Nodes)
            .ThenInclude(n => n.Choices)
            .ThenInclude(c => c.TargetNode);

    private static IQueryable<StoryPlaythroughEntity> LoadPlaythroughQuery(StoryDbContext ctx)
        => ctx.Playthroughs.AsNoTracking()
            .Include(p => p.CurrentNode)
            .Include(p => p.Variables)
            .Include(p => p.Events)
            .ThenInclude(e => e.Node)
            .Include(p => p.Events)
            .ThenInclude(e => e.Choice)
            .Include(p => p.Events)
            .ThenInclude(e => e.Reaction)
            .Include(p => p.Events)
            .ThenInclude(e => e.Action);

    private static Story ToStory(StoryEntity entity)
    {
        var nodeKeyById = entity.Nodes.ToDictionary(n => n.Id, n => n.Key);
        var actionKeyById = entity.Actions.ToDictionary(a => a.Id, a => a.Key);
        return new() {
            Id = entity.Id,
            Title = entity.Title,
            Slug = entity.Slug,
            TenantId = entity.TenantId,
            GameKey = entity.GameKey,
            Description = entity.Description,
            Language = entity.Language,
            Status = entity.Status,
            StartNodeKey = entity.StartNode?.Key,
            CoverImageRef = entity.CoverImageRef,
            CreatedTimestamp = entity.CreatedTimestamp,
            UpdatedTimestamp = entity.UpdatedTimestamp,
            Scenes = entity.Scenes.OrderBy(s => s.SortOrder)
                .Select(s => new StoryScene {
                    Id = s.Id,
                    Key = s.Key,
                    Title = s.Title,
                    SortOrder = s.SortOrder
                })
                .ToArray(),
            Variables = entity.Variables.Select(v => new StoryVariable {
                    Id = v.Id,
                    Key = v.Key,
                    Name = v.Name,
                    Type = v.Type,
                    DefaultValue = v.DefaultValue,
                    Description = v.Description
                })
                .ToArray(),
            Actions = entity.Actions.Select(a => new StoryAction {
                    Id = a.Id,
                    Key = a.Key,
                    Name = a.Name,
                    Effects = a.Effects.OrderBy(e => e.SortOrder).Select(e => ToEffect(e, nodeKeyById, actionKeyById)).ToArray()
                })
                .ToArray(),
            Reactions = entity.Reactions.Select(r => new StoryReaction {
                    Id = r.Id,
                    Key = r.Key,
                    Trigger = r.Trigger,
                    ScopeNodeKey = r.ScopeNode?.Key,
                    ScopeVariableKey = r.ScopeVariableKey,
                    Once = r.Once,
                    When = DeserializeWhere(r.WhenPredicateJson),
                    Effects = r.Effects.OrderBy(e => e.SortOrder).Select(e => ToEffect(e, nodeKeyById, actionKeyById)).ToArray()
                })
                .ToArray(),
            Nodes = entity.Nodes.Select(n => new StoryNode {
                    Id = n.Id,
                    Key = n.Key,
                    Title = n.Title,
                    SceneKey = n.Scene?.Key,
                    IsEnding = n.IsEnding,
                    PresentationKind = n.PresentationKind,
                    Body = n.Body,
                    ContentType = n.ContentType,
                    MediaRef = n.MediaRef,
                    Choices = n.Choices.OrderBy(c => c.SortOrder)
                        .Select(c => new StoryChoice {
                            Id = c.Id,
                            Key = c.Key,
                            Prompt = c.Prompt,
                            SortOrder = c.SortOrder,
                            TargetNodeKey = c.TargetNode?.Key,
                            VisibleWhen = DeserializeWhere(c.VisibleWhenJson),
                            EnabledWhen = DeserializeWhere(c.EnabledWhenJson),
                            Effects = c.Effects.OrderBy(e => e.SortOrder).Select(e => ToEffect(e, nodeKeyById, actionKeyById)).ToArray()
                        })
                        .ToArray()
                })
                .ToArray()
        };
    }

    private static StoryPlaySnapshot ToSnapshot(StoryPlaythroughEntity entity, Story story)
    {
        var snapshot = new StoryPlaySnapshot { CurrentNodeKey = entity.CurrentNode?.Key, Status = entity.Status };
        foreach (var variable in entity.Variables)
            snapshot.Variables[variable.VariableKey] = variable.Value;

        foreach (var evt in entity.Events.OrderBy(e => e.Sequence)) {
            snapshot.Events.Add(
                new() {
                    Id = evt.Id,
                    Sequence = evt.Sequence,
                    Kind = evt.Kind,
                    NodeKey = evt.Node?.Key,
                    ChoiceKey = evt.Choice?.Key,
                    ReactionKey = evt.Reaction?.Key,
                    ActionKey = evt.Action?.Key,
                    VariableKey = evt.VariableKey,
                    OldValue = evt.OldValue,
                    NewValue = evt.NewValue,
                    CausedByEventId = evt.CausedByEventId,
                    OccurredAt = evt.OccurredAt
                });

            if (evt.Kind == StoryPlaythroughEventKind.ChoiceTaken && evt.Choice?.Key is { } choiceKey)
                snapshot.Chose.Add(choiceKey);

            if (evt.Kind == StoryPlaythroughEventKind.NodeEntered && evt.Node?.Key is { } nodeKey)
                snapshot.Visited.Add(nodeKey);

            if (evt.Kind == StoryPlaythroughEventKind.ReactionFired && evt.Reaction is { Once: true, Key: { } reactionKey })
                snapshot.FiredOnce.Add(reactionKey);
        }

        return snapshot;
    }

    private static StoryPlayState ToPlayState(StoryPlaythroughEntity entity, Story story, StoryPlaySnapshot snapshot)
    {
        var node = story.Nodes.FirstOrDefault(n => n.Key == snapshot.CurrentNodeKey);
        return new() {
            Playthrough = new() {
                Id = entity.Id,
                StoryId = entity.StoryId,
                PlayerRef = entity.PlayerRef,
                Status = entity.Status,
                CurrentNodeKey = snapshot.CurrentNodeKey,
                StartedAt = entity.StartedAt,
                UpdatedAt = entity.UpdatedAt,
                CompletedAt = entity.CompletedAt,
                Variables = snapshot.Variables.Select(p => new StoryPlaythroughVariable { VariableKey = p.Key, Value = p.Value, UpdatedAt = entity.UpdatedAt ?? entity.StartedAt })
                    .ToArray()
            },
            CurrentNode = node,
            Moves = StoryPlayEngine.AvailableMoves(story, snapshot)
        };
    }

    private static StoryEffect ToEffect(StoryEffectEntity entity, Dictionary<Guid, string> nodeKeyById, Dictionary<Guid, string> actionKeyById)
        => new() {
            Id = entity.Id,
            Kind = entity.Kind,
            SortOrder = entity.SortOrder,
            VariableKey = entity.VariableKey,
            Value = entity.Value,
            TargetNodeKey = entity.TargetNodeId is Guid nid && nodeKeyById.TryGetValue(nid, out var nk) ? nk : null,
            InvokeActionKey = entity.InvokeActionId is Guid aid && actionKeyById.TryGetValue(aid, out var ak) ? ak : null
        };

    private static StoryEffectEntity ToEffectEntity(
        StoryEffect effect,
        Guid? choiceId,
        Guid? actionId,
        Guid? reactionId,
        Dictionary<string, Guid> nodeIds,
        Dictionary<string, Guid> actionIds)
    {
        Guid? targetNodeId = null;
        if (!string.IsNullOrWhiteSpace(effect.TargetNodeKey) && nodeIds.TryGetValue(effect.TargetNodeKey, out var nid))
            targetNodeId = nid;

        Guid? invokeActionId = null;
        if (!string.IsNullOrWhiteSpace(effect.InvokeActionKey) && actionIds.TryGetValue(effect.InvokeActionKey, out var aid))
            invokeActionId = aid;

        return new() {
            Id = effect.Id == Guid.Empty ? LyoGuid.CreateV7() : effect.Id,
            ChoiceId = choiceId,
            ActionId = actionId,
            ReactionId = reactionId,
            SortOrder = effect.SortOrder,
            Kind = effect.Kind,
            VariableKey = effect.VariableKey,
            Value = effect.Value,
            TargetNodeId = targetNodeId,
            InvokeActionId = invokeActionId
        };
    }

    private static string? SerializeWhere(WhereClause? clause) => clause is null ? null : JsonSerializer.Serialize(clause, Json);

    private static WhereClause? DeserializeWhere(string? json) => string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<WhereClause>(json, Json);

    private static async Task SaveCatchingConflictAsync(StoryDbContext ctx, string resource, CancellationToken ct)
    {
        try {
            await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }) {
            throw ConflictException.ForResource(resource, innerException: ex);
        }
    }
}