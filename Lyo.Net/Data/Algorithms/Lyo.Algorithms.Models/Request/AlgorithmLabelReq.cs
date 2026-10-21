namespace Lyo.Algorithms.Models.Request;

/// <summary>Body for creating or retargeting a label. The label name is the route.</summary>
public sealed class AlgorithmLabelReq
{
    /// <summary>Revision this label should select.</summary>
    public int Revision { get; set; }

    /// <summary>When false, a later call cannot move the label to a different revision.</summary>
    public bool IsMutable { get; set; }
}