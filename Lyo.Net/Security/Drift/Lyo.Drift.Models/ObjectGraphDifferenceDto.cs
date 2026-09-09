namespace Lyo.Drift.Models;

/// <summary>One persistable object-graph leaf difference. Values are JSON, never live CLR objects.</summary>
public sealed class ObjectGraphDifferenceDto
{
    /// <summary>Dotted path (for example <c>Drives.0.Name</c>).</summary>
    public string Path { get; set; } = "";

    /// <summary>JSON of the previous leaf, or null.</summary>
    public string? OldValueJson { get; set; }

    /// <summary>JSON of the new leaf, or null.</summary>
    public string? NewValueJson { get; set; }
}
