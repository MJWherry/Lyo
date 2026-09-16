namespace Lyo.Algorithms.Models.Request;

/// <summary>Body for appending a revision. Revert uses <see cref="AlgorithmRevertReq" />.</summary>
public sealed class AlgorithmRevisionReq
{
    /// <summary>Catalog kind stored on this snapshot.</summary>
    public AlgorithmKind Kind { get; set; }

    /// <summary>Expression source, or the startup key when <see cref="Kind" /> is <see cref="AlgorithmKind.Named" />.</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>JSON body for structured scorers. Null for expressions.</summary>
    public string? Payload { get; set; }

    /// <summary>Optional display name written onto the definition with this revision.</summary>
    public string? Name { get; set; }

    /// <summary>Optional description written onto the definition with this revision.</summary>
    public string? Description { get; set; }
}
