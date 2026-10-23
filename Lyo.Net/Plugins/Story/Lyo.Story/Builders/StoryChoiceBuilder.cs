using Lyo.Exceptions;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Enums;
using Lyo.Story.Enums;

namespace Lyo.Story.Builders;

/// <summary>Builds a <see cref="StoryChoice" /> with All/OneOf conditions and a <c>Do</c> response list.</summary>
public sealed class StoryChoiceBuilder
{
    private readonly StoryChoice _choice;
    private readonly StoryResponseBuilder _responses = new();
    private WhereClause? _enabledWhen;
    private bool _usedDo;
    private WhereClause? _visibleWhen;

    internal StoryChoiceBuilder(string key, string? prompt)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        _choice = new() { Key = key, Prompt = prompt };
    }

    /// <summary>Enabled-to-take: every nested leaf must match. Further <see cref="WhenAll" /> calls AND onto the tree.</summary>
    public StoryChoiceBuilder WhenAll(Action<StoryConditionBuilder> configure) => MergeEnabled(GroupOperatorEnum.And, configure);

    /// <summary>Enabled-to-take: at least one nested leaf must match. Combined with existing conditions via AND.</summary>
    public StoryChoiceBuilder WhenOneOf(Action<StoryConditionBuilder> configure) => MergeEnabled(GroupOperatorEnum.Or, configure);

    /// <summary>Inverts the current enabled tree (De Morgan). No-op when there is no tree yet.</summary>
    public StoryChoiceBuilder WhenNot()
    {
        _enabledWhen = StoryWhere.Negate(_enabledWhen);
        return this;
    }

    /// <summary>Visibility: every nested leaf must match.</summary>
    public StoryChoiceBuilder VisibleWhenAll(Action<StoryConditionBuilder> configure) => MergeVisible(GroupOperatorEnum.And, configure);

    /// <summary>Visibility: at least one nested leaf must match.</summary>
    public StoryChoiceBuilder VisibleWhenOneOf(Action<StoryConditionBuilder> configure) => MergeVisible(GroupOperatorEnum.Or, configure);

    /// <summary>Appends response steps. Prefer this when there is more than one step.</summary>
    public StoryChoiceBuilder Do(Action<StoryResponseBuilder> configure)
    {
        ArgumentHelpers.ThrowIfNull(configure);
        _usedDo = true;
        configure(_responses);
        return this;
    }

    /// <summary>Sugar that appends a Set step. Same list as <see cref="Do" />.</summary>
    public StoryChoiceBuilder Set(string key, object? value)
    {
        _responses.Set(key, value);
        return this;
    }

    /// <summary>Sugar that appends a Goto step. Same list as <see cref="Do" />.</summary>
    public StoryChoiceBuilder GoesTo(string nodeKey)
    {
        _responses.GoesTo(nodeKey);
        return this;
    }

    /// <summary>Sugar that appends an Invoke step.</summary>
    public StoryChoiceBuilder Invoke(string actionKey)
    {
        _responses.Invoke(actionKey);
        return this;
    }

    internal StoryChoice Build(int sortOrder)
    {
        _choice.SortOrder = sortOrder;
        _choice.EnabledWhen = _enabledWhen;
        _choice.VisibleWhen = _visibleWhen;
        _choice.Effects = _responses.Build();
        var gotoKey = _choice.Effects.LastOrDefault(e => e.Kind == StoryEffectKind.GotoNode)?.TargetNodeKey;
        _choice.TargetNodeKey = gotoKey;
        _ = _usedDo;
        return _choice;
    }

    private StoryChoiceBuilder MergeEnabled(GroupOperatorEnum op, Action<StoryConditionBuilder> configure)
    {
        ArgumentHelpers.ThrowIfNull(configure);
        var builder = new StoryConditionBuilder(op);
        configure(builder);
        var clause = builder.Build();
        _enabledWhen = StoryWhere.And(_enabledWhen, clause);
        return this;
    }

    private StoryChoiceBuilder MergeVisible(GroupOperatorEnum op, Action<StoryConditionBuilder> configure)
    {
        ArgumentHelpers.ThrowIfNull(configure);
        var builder = new StoryConditionBuilder(op);
        configure(builder);
        var clause = builder.Build();
        _visibleWhen = StoryWhere.And(_visibleWhen, clause);
        return this;
    }
}