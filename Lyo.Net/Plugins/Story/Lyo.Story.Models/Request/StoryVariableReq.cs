using Lyo.Story.Enums;

namespace Lyo.Story.Models.Request;

/// <summary>Create or update a declared variable.</summary>
public sealed class StoryVariableReq
{
    /// <summary>Owning story.</summary>
    public Guid StoryId { get; set; }

    /// <summary>Stable key within the story.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Optional display name.</summary>
    public string? Name { get; set; }

    /// <summary>Value kind.</summary>
    public StoryVariableType Type { get; set; }

    /// <summary>Default written into the play bag at start.</summary>
    public string? DefaultValue { get; set; }

    /// <summary>Optional description.</summary>
    public string? Description { get; set; }
}