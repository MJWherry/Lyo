using Lyo.Story.Enums;

namespace Lyo.Story.Models.Response;

/// <summary>Playthrough snapshot plus the current node's presentation and available moves.</summary>
public sealed class StoryPlayStateRes
{
    /// <summary>Playthrough header and variable bag.</summary>
    public StoryPlaySnapshotRes Playthrough { get; set; } = new();

    /// <summary>Current node presentation. Null when the playthrough has no cursor.</summary>
    public StoryPlayNodeRes? CurrentNode { get; set; }

    /// <summary>Choices and Advance the player may take.</summary>
    public IReadOnlyList<StoryMoveRes> Moves { get; set; } = [];
}

/// <summary>Playthrough fields returned with a live snapshot. The cursor is a node key, not a row id.</summary>
public sealed class StoryPlaySnapshotRes
{
    /// <summary>Playthrough id.</summary>
    public Guid Id { get; set; }

    /// <summary>Story being played.</summary>
    public Guid StoryId { get; set; }

    /// <summary>Optional opaque player handle.</summary>
    public string? PlayerRef { get; set; }

    /// <summary>Play status.</summary>
    public StoryPlaythroughStatus Status { get; set; }

    /// <summary>Current node key.</summary>
    public string? CurrentNodeKey { get; set; }

    /// <summary>When play started (UTC).</summary>
    public DateTime StartedAt { get; set; }

    /// <summary>Last move (UTC).</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>When play completed (UTC).</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>Current variable values.</summary>
    public IReadOnlyList<StoryPlayVariableRes> Variables { get; set; } = [];
}

/// <summary>One variable value on a play snapshot.</summary>
public sealed class StoryPlayVariableRes
{
    /// <summary>Variable key.</summary>
    public string VariableKey { get; set; } = string.Empty;

    /// <summary>Current value.</summary>
    public string? Value { get; set; }

    /// <summary>When the value last changed (UTC).</summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>Presentation of the node the player is on.</summary>
public sealed class StoryPlayNodeRes
{
    /// <summary>Node key.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Optional title.</summary>
    public string? Title { get; set; }

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
}
