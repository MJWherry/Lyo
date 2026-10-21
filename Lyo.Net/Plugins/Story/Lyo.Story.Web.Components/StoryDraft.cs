using Lyo.Exceptions;

namespace Lyo.Story.Web.Components;

/// <summary>Authoring copy of a play graph. Keys are the identity the builder and the graph routes share.</summary>
public sealed class StoryDraft
{
    /// <summary>Existing id. Empty creates a story on save.</summary>
    public Guid Id { get; set; }

    /// <summary>Display title.</summary>
    public string Title { get; set; } = "";

    /// <summary>URL slug.</summary>
    public string Slug { get; set; } = "";

    /// <summary>Tenant that owns the story, or null.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Game the story belongs to, or null.</summary>
    public string? GameKey { get; set; }

    /// <summary>Optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Optional BCP 47 language tag.</summary>
    public string? Language { get; set; }

    /// <summary>Authoring status.</summary>
    public StoryStatus Status { get; set; } = StoryStatus.Draft;

    /// <summary>Key of the node play starts on.</summary>
    public string? StartNodeKey { get; set; }

    /// <summary>Optional cover image reference.</summary>
    public string? CoverImageRef { get; set; }

    /// <summary>Optional scene groups.</summary>
    public List<StorySceneDraft> Scenes { get; set; } = [];

    /// <summary>Nodes, including their choices.</summary>
    public List<StoryNodeDraft> Nodes { get; set; } = [];

    /// <summary>Declared variables.</summary>
    public List<StoryVariableDraft> Variables { get; set; } = [];

    /// <summary>Named actions.</summary>
    public List<StoryActionDraft> Actions { get; set; } = [];

    /// <summary>Ambient reactions.</summary>
    public List<StoryReactionDraft> Reactions { get; set; } = [];
}

/// <summary>Optional scene group on a <see cref="StoryDraft" />.</summary>
public sealed class StorySceneDraft
{
    /// <summary>Stable key within the story.</summary>
    public string Key { get; set; } = "";

    /// <summary>Optional title.</summary>
    public string? Title { get; set; }

    /// <summary>Sort among scenes.</summary>
    public int SortOrder { get; set; }
}

/// <summary>One beat on a <see cref="StoryDraft" />.</summary>
public sealed class StoryNodeDraft
{
    /// <summary>Stable key within the story.</summary>
    public string Key { get; set; } = "";

    /// <summary>Optional title.</summary>
    public string? Title { get; set; }

    /// <summary>Scene key. Null when the node is ungrouped.</summary>
    public string? SceneKey { get; set; }

    /// <summary>True when this node ends the playthrough.</summary>
    public bool IsEnding { get; set; }

    /// <summary>How the node is shown.</summary>
    public StoryPresentationKind PresentationKind { get; set; }

    /// <summary>Text body.</summary>
    public string? Body { get; set; }

    /// <summary>MIME token for <see cref="Body" />.</summary>
    public string? ContentType { get; set; }

    /// <summary>Cutscene media reference.</summary>
    public string? MediaRef { get; set; }

    /// <summary>Choices on this node.</summary>
    public List<StoryChoiceDraft> Choices { get; set; } = [];
}

/// <summary>One player choice on a <see cref="StoryNodeDraft" />.</summary>
public sealed class StoryChoiceDraft
{
    /// <summary>Stable key on the node.</summary>
    public string Key { get; set; } = "";

    /// <summary>Player-facing prompt.</summary>
    public string? Prompt { get; set; }

    /// <summary>Sort among choices.</summary>
    public int SortOrder { get; set; }

    /// <summary>Node key written from the last <see cref="StoryEffectKind.GotoNode" /> step.</summary>
    public string? TargetNodeKey { get; set; }

    /// <summary>Visibility condition. Null means always visible.</summary>
    public WhereClause? VisibleWhen { get; set; }

    /// <summary>Enablement condition. Null means always enabled when visible.</summary>
    public WhereClause? EnabledWhen { get; set; }

    /// <summary>Response steps.</summary>
    public List<StoryEffectDraft> Effects { get; set; } = [];
}

