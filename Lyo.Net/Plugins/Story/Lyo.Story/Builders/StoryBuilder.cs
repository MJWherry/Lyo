using Lyo.Exceptions;
using Lyo.Story.Enums;

namespace Lyo.Story.Builders;

/// <summary>Fluent authoring for a <see cref="Story" /> aggregate. Keys are strings; the store assigns Guids.</summary>
public sealed class StoryBuilder
{
    private readonly List<StoryAction> _actions = [];
    private readonly List<StoryReaction> _nodeReactions = [];
    private readonly List<StoryNode> _nodes = [];
    private readonly List<StoryReaction> _reactions = [];
    private readonly List<StoryScene> _scenes = [];
    private readonly Story _story;
    private readonly List<StoryVariable> _variables = [];

    private StoryBuilder(string title)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(title);
        _story = new() { Title = title, Status = StoryStatus.Draft };
    }

    /// <summary>Starts a story titled <paramref name="title" />.</summary>
    public static StoryBuilder New(string title) => new(title);

    /// <summary>Sets the unique slug.</summary>
    public StoryBuilder Slug(string slug)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(slug);
        _story.Slug = slug;
        return this;
    }

    /// <summary>Optional description.</summary>
    public StoryBuilder Description(string description)
    {
        _story.Description = description;
        return this;
    }

    /// <summary>Language tag (for example <c>en</c>).</summary>
    public StoryBuilder Language(string language)
    {
        _story.Language = language;
        return this;
    }

    /// <summary>Declares a typed variable with an optional default.</summary>
    public StoryBuilder Variable(string key, StoryVariableType type, object? defaultValue = null, string? name = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        _variables.Add(
            new() {
                Key = key,
                Type = type,
                Name = name,
                DefaultValue = defaultValue is null ? null : Convert.ToString(defaultValue)
            });

        return this;
    }

    /// <summary>Optional scene group. Nodes opt in with <see cref="StoryNodeBuilder.InScene" />.</summary>
    public StoryBuilder Scene(string key, string? title = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        _scenes.Add(new() { Key = key, Title = title, SortOrder = _scenes.Count });
        return this;
    }

    /// <summary>Adds a named action.</summary>
    public StoryBuilder Action(string key, Action<StoryActionBuilder> configure)
    {
        ArgumentHelpers.ThrowIfNull(configure);
        var builder = new StoryActionBuilder(key);
        configure(builder);
        _actions.Add(builder.Build());
        return this;
    }

    /// <summary>Adds the start node (play cursor at start).</summary>
    public StoryBuilder StartNode(string key, Action<StoryNodeBuilder> configure)
    {
        AddNode(key, configure, true);
        return this;
    }

    /// <summary>Adds a node.</summary>
    public StoryBuilder Node(string key, Action<StoryNodeBuilder> configure)
    {
        AddNode(key, configure, false);
        return this;
    }

    /// <summary>Adds an ambient reaction.</summary>
    public StoryBuilder React(string key, Action<StoryReactionBuilder> configure)
    {
        ArgumentHelpers.ThrowIfNull(configure);
        var builder = new StoryReactionBuilder(key);
        configure(builder);
        _reactions.Add(builder.Build());
        return this;
    }

    /// <summary>Builds the aggregate and validates goto/action keys.</summary>
    public Story Build()
    {
        if (_nodes.Count == 0)
            throw new StoryException("A story requires at least one node.");

        if (string.IsNullOrWhiteSpace(_story.StartNodeKey))
            _story.StartNodeKey = _nodes[0].Key;

        if (string.IsNullOrWhiteSpace(_story.Slug))
            _story.Slug = Slugify(_story.Title);

        _story.Scenes = _scenes.ToArray();
        _story.Nodes = _nodes.ToArray();
        _story.Variables = _variables.ToArray();
        _story.Actions = _actions.ToArray();
        _story.Reactions = _reactions.Concat(_nodeReactions).ToArray();
        ValidateGraph(_story);
        return _story;
    }

    private void AddNode(string key, Action<StoryNodeBuilder> configure, bool isStart)
    {
        ArgumentHelpers.ThrowIfNull(configure);
        var builder = new StoryNodeBuilder(key);
        configure(builder);
        var node = builder.Build();
        _nodes.Add(node);
        _nodeReactions.AddRange(builder.ScopedReactions);
        if (isStart)
            _story.StartNodeKey = node.Key;
    }

    /// <summary>Rejects unknown start/goto node keys and invoke action keys. Called from <see cref="Build" /> and from the store on save.</summary>
    public static void ValidateGraph(Story story)
    {
        var nodeKeys = new HashSet<string>(story.Nodes.Select(n => n.Key), StringComparer.Ordinal);
        var actionKeys = new HashSet<string>(story.Actions.Select(a => a.Key), StringComparer.Ordinal);
        if (story.StartNodeKey is not null && !nodeKeys.Contains(story.StartNodeKey))
            throw new StoryException($"Start node '{story.StartNodeKey}' is not defined.");

        foreach (var effect in EnumerateEffects(story)) {
            if (effect.Kind == StoryEffectKind.GotoNode && !string.IsNullOrWhiteSpace(effect.TargetNodeKey) && !nodeKeys.Contains(effect.TargetNodeKey))
                throw new StoryException($"Goto targets unknown node '{effect.TargetNodeKey}'.");

            if (effect.Kind == StoryEffectKind.InvokeAction && !string.IsNullOrWhiteSpace(effect.InvokeActionKey) && !actionKeys.Contains(effect.InvokeActionKey))
                throw new StoryException($"Invoke targets unknown action '{effect.InvokeActionKey}'.");
        }
    }

    private static IEnumerable<StoryEffect> EnumerateEffects(Story story)
    {
        foreach (var action in story.Actions) {
            foreach (var effect in action.Effects)
                yield return effect;
        }

        foreach (var reaction in story.Reactions) {
            foreach (var effect in reaction.Effects)
                yield return effect;
        }

        foreach (var node in story.Nodes) {
            foreach (var choice in node.Choices) {
                foreach (var effect in choice.Effects)
                    yield return effect;
            }
        }
    }

    private static string Slugify(string title)
    {
        var chars = title.Trim().ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray();
        return new string(chars).Trim('-');
    }
}