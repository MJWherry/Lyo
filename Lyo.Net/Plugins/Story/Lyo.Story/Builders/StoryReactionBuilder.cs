using Lyo.Exceptions;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Enums;
using Lyo.Story.Enums;

namespace Lyo.Story.Builders;

/// <summary>Builds a <see cref="StoryReaction"/> rule.</summary>
public sealed class StoryReactionBuilder
{
    private readonly StoryReaction _reaction;
    private readonly StoryResponseBuilder _responses = new();
    private WhereClause? _when;

    internal StoryReactionBuilder(string key)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        _reaction = new() { Key = key };
    }

    /// <summary>Trigger for this rule. Optional <paramref name="scopeKey"/> is a node key or variable key depending on the trigger.</summary>
    public StoryReactionBuilder On(StoryTrigger trigger, string? scopeKey = null)
    {
        _reaction.Trigger = trigger;
        if (trigger == StoryTrigger.VariableChanged)
            _reaction.ScopeVariableKey = scopeKey;
        else if (trigger is StoryTrigger.EnterNode or StoryTrigger.LeaveNode or StoryTrigger.Advanced or StoryTrigger.ChoiceTaken)
            _reaction.ScopeNodeKey = scopeKey;
        return this;
    }

    /// <summary>Fire at most once per playthrough.</summary>
    public StoryReactionBuilder Once()
    {
        _reaction.Once = true;
        return this;
    }

    /// <summary>Condition: every nested leaf must match.</summary>
    public StoryReactionBuilder WhenAll(Action<StoryConditionBuilder> configure)
    {
        ArgumentHelpers.ThrowIfNull(configure);
        var builder = new StoryConditionBuilder(GroupOperatorEnum.And);
        configure(builder);
        _when = StoryWhere.And(_when, builder.Build());
        return this;
    }

    /// <summary>Condition: at least one nested leaf must match.</summary>
    public StoryReactionBuilder WhenOneOf(Action<StoryConditionBuilder> configure)
    {
        ArgumentHelpers.ThrowIfNull(configure);
        var builder = new StoryConditionBuilder(GroupOperatorEnum.Or);
        configure(builder);
        _when = StoryWhere.And(_when, builder.Build());
        return this;
    }

    /// <summary>Response steps.</summary>
    public StoryReactionBuilder Do(Action<StoryResponseBuilder> configure)
    {
        ArgumentHelpers.ThrowIfNull(configure);
        configure(_responses);
        return this;
    }

    /// <summary>Sugar for a single Goto response.</summary>
    public StoryReactionBuilder Goto(string nodeKey)
    {
        _responses.GoesTo(nodeKey);
        return this;
    }

    internal StoryReaction Build()
    {
        _reaction.When = _when;
        _reaction.Effects = _responses.Build();
        return _reaction;
    }
}
