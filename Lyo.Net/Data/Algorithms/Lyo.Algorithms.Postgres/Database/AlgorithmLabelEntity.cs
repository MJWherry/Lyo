using System.Diagnostics;

namespace Lyo.Algorithms.Postgres.Database;

/// <summary>Row in <c>algorithms.algorithm_definition_label</c>.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class AlgorithmLabelEntity
{
    public Guid DefinitionId { get; set; }

    public string Label { get; set; } = string.Empty;

    public int Revision { get; set; }

    public bool IsMutable { get; set; }

    public AlgorithmDefinitionEntity? Definition { get; set; }

    /// <summary>Type name and primary key.</summary>
    public override string ToString() => $"{GetType().Name} {DefinitionId} {Label}";
}