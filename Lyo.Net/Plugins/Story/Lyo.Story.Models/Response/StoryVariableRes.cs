using Lyo.Story.Enums;

namespace Lyo.Story.Models.Response;

/// <summary>Variable row as returned by the API.</summary>
public sealed class StoryVariableRes
{
    /// <summary>Row id.</summary>
    public Guid Id { get; set; }

    /// <summary>Owning story.</summary>
    public Guid StoryId { get; set; }

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
