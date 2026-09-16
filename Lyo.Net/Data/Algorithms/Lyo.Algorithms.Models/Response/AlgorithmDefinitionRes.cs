namespace Lyo.Algorithms.Models.Response;

/// <summary>One catalog definition returned by the store and the API.</summary>
public sealed class AlgorithmDefinitionRes
{
    /// <summary>Stable id. Assigned on first save.</summary>
    public Guid Id { get; set; }

    /// <summary>Stable catalog key.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Optional display name.</summary>
    public string? Name { get; set; }

    /// <summary>Optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Kind of the current revision.</summary>
    public AlgorithmKind Kind { get; set; }

    /// <summary>Highest saved revision.</summary>
    public int CurrentRevision { get; set; }

    /// <summary>When the definition was created (UTC).</summary>
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>When the current revision last changed (UTC).</summary>
    public DateTime? UpdatedTimestamp { get; set; }
}
