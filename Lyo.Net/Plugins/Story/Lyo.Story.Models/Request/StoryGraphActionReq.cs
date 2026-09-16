namespace Lyo.Story.Models.Request;

/// <summary>Named action inside a <see cref="StoryGraphReq" />.</summary>
public sealed class StoryGraphActionReq
{
    /// <summary>Stable key.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Optional display name.</summary>
    public string? Name { get; set; }

    /// <summary>Response steps.</summary>
    public List<StoryGraphEffectReq> Effects { get; set; } = [];
}
