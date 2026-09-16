namespace Lyo.Algorithms.Sqlite.Database;

/// <summary>Row in <c>algorithm_definition_label</c>.</summary>
public sealed class AlgorithmLabelEntity
{
    public Guid DefinitionId { get; set; }

    public string Label { get; set; } = string.Empty;

    public int Revision { get; set; }

    public bool IsMutable { get; set; }

    public AlgorithmDefinitionEntity? Definition { get; set; }
}
