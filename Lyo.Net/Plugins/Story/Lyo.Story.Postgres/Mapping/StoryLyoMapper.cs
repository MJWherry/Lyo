using Lyo.Api.Mapping;
using Lyo.Story.Models.Request;
using Lyo.Story.Models.Response;
using Lyo.Story.Postgres.Database;

namespace Lyo.Story.Postgres.Mapping;

/// <summary>Hand-rolled <see cref="ILyoMapper" /> for Story Req/entity/Res. Explicit property assignments.</summary>
public sealed class StoryLyoMapper : ILyoMapper
{
    /// <inheritdoc />
    public TResult Map<TResult>(object source)
        => source switch {
            StoryReq req when typeof(TResult) == typeof(StoryEntity) => (TResult)(object)ReqToNew(req),
            StorySceneReq req when typeof(TResult) == typeof(StorySceneEntity) => (TResult)(object)ReqToNew(req),
            StoryNodeReq req when typeof(TResult) == typeof(StoryNodeEntity) => (TResult)(object)ReqToNew(req),
            StoryVariableReq req when typeof(TResult) == typeof(StoryVariableEntity) => (TResult)(object)ReqToNew(req),
            StoryActionReq req when typeof(TResult) == typeof(StoryActionEntity) => (TResult)(object)ReqToNew(req),
            StoryChoiceReq req when typeof(TResult) == typeof(StoryChoiceEntity) => (TResult)(object)ReqToNew(req),
            StoryReactionReq req when typeof(TResult) == typeof(StoryReactionEntity) => (TResult)(object)ReqToNew(req),
            StoryEffectReq req when typeof(TResult) == typeof(StoryEffectEntity) => (TResult)(object)ReqToNew(req),
            StoryEntity e when typeof(TResult) == typeof(StoryRes) => (TResult)(object)ToRes(e),
            StorySceneEntity e when typeof(TResult) == typeof(StorySceneRes) => (TResult)(object)ToRes(e),
            StoryNodeEntity e when typeof(TResult) == typeof(StoryNodeRes) => (TResult)(object)ToRes(e),
            StoryVariableEntity e when typeof(TResult) == typeof(StoryVariableRes) => (TResult)(object)ToRes(e),
            StoryActionEntity e when typeof(TResult) == typeof(StoryActionRes) => (TResult)(object)ToRes(e),
            StoryChoiceEntity e when typeof(TResult) == typeof(StoryChoiceRes) => (TResult)(object)ToRes(e),
            StoryReactionEntity e when typeof(TResult) == typeof(StoryReactionRes) => (TResult)(object)ToRes(e),
            StoryEffectEntity e when typeof(TResult) == typeof(StoryEffectRes) => (TResult)(object)ToRes(e),
            StoryPlaythroughEntity e when typeof(TResult) == typeof(StoryPlaythroughRes) => (TResult)(object)ToRes(e),
            var _ => throw Unmapped(source.GetType(), typeof(TResult))
        };

    /// <inheritdoc />
    public void Map<TSource, TDest>(TSource source, TDest destination)
    {
        switch (source, destination) {
            case (StoryReq req, StoryEntity e):
                Apply(req, e);
                break;
            case (StorySceneReq req, StorySceneEntity e):
                Apply(req, e);
                break;
            case (StoryNodeReq req, StoryNodeEntity e):
                Apply(req, e);
                break;
            case (StoryVariableReq req, StoryVariableEntity e):
                Apply(req, e);
                break;
            case (StoryActionReq req, StoryActionEntity e):
                Apply(req, e);
                break;
            case (StoryChoiceReq req, StoryChoiceEntity e):
                Apply(req, e);
                break;
            case (StoryReactionReq req, StoryReactionEntity e):
                Apply(req, e);
                break;
            case (StoryEffectReq req, StoryEffectEntity e):
                Apply(req, e);
                break;
            default:
                throw Unmapped(typeof(TSource), typeof(TDest));
        }
    }

    private static InvalidOperationException Unmapped(Type source, Type dest)
        => new($"No mapping configured from {source.Name} to {dest.Name}.");

    private static StoryEntity ReqToNew(StoryReq req)
    {
        var dest = new StoryEntity { CreatedTimestamp = DateTime.UtcNow };
        Apply(req, dest);
        return dest;
    }

    private static StorySceneEntity ReqToNew(StorySceneReq req)
    {
        var dest = new StorySceneEntity();
        Apply(req, dest);
        return dest;
    }

    private static StoryNodeEntity ReqToNew(StoryNodeReq req)
    {
        var dest = new StoryNodeEntity();
        Apply(req, dest);
        return dest;
    }

    private static StoryVariableEntity ReqToNew(StoryVariableReq req)
    {
        var dest = new StoryVariableEntity();
        Apply(req, dest);
        return dest;
    }

    private static StoryActionEntity ReqToNew(StoryActionReq req)
    {
        var dest = new StoryActionEntity();
        Apply(req, dest);
        return dest;
    }

    private static StoryChoiceEntity ReqToNew(StoryChoiceReq req)
    {
        var dest = new StoryChoiceEntity();
        Apply(req, dest);
        return dest;
    }

    private static StoryReactionEntity ReqToNew(StoryReactionReq req)
    {
        var dest = new StoryReactionEntity();
        Apply(req, dest);
        return dest;
    }

    private static StoryEffectEntity ReqToNew(StoryEffectReq req)
    {
        var dest = new StoryEffectEntity();
        Apply(req, dest);
        return dest;
    }

    private static void Apply(StoryReq req, StoryEntity dest)
    {
        dest.Title = req.Title;
        dest.Slug = req.Slug;
        dest.Description = req.Description;
        dest.Language = req.Language;
        dest.Status = req.Status;
        dest.StartNodeId = req.StartNodeId;
        dest.CoverImageRef = req.CoverImageRef;
        dest.UpdatedTimestamp = DateTime.UtcNow;
    }

    private static void Apply(StorySceneReq req, StorySceneEntity dest)
    {
        dest.StoryId = req.StoryId;
        dest.Key = req.Key;
        dest.Title = req.Title;
        dest.SortOrder = req.SortOrder;
    }

    private static void Apply(StoryNodeReq req, StoryNodeEntity dest)
    {
        dest.StoryId = req.StoryId;
        dest.SceneId = req.SceneId;
        dest.Key = req.Key;
        dest.Title = req.Title;
        dest.IsEnding = req.IsEnding;
        dest.PresentationKind = req.PresentationKind;
        dest.Body = req.Body;
        dest.ContentType = req.ContentType;
        dest.MediaRef = req.MediaRef;
    }

    private static void Apply(StoryVariableReq req, StoryVariableEntity dest)
    {
        dest.StoryId = req.StoryId;
        dest.Key = req.Key;
        dest.Name = req.Name;
        dest.Type = req.Type;
        dest.DefaultValue = req.DefaultValue;
        dest.Description = req.Description;
    }

    private static void Apply(StoryActionReq req, StoryActionEntity dest)
    {
        dest.StoryId = req.StoryId;
        dest.Key = req.Key;
        dest.Name = req.Name;
    }

    private static void Apply(StoryChoiceReq req, StoryChoiceEntity dest)
    {
        dest.NodeId = req.NodeId;
        dest.Key = req.Key;
        dest.Prompt = req.Prompt;
        dest.SortOrder = req.SortOrder;
        dest.TargetNodeId = req.TargetNodeId;
        dest.VisibleWhenJson = req.VisibleWhenJson;
        dest.EnabledWhenJson = req.EnabledWhenJson;
    }

    private static void Apply(StoryReactionReq req, StoryReactionEntity dest)
    {
        dest.StoryId = req.StoryId;
        dest.Key = req.Key;
        dest.Trigger = req.Trigger;
        dest.ScopeNodeId = req.ScopeNodeId;
        dest.ScopeVariableKey = req.ScopeVariableKey;
        dest.Once = req.Once;
        dest.WhenPredicateJson = req.WhenPredicateJson;
    }

