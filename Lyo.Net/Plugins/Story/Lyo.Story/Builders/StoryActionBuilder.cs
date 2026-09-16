using Lyo.Exceptions;

namespace Lyo.Story.Builders;

/// <summary>Builds a named <see cref="StoryAction"/>.</summary>
public sealed class StoryActionBuilder
{
    private readonly StoryAction _action;
    private readonly StoryResponseBuilder _responses = new();

    internal StoryActionBuilder(string key)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        _action = new() { Key = key };
    }

    /// <summary>Display name.</summary>
    public StoryActionBuilder Named(string name)
    {
        _action.Name = name;
        return this;
    }

    /// <summary>Response steps for this action.</summary>
    public StoryActionBuilder Do(Action<StoryResponseBuilder> configure)
    {
        ArgumentHelpers.ThrowIfNull(configure);
        configure(_responses);
        return this;
    }

    internal StoryAction Build()
    {
        _action.Effects = _responses.Build();
        return _action;
    }
}