/// <summary>One response step on a choice, action, or reaction.</summary>
public sealed class StoryEffectDraft
{
    /// <summary>What the step does.</summary>
    public StoryEffectKind Kind { get; set; }

    /// <summary>Order within the parent.</summary>
    public int SortOrder { get; set; }

    /// <summary>Variable to set.</summary>
    public string? VariableKey { get; set; }

    /// <summary>Value written.</summary>
    public string? Value { get; set; }

    /// <summary>Node key to move to.</summary>
    public string? TargetNodeKey { get; set; }

    /// <summary>Action key to invoke.</summary>
    public string? InvokeActionKey { get; set; }
}

/// <summary>Declared variable on a <see cref="StoryDraft" />.</summary>
public sealed class StoryVariableDraft
{
    /// <summary>Stable key.</summary>
    public string Key { get; set; } = "";

    /// <summary>Optional display name.</summary>
    public string? Name { get; set; }

    /// <summary>Value kind.</summary>
    public StoryVariableType Type { get; set; } = StoryVariableType.Boolean;

    /// <summary>Default written into the play bag at start.</summary>
    public string? DefaultValue { get; set; }

    /// <summary>Optional description.</summary>
    public string? Description { get; set; }
}

/// <summary>Named action on a <see cref="StoryDraft" />.</summary>
public sealed class StoryActionDraft
{
    /// <summary>Stable key.</summary>
    public string Key { get; set; } = "";

    /// <summary>Optional display name.</summary>
    public string? Name { get; set; }

    /// <summary>Response steps.</summary>
    public List<StoryEffectDraft> Effects { get; set; } = [];
}

/// <summary>Ambient reaction on a <see cref="StoryDraft" />.</summary>
public sealed class StoryReactionDraft
{
    /// <summary>Stable key.</summary>
    public string Key { get; set; } = "";

    /// <summary>When the reaction runs.</summary>
    public StoryTrigger Trigger { get; set; }

    /// <summary>Node key scope. Null means the whole story.</summary>
    public string? ScopeNodeKey { get; set; }

    /// <summary>Variable the trigger watches.</summary>
    public string? ScopeVariableKey { get; set; }

    /// <summary>When true, the reaction fires at most once per playthrough.</summary>
    public bool Once { get; set; }

    /// <summary>Condition. Null means always.</summary>
    public WhereClause? When { get; set; }

    /// <summary>Response steps.</summary>
    public List<StoryEffectDraft> Effects { get; set; } = [];
}

/// <summary>Maps a <see cref="StoryDraft" /> to and from the graph request and response.</summary>
public static class StoryDraftMap
{
    /// <summary>Copies <paramref name="graph" /> into a draft the builder can edit.</summary>
    public static StoryDraft FromGraph(StoryGraphRes graph)
    {
        ArgumentHelpers.ThrowIfNull(graph);
        return new() {
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
            Scenes = graph.Scenes.Select(scene => new StorySceneDraft { Key = scene.Key, Title = scene.Title, SortOrder = scene.SortOrder }).ToList(),
            Nodes = graph.Nodes.Select(FromNode).ToList(),
            Variables = graph.Variables.Select(variable => new StoryVariableDraft {
                    Key = variable.Key,
                    Name = variable.Name,
                    Type = variable.Type,
                    DefaultValue = variable.DefaultValue,
                    Description = variable.Description
                })
                .ToList(),
            Actions = graph.Actions.Select(action => new StoryActionDraft { Key = action.Key, Name = action.Name, Effects = action.Effects.Select(FromEffect).ToList() }).ToList(),
            Reactions = graph.Reactions.Select(FromReaction).ToList()
        };
    }

    /// <summary>Builds the save body. Each choice's <see cref="StoryChoiceDraft.TargetNodeKey" /> is the last goto step.</summary>
    public static StoryGraphReq ToRequest(StoryDraft draft)
    {
        ArgumentHelpers.ThrowIfNull(draft);
        return new() {
            Id = draft.Id,
            Title = draft.Title,
            Slug = draft.Slug,
            TenantId = draft.TenantId,
            GameKey = draft.GameKey,
            Description = draft.Description,
            Language = draft.Language,
            Status = draft.Status,
            StartNodeKey = draft.StartNodeKey,
            CoverImageRef = draft.CoverImageRef,
            Scenes = draft.Scenes.Select(scene => new StoryGraphSceneReq { Key = scene.Key, Title = scene.Title, SortOrder = scene.SortOrder }).ToList(),
            Nodes = draft.Nodes.Select(ToNode).ToList(),
            Variables = draft.Variables.Select(variable => new StoryGraphVariableReq {
                    Key = variable.Key,
                    Name = variable.Name,
                    Type = variable.Type,
                    DefaultValue = variable.DefaultValue,
                    Description = variable.Description
                })
                .ToList(),
            Actions = draft.Actions.Select(action => new StoryGraphActionReq { Key = action.Key, Name = action.Name, Effects = ToEffects(action.Effects) }).ToList(),
            Reactions = draft.Reactions.Select(ToReaction).ToList()
        };
    }

    /// <summary>Builds a draft from <paramref name="request" />. Used when the JSON tab is applied.</summary>
    public static StoryDraft FromRequest(StoryGraphReq request)
    {
        ArgumentHelpers.ThrowIfNull(request);
        return new() {
            Id = request.Id,
            Title = request.Title,
            Slug = request.Slug,
            TenantId = request.TenantId,
            GameKey = request.GameKey,
            Description = request.Description,
            Language = request.Language,
            Status = request.Status,
            StartNodeKey = request.StartNodeKey,
            CoverImageRef = request.CoverImageRef,
            Scenes = request.Scenes.Select(scene => new StorySceneDraft { Key = scene.Key, Title = scene.Title, SortOrder = scene.SortOrder }).ToList(),
            Nodes = request.Nodes.Select(node => new StoryNodeDraft {
                    Key = node.Key,
                    Title = node.Title,
                    SceneKey = node.SceneKey,
                    IsEnding = node.IsEnding,
                    PresentationKind = node.PresentationKind,
                    Body = node.Body,
                    ContentType = node.ContentType,
                    MediaRef = node.MediaRef,
                    Choices = node.Choices.Select(choice => new StoryChoiceDraft {
                            Key = choice.Key,
                            Prompt = choice.Prompt,
                            SortOrder = choice.SortOrder,
                            TargetNodeKey = LastGoto(choice.Effects.Select(FromEffectReq)) ?? choice.TargetNodeKey,
                            VisibleWhen = choice.VisibleWhen,
                            EnabledWhen = choice.EnabledWhen,
                            Effects = choice.Effects.Select(effect => new StoryEffectDraft {
                                    Kind = effect.Kind,
                                    SortOrder = effect.SortOrder,
                                    VariableKey = effect.VariableKey,
                                    Value = effect.Value,
                                    TargetNodeKey = effect.TargetNodeKey,
                                    InvokeActionKey = effect.InvokeActionKey
                                })
                                .ToList()
                        })
                        .ToList()
                })
                .ToList(),
            Variables = request.Variables.Select(variable => new StoryVariableDraft {
                    Key = variable.Key,
                    Name = variable.Name,
                    Type = variable.Type,
                    DefaultValue = variable.DefaultValue,
                    Description = variable.Description
                })
                .ToList(),
            Actions = request.Actions.Select(action => new StoryActionDraft { Key = action.Key, Name = action.Name, Effects = action.Effects.Select(FromEffectReq).ToList() })
                .ToList(),
            Reactions = request.Reactions.Select(reaction => new StoryReactionDraft {
                    Key = reaction.Key,
                    Trigger = reaction.Trigger,
                    ScopeNodeKey = reaction.ScopeNodeKey,
                    ScopeVariableKey = reaction.ScopeVariableKey,
                    Once = reaction.Once,
                    When = reaction.When,
                    Effects = reaction.Effects.Select(FromEffectReq).ToList()
                })
                .ToList()
        };
    }

