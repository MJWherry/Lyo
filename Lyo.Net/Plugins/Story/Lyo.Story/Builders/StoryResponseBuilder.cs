using Lyo.Exceptions;
using Lyo.Story.Enums;

namespace Lyo.Story.Builders;

/// <summary>Ordered response list shared by choices, actions, and reactions.</summary>
public sealed class StoryResponseBuilder
{
    private readonly List<StoryEffect> _effects = [];

    /// <summary>Sets a variable to <paramref name="value"/>.</summary>
    public StoryResponseBuilder Set(string key, object? value)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        Add(StoryEffectKind.Set, key, value is null ? null : Convert.ToString(value), targetNodeKey: null, invokeActionKey: null);
        return this;
    }

    /// <summary>Removes a variable from the bag (reverts to undeclared / false).</summary>
    public StoryResponseBuilder Unset(string key)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        Add(StoryEffectKind.Unset, key, null, null, null);
        return this;
    }

    /// <summary>Adds <paramref name="by"/> to an integer variable (default 1).</summary>
    public StoryResponseBuilder Increment(string key, int by = 1)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        Add(StoryEffectKind.Increment, key, by.ToString(), null, null);
        return this;
    }

    /// <summary>Moves the play cursor to <paramref name="nodeKey"/>.</summary>
    public StoryResponseBuilder GoesTo(string nodeKey)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(nodeKey);
        Add(StoryEffectKind.GotoNode, null, null, nodeKey, null);
        return this;
    }

    /// <summary>Completes the playthrough.</summary>
    public StoryResponseBuilder End()
    {
        Add(StoryEffectKind.EndPlaythrough, null, null, null, null);
        return this;
    }

    /// <summary>Runs the named action's responses.</summary>
    public StoryResponseBuilder Invoke(string actionKey)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(actionKey);
        Add(StoryEffectKind.InvokeAction, null, null, null, actionKey);
        return this;
    }

    internal IReadOnlyList<StoryEffect> Build() => _effects.ToArray();

    private void Add(StoryEffectKind kind, string? variableKey, string? value, string? targetNodeKey, string? invokeActionKey)
    {
        _effects.Add(
            new StoryEffect {
                Kind = kind,
                SortOrder = _effects.Count,
                VariableKey = variableKey,
                Value = value,
                TargetNodeKey = targetNodeKey,
                InvokeActionKey = invokeActionKey
            });
    }
}
