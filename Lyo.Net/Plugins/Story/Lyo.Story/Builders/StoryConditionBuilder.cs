using Lyo.Exceptions;
using Lyo.Query.Models.Builders;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Enums;

namespace Lyo.Story.Builders;

/// <summary>Fluent leaves for a story condition group (All or OneOf).</summary>
public sealed class StoryConditionBuilder
{
    private readonly WhereClauseBuilder _inner;
    private int _count;

    internal StoryConditionBuilder(GroupOperatorEnum op) => _inner = op == GroupOperatorEnum.Or ? WhereClauseBuilder.Or() : WhereClauseBuilder.And();

    /// <summary>Variable <paramref name="key" /> is true.</summary>
    public StoryConditionBuilder Flag(string key)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        _inner.Equals(StoryConditionFields.Var(key), true);
        _count++;
        return this;
    }

    /// <summary>Variable <paramref name="key" /> is missing or false.</summary>
    public StoryConditionBuilder NotFlag(string key)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        _inner.Equals(StoryConditionFields.Var(key), false);
        _count++;
        return this;
    }

    /// <summary>Variable <paramref name="key" /> equals <paramref name="value" />.</summary>
    public StoryConditionBuilder Eq(string key, object? value)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        _inner.Equals(StoryConditionFields.Var(key), value);
        _count++;
        return this;
    }

    /// <summary>The playthrough has taken choice <paramref name="choiceKey" />.</summary>
    public StoryConditionBuilder Chose(string choiceKey)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(choiceKey);
        _inner.Equals(StoryConditionFields.Chose(choiceKey), true);
        _count++;
        return this;
    }

    /// <summary>The playthrough has entered node <paramref name="nodeKey" />.</summary>
    public StoryConditionBuilder Visited(string nodeKey)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(nodeKey);
        _inner.Equals(StoryConditionFields.Visited(nodeKey), true);
        _count++;
        return this;
    }

    internal WhereClause? Build() => _count == 0 ? null : _inner.Build();
}