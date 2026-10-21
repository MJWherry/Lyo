namespace Lyo.Algorithms.Postgres.Database;

/// <summary>Append-only row in <c>algorithms.algorithm_definition_revision</c>.</summary>
public sealed class AlgorithmRevisionEntity
{
    public Guid DefinitionId { get; set; }

    public int Revision { get; set; }

    public string Kind { get; set; } = string.Empty;

    public string Source { get; set; } = string.Empty;

    public string? Payload { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public AlgorithmDefinitionEntity? Definition { get; set; }
}