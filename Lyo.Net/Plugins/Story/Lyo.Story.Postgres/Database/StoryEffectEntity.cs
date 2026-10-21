using Lyo.Story.Enums;

namespace Lyo.Story.Postgres.Database;

public sealed class StoryEffectEntity
{
    public Guid Id { get; set; }

    public Guid? ChoiceId { get; set; }

    public Guid? ActionId { get; set; }

    public Guid? ReactionId { get; set; }

    public int SortOrder { get; set; }

    public StoryEffectKind Kind { get; set; }

    public string? VariableKey { get; set; }

    public string? Value { get; set; }

    public Guid? TargetNodeId { get; set; }

    public Guid? InvokeActionId { get; set; }

    public StoryChoiceEntity? Choice { get; set; }

    public StoryActionEntity? Action { get; set; }

    public StoryReactionEntity? Reaction { get; set; }

    public StoryNodeEntity? TargetNode { get; set; }

    public StoryActionEntity? InvokeAction { get; set; }
}