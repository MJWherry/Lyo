namespace Lyo.Diagram.Models;

/// <summary>Identity and name of a stored diagram, without its cells. <see cref="IDiagramStore.ListAsync" /> returns these.</summary>
public sealed record DiagramSummary
{
    /// <summary>Store key. Matches <see cref="DiagramDocument.Id" />.</summary>
    public Guid Id { get; init; }

    /// <summary>Display name.</summary>
    public required string Name { get; init; }

    /// <summary>When the row was last saved, in UTC. The first save uses the creation time.</summary>
    public DateTime UpdatedTimestamp { get; init; }
}
