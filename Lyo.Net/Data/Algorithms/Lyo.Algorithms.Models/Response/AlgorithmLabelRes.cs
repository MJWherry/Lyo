namespace Lyo.Algorithms.Models.Response;

/// <summary>A named pointer at a revision.</summary>
public sealed class AlgorithmLabelRes
{
    /// <summary>Owning definition.</summary>
    public Guid DefinitionId { get; set; }

    /// <summary>Label name.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Revision this label currently selects.</summary>
    public int Revision { get; set; }

    /// <summary>When false, the label cannot move to a different revision.</summary>
    public bool IsMutable { get; set; }
}