    /// <summary>The node key of the last goto step, or null when the list has none.</summary>
    internal static string? LastGoto(IEnumerable<StoryEffectDraft> effects)
        => effects.LastOrDefault(effect => effect.Kind == StoryEffectKind.GotoNode && !string.IsNullOrWhiteSpace(effect.TargetNodeKey))?.TargetNodeKey;

    private static StoryNodeDraft FromNode(StoryGraphNodeRes node)
        => new() {
            Key = node.Key,
            Title = node.Title,
            SceneKey = node.SceneKey,
            IsEnding = node.IsEnding,
            PresentationKind = node.PresentationKind,
            Body = node.Body,
            ContentType = node.ContentType,
            MediaRef = node.MediaRef,
            Choices = node.Choices.Select(choice => new StoryChoiceDraft {
                    Key = choice.Key,
                    Prompt = choice.Prompt,
                    SortOrder = choice.SortOrder,
                    TargetNodeKey = choice.TargetNodeKey,
                    VisibleWhen = choice.VisibleWhen,
                    EnabledWhen = choice.EnabledWhen,
                    Effects = choice.Effects.Select(FromEffect).ToList()
                })
                .ToList()
        };

    private static StoryReactionDraft FromReaction(StoryGraphReactionRes reaction)
        => new() {
            Key = reaction.Key,
            Trigger = reaction.Trigger,
            ScopeNodeKey = reaction.ScopeNodeKey,
            ScopeVariableKey = reaction.ScopeVariableKey,
            Once = reaction.Once,
            When = reaction.When,
            Effects = reaction.Effects.Select(FromEffect).ToList()
        };

    private static StoryEffectDraft FromEffect(StoryGraphEffectRes effect)
        => new() {
            Kind = effect.Kind,
            SortOrder = effect.SortOrder,
            VariableKey = effect.VariableKey,
            Value = effect.Value,
            TargetNodeKey = effect.TargetNodeKey,
            InvokeActionKey = effect.InvokeActionKey
        };

    private static StoryEffectDraft FromEffectReq(StoryGraphEffectReq effect)
        => new() {
            Kind = effect.Kind,
            SortOrder = effect.SortOrder,
            VariableKey = effect.VariableKey,
            Value = effect.Value,
            TargetNodeKey = effect.TargetNodeKey,
            InvokeActionKey = effect.InvokeActionKey
        };

    private static StoryGraphNodeReq ToNode(StoryNodeDraft node)
        => new() {
            Key = node.Key,
            Title = node.Title,
            SceneKey = node.SceneKey,
            IsEnding = node.IsEnding,
            PresentationKind = node.PresentationKind,
            Body = node.Body,
            ContentType = node.ContentType,
            MediaRef = node.MediaRef,
            Choices = node.Choices.Select((choice, index) => new StoryGraphChoiceReq {
                    Key = choice.Key,
                    Prompt = choice.Prompt,
                    SortOrder = index,
                    TargetNodeKey = LastGoto(choice.Effects),
                    VisibleWhen = choice.VisibleWhen,
                    EnabledWhen = choice.EnabledWhen,
                    Effects = ToEffects(choice.Effects)
                })
                .ToList()
        };

    private static StoryGraphReactionReq ToReaction(StoryReactionDraft reaction)
        => new() {
            Key = reaction.Key,
            Trigger = reaction.Trigger,
            ScopeNodeKey = reaction.ScopeNodeKey,
            ScopeVariableKey = reaction.ScopeVariableKey,
            Once = reaction.Once,
            When = reaction.When,
            Effects = ToEffects(reaction.Effects)
        };

    private static List<StoryGraphEffectReq> ToEffects(List<StoryEffectDraft> effects)
        => effects.Select((effect, index) => new StoryGraphEffectReq {
                Kind = effect.Kind,
                SortOrder = index,
                VariableKey = effect.VariableKey,
                Value = effect.Value,
                TargetNodeKey = effect.TargetNodeKey,
                InvokeActionKey = effect.InvokeActionKey
            })
            .ToList();
}