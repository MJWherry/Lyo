using Lyo.Common.Metadata.Records;
using Lyo.Exceptions;
using Lyo.Story.Enums;

namespace Lyo.Story.Builders;

/// <summary>Builds a <see cref="StoryNode"/> with optional presentation and choices.</summary>
public sealed class StoryNodeBuilder
{
    private readonly StoryNode _node;
    private readonly List<StoryChoice> _choices = [];
    private readonly List<StoryReaction> _scopedReactions = [];

    internal StoryNodeBuilder(string key)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        _node = new() { Key = key };
    }

    /// <summary>Optional title.</summary>
    public StoryNodeBuilder Titled(string title)
    {
        _node.Title = title;
        return this;
    }

    /// <summary>Places this node in an optional scene group.</summary>
    public StoryNodeBuilder InScene(string sceneKey)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(sceneKey);
        _node.SceneKey = sceneKey;
        return this;
    }

    /// <summary>Marks this node as an ending beat.</summary>
    public StoryNodeBuilder Ending()
    {
        _node.IsEnding = true;
        return this;
    }

    /// <summary>Text presentation. Defaults to <c>MimeTypeInfo.Txt.Value</c>. Pass <c>MimeTypeInfo.Html.Value</c> for HTML.</summary>
    public StoryNodeBuilder Text(string body, string? contentType = null)
    {
        _node.PresentationKind = StoryPresentationKind.Text;
        _node.Body = body;
        _node.ContentType = contentType ?? MimeTypeInfo.Txt.Value;
        return this;
    }

    /// <summary>Cutscene presentation. Completing it is Advance, not a choice.</summary>
    public StoryNodeBuilder Cutscene(string mediaRef)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(mediaRef);
        _node.PresentationKind = StoryPresentationKind.Cutscene;
        _node.MediaRef = mediaRef;
        return this;
    }

    /// <summary>Adds a player choice. Prompt defaults to <paramref name="key"/> when omitted.</summary>
    public StoryNodeBuilder Choice(string key, Action<StoryChoiceBuilder> configure) => Choice(key, prompt: null, configure);

    /// <summary>Adds a player choice with an explicit prompt.</summary>
    public StoryNodeBuilder Choice(string key, string? prompt, Action<StoryChoiceBuilder> configure)
    {
        ArgumentHelpers.ThrowIfNull(configure);
        var builder = new StoryChoiceBuilder(key, prompt ?? key);
        configure(builder);
        _choices.Add(builder.Build(_choices.Count));
        return this;
    }

    /// <summary>Adds a node-scoped reaction for <paramref name="trigger"/> (for example Advanced on a cutscene).</summary>
    public StoryNodeBuilder On(StoryTrigger trigger, Action<StoryReactionBuilder> configure)
    {
        ArgumentHelpers.ThrowIfNull(configure);
        var key = $"{_node.Key}_{trigger}_{_scopedReactions.Count}";
        var builder = new StoryReactionBuilder(key);
        builder.On(trigger, _node.Key);
        configure(builder);
        _scopedReactions.Add(builder.Build());
        return this;
    }

    internal StoryNode Build()
    {
        _node.Choices = [.. _choices];
        return _node;
    }

    internal IReadOnlyList<StoryReaction> ScopedReactions => _scopedReactions;
}