    private static void Apply(StoryEffectReq req, StoryEffectEntity dest)
    {
        dest.ChoiceId = req.ChoiceId;
        dest.ActionId = req.ActionId;
        dest.ReactionId = req.ReactionId;
        dest.SortOrder = req.SortOrder;
        dest.Kind = req.Kind;
        dest.VariableKey = req.VariableKey;
        dest.Value = req.Value;
        dest.TargetNodeId = req.TargetNodeId;
        dest.InvokeActionId = req.InvokeActionId;
    }

    private static StoryRes ToRes(StoryEntity e)
        => new() {
            Id = e.Id,
            Title = e.Title,
            Slug = e.Slug,
            Description = e.Description,
            Language = e.Language,
            Status = e.Status,
            StartNodeId = e.StartNodeId,
            CoverImageRef = e.CoverImageRef,
            CreatedTimestamp = e.CreatedTimestamp,
            UpdatedTimestamp = e.UpdatedTimestamp
        };

    private static StorySceneRes ToRes(StorySceneEntity e)
        => new() { Id = e.Id, StoryId = e.StoryId, Key = e.Key, Title = e.Title, SortOrder = e.SortOrder };

    private static StoryNodeRes ToRes(StoryNodeEntity e)
        => new() {
            Id = e.Id,
            StoryId = e.StoryId,
            SceneId = e.SceneId,
            Key = e.Key,
            Title = e.Title,
            IsEnding = e.IsEnding,
            PresentationKind = e.PresentationKind,
            Body = e.Body,
            ContentType = e.ContentType,
            MediaRef = e.MediaRef
        };

    private static StoryVariableRes ToRes(StoryVariableEntity e)
        => new() {
            Id = e.Id,
            StoryId = e.StoryId,
            Key = e.Key,
            Name = e.Name,
            Type = e.Type,
            DefaultValue = e.DefaultValue,
            Description = e.Description
        };

    private static StoryActionRes ToRes(StoryActionEntity e)
        => new() { Id = e.Id, StoryId = e.StoryId, Key = e.Key, Name = e.Name };

    private static StoryChoiceRes ToRes(StoryChoiceEntity e)
        => new() {
            Id = e.Id,
            NodeId = e.NodeId,
            Key = e.Key,
            Prompt = e.Prompt,
            SortOrder = e.SortOrder,
            TargetNodeId = e.TargetNodeId,
            VisibleWhenJson = e.VisibleWhenJson,
            EnabledWhenJson = e.EnabledWhenJson
        };

    private static StoryReactionRes ToRes(StoryReactionEntity e)
        => new() {
            Id = e.Id,
            StoryId = e.StoryId,
            Key = e.Key,
            Trigger = e.Trigger,
            ScopeNodeId = e.ScopeNodeId,
            ScopeVariableKey = e.ScopeVariableKey,
            Once = e.Once,
            WhenPredicateJson = e.WhenPredicateJson
        };

    private static StoryEffectRes ToRes(StoryEffectEntity e)
        => new() {
            Id = e.Id,
            ChoiceId = e.ChoiceId,
            ActionId = e.ActionId,
            ReactionId = e.ReactionId,
            SortOrder = e.SortOrder,
            Kind = e.Kind,
            VariableKey = e.VariableKey,
            Value = e.Value,
            TargetNodeId = e.TargetNodeId,
            InvokeActionId = e.InvokeActionId
        };

    private static StoryPlaythroughRes ToRes(StoryPlaythroughEntity e)
        => new() {
            Id = e.Id,
            StoryId = e.StoryId,
            PlayerRef = e.PlayerRef,
            Status = e.Status,
            CurrentNodeId = e.CurrentNodeId,
            StartedAt = e.StartedAt,
            UpdatedAt = e.UpdatedAt,
            CompletedAt = e.CompletedAt
        };
}
