using Lyo.Result;

namespace Lyo.Algorithms;

/// <summary>Validated catalog writes. Rejects bad keys, denied expressions, and runtime rows that would replace a startup algorithm.</summary>
public interface IAlgorithmCatalog
{
    /// <summary>Appends a revision after syntax and shadow checks. A failed result leaves the previous current revision in place.</summary>
    Task<Result<AlgorithmDefinitionRes>> SaveRevisionAsync(string key, AlgorithmRevisionReq request, CancellationToken ct = default);

    /// <summary>Definition by key, or null.</summary>
    Task<AlgorithmDefinitionRes?> GetByKeyAsync(string key, CancellationToken ct = default);

    /// <summary>Revisions newest first.</summary>
    Task<IReadOnlyList<AlgorithmRevisionRes>> GetRevisionsAsync(string key, CancellationToken ct = default);

    /// <summary>Copies a snapshot onto a new current revision.</summary>
    Task<Result<AlgorithmDefinitionRes>> RevertToRevisionAsync(string key, AlgorithmRevertReq request, CancellationToken ct = default);

    /// <summary>Creates or retargets a label. Immutable labels cannot move.</summary>
    Task<Result<AlgorithmLabelRes>> SetLabelAsync(string key, string label, AlgorithmLabelReq request, CancellationToken ct = default);
}