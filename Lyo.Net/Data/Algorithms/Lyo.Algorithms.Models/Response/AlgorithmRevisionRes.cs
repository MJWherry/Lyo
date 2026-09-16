namespace Lyo.Algorithms.Models.Response;

/// <summary>One append-only revision snapshot returned by the store and the API.</summary>
public sealed class AlgorithmRevisionRes
{
    /// <summary>Owning definition.</summary>
    public Guid DefinitionId { get; set; }

    /// <summary>1-based revision number.</summary>
    public int Revision { get; set; }

    /// <summary>Kind stored on this snapshot.</summary>
    public AlgorithmKind Kind { get; set; }

    /// <summary>Expression source, or the startup key when <see cref="Kind" /> is <see cref="AlgorithmKind.Named" />.</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>JSON body for structured scorers.</summary>
    public string? Payload { get; set; }

    /// <summary>When this snapshot was written (UTC).</summary>
    public DateTime CreatedTimestamp { get; set; }
}
