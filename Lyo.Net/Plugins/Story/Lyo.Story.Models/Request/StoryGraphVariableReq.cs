using Lyo.Story.Enums;

namespace Lyo.Story.Models.Request;

/// <summary>Variable inside a <see cref="StoryGraphReq" />.</summary>
public sealed class StoryGraphVariableReq
{
    /// <summary>Stable key.</summary>
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