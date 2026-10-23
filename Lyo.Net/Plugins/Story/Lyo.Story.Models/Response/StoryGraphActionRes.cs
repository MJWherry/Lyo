namespace Lyo.Story.Models.Response;

/// <summary>Named action inside a <see cref="StoryGraphRes" />.</summary>
public sealed class StoryGraphActionRes
{
    /// <summary>Row id.</summary>
    public Guid Id { get; set; }

    /// <summary>Stable key.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Optional display name.</summary>
    public string? Name { get; set; }

    /// <summary>Response steps.</summary>
    public IReadOnlyList<StoryGraphEffectRes> Effects { get; set; } = [];
}