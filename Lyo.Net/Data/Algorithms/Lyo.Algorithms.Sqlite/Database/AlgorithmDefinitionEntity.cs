namespace Lyo.Algorithms.Sqlite.Database;

/// <summary>Row in <c>algorithm_definition</c>.</summary>
public sealed class AlgorithmDefinitionEntity
{
    public Guid Id { get; set; }

    public string Key { get; set; } = string.Empty;

    public string? Name { get; set; }

    public string? Description { get; set; }

    public string Kind { get; set; } = string.Empty;

    public int CurrentRevision { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public List<AlgorithmRevisionEntity> Revisions { get; set; } = [];

    public List<AlgorithmLabelEntity> Labels { get; set; } = [];
}