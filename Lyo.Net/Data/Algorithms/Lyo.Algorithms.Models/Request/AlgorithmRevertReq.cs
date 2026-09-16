namespace Lyo.Algorithms.Models.Request;

/// <summary>Body for copying an existing snapshot onto a new current revision.</summary>
public sealed class AlgorithmRevertReq
{
    /// <summary>Revision to copy. The copy is a new revision; the old row is not edited.</summary>
    public int Revision { get; set; }
}
