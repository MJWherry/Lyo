using Lyo.Story.Models.Request;
using Lyo.Story.Models.Response;

namespace Lyo.Story.Api;

/// <summary>Maps graph and play wire types to and from the domain records in <c>Lyo.Story</c>.</summary>
internal static class StoryGraphMapping
{
    public static Story ToDomain(StoryGraphReq req)
        => new() {
            Id = req.Id,
            Title = req.Title,
            Slug = req.Slug,
            TenantId = req.TenantId,
            GameKey = req.GameKey,
            Description = req.Description,
            Language = req.Language,
            Status = req.Status,
            StartNodeKey = req.StartNodeKey,
            CoverImageRef = req.CoverImageRef,
            Scenes = req.Scenes.Select(s => new StoryScene { Key = s.Key, Title = s.Title, SortOrder = s.SortOrder }).ToArray(),
            Nodes = req.Nodes.Select(ToDomain).ToArray(),
            Variables = req.Variables.Select(v => new StoryVariable { Key = v.Key, Name = v.Name, Type = v.Type, DefaultValue = v.DefaultValue, Description = v.Description }).ToArray(),
            Actions = req.Actions.Select(a => new StoryAction { Key = a.Key, Name = a.Name, Effects = a.Effects.Select(ToDomain).ToArray() }).ToArray(),
            Reactions = req.Reactions.Select(ToDomain).ToArray()
        };

    public static StoryGraphRes ToRes(Story story)
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
            CreatedTimestamp = story.CreatedTimestamp,
            UpdatedTimestamp = story.UpdatedTimestamp,
            Scenes = story.Scenes.Select(s => new StoryGraphSceneRes { Id = s.Id, Key = s.Key, Title = s.Title, SortOrder = s.SortOrder }).ToArray(),
            Nodes = story.Nodes.Select(ToRes).ToArray(),
            Variables = story.Variables.Select(v => new StoryGraphVariableRes {
                Id = v.Id, Key = v.Key, Name = v.Name, Type = v.Type, DefaultValue = v.DefaultValue, Description = v.Description
            }).ToArray(),
            Actions = story.Actions.Select(a => new StoryGraphActionRes { Id = a.Id, Key = a.Key, Name = a.Name, Effects = a.Effects.Select(ToRes).ToArray() }).ToArray(),
            Reactions = story.Reactions.Select(ToRes).ToArray()
        };

    public static StoryPlayStateRes ToRes(StoryPlayState state)
        => new() {
            Playthrough = new StoryPlaySnapshotRes {
                Id = state.Playthrough.Id,
                StoryId = state.Playthrough.StoryId,
                PlayerRef = state.Playthrough.PlayerRef,
                Status = state.Playthrough.Status,
                CurrentNodeKey = state.Playthrough.CurrentNodeKey,
                StartedAt = state.Playthrough.StartedAt,
                UpdatedAt = state.Playthrough.UpdatedAt,
                CompletedAt = state.Playthrough.CompletedAt,
                Variables = state.Playthrough.Variables.Select(v => new StoryPlayVariableRes { VariableKey = v.VariableKey, Value = v.Value, UpdatedAt = v.UpdatedAt }).ToArray()
            },
            CurrentNode = state.CurrentNode is null ? null : new StoryPlayNodeRes {
                Key = state.CurrentNode.Key,
                Title = state.CurrentNode.Title,
                IsEnding = state.CurrentNode.IsEnding,
                PresentationKind = state.CurrentNode.PresentationKind,
                Body = state.CurrentNode.Body,
                ContentType = state.CurrentNode.ContentType,
                MediaRef = state.CurrentNode.MediaRef
            },
            Moves = state.Moves.Select(m => new StoryMoveRes { Kind = m.Kind, ChoiceKey = m.ChoiceKey, Prompt = m.Prompt, Enabled = m.Enabled }).ToArray()
        };

    public static StoryPlaythroughEventRes ToRes(StoryPlaythroughEvent ev)
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

    private static StoryNode ToDomain(StoryGraphNodeReq node)
        => new() {
            Key = node.Key,
            Title = node.Title,
            SceneKey = node.SceneKey,
            IsEnding = node.IsEnding,
            PresentationKind = node.PresentationKind,
            Body = node.Body,
            ContentType = node.ContentType,
            MediaRef = node.MediaRef,
            Choices = node.Choices.Select(c => new StoryChoice {
                Key = c.Key,
                Prompt = c.Prompt,
                SortOrder = c.SortOrder,
                TargetNodeKey = c.TargetNodeKey,
                VisibleWhen = c.VisibleWhen,
                EnabledWhen = c.EnabledWhen,
                Effects = c.Effects.Select(ToDomain).ToArray()
            }).ToArray()
        };

    private static StoryReaction ToDomain(StoryGraphReactionReq reaction)
        => new() {
            Key = reaction.Key,
            Trigger = reaction.Trigger,
            ScopeNodeKey = reaction.ScopeNodeKey,
            ScopeVariableKey = reaction.ScopeVariableKey,
            Once = reaction.Once,
            When = reaction.When,
            Effects = reaction.Effects.Select(ToDomain).ToArray()
        };

    private static StoryEffect ToDomain(StoryGraphEffectReq effect)
        => new() {
            Kind = effect.Kind,
            SortOrder = effect.SortOrder,
            VariableKey = effect.VariableKey,
            Value = effect.Value,
            TargetNodeKey = effect.TargetNodeKey,
            InvokeActionKey = effect.InvokeActionKey
        };

    private static StoryGraphNodeRes ToRes(StoryNode node)
        => new() {
            Id = node.Id,
            Key = node.Key,
            Title = node.Title,
            SceneKey = node.SceneKey,
            IsEnding = node.IsEnding,
            PresentationKind = node.PresentationKind,
            Body = node.Body,
            ContentType = node.ContentType,
            MediaRef = node.MediaRef,
            Choices = node.Choices.Select(c => new StoryGraphChoiceRes {
                Id = c.Id,
                Key = c.Key,
                Prompt = c.Prompt,
                SortOrder = c.SortOrder,
                TargetNodeKey = c.TargetNodeKey,
                VisibleWhen = c.VisibleWhen,
                EnabledWhen = c.EnabledWhen,
                Effects = c.Effects.Select(ToRes).ToArray()
            }).ToArray()
        };

    private static StoryGraphReactionRes ToRes(StoryReaction reaction)
        => new() {
            Id = reaction.Id,
            Key = reaction.Key,
            Trigger = reaction.Trigger,
            ScopeNodeKey = reaction.ScopeNodeKey,
            ScopeVariableKey = reaction.ScopeVariableKey,
            Once = reaction.Once,
            When = reaction.When,
            Effects = reaction.Effects.Select(ToRes).ToArray()
        };

    private static StoryGraphEffectRes ToRes(StoryEffect effect)
        => new() {
            Id = effect.Id,
            Kind = effect.Kind,
            SortOrder = effect.SortOrder,
            VariableKey = effect.VariableKey,
            Value = effect.Value,
            TargetNodeKey = effect.TargetNodeKey,
            InvokeActionKey = effect.InvokeActionKey
        };
}
