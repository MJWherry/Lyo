using Lyo.Algorithms.Models.Request;
using Lyo.Algorithms.Models.Response;

namespace Lyo.Algorithms.Client;

/// <summary>Typed HTTP client for the Lyo algorithm catalog. Saved keys still execute in-process. <see cref="PreviewAsync" /> is the one remote run, and it does not persist.</summary>
public interface IAlgorithmClient
{
    /// <summary>Lists every catalog definition, ordered by key.</summary>
    Task<IReadOnlyList<AlgorithmDefinitionRes>> ListAsync(CancellationToken ct = default);

    /// <summary>Definition by key, or null when the catalog has no row.</summary>
    Task<AlgorithmDefinitionRes?> GetAsync(string key, CancellationToken ct = default);

    /// <summary>Appends a revision after the server runs syntax and startup-key checks.</summary>
    Task<AlgorithmDefinitionRes> SaveRevisionAsync(string key, AlgorithmRevisionReq request, CancellationToken ct = default);

    /// <summary>Revisions newest first.</summary>
    Task<IReadOnlyList<AlgorithmRevisionRes>> GetRevisionsAsync(string key, CancellationToken ct = default);

    /// <summary>Copies <paramref name="request" />'s revision onto a new current revision.</summary>
    Task<AlgorithmDefinitionRes> RevertAsync(string key, AlgorithmRevertReq request, CancellationToken ct = default);

    /// <summary>Label row, or null.</summary>
    Task<AlgorithmLabelRes?> GetLabelAsync(string key, string label, CancellationToken ct = default);

    /// <summary>Creates or retargets a label. Immutable labels cannot move.</summary>
    Task<AlgorithmLabelRes> SetLabelAsync(string key, string label, AlgorithmLabelReq request, CancellationToken ct = default);

    /// <summary>Compiles <paramref name="request" /> and runs it. The catalog is not changed.</summary>
    Task<AlgorithmPreviewRes> PreviewAsync(AlgorithmPreviewReq request, CancellationToken ct = default);

    /// <summary>Removes the definition, its revisions, and its labels. A missing key is a failed response from the server.</summary>
    Task DeleteAsync(string key, CancellationToken ct = default);
}