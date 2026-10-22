namespace Lyo.Algorithms;

/// <summary>Persistence for catalog definitions. Memory, Postgres, and SQLite implement this. Writes are unvalidated; <see cref="IAlgorithmCatalog" /> checks keys and syntax.</summary>
public interface IAlgorithmDefinitionStore
{
    /// <summary>Appends a revision and makes it current. Creates the definition on the first call.</summary>
    Task<AlgorithmDefinitionRes> SaveRevisionAsync(string key, AlgorithmRevisionReq request, CancellationToken ct = default);

    /// <summary>Definition by key, or null.</summary>
    Task<AlgorithmDefinitionRes?> GetByKeyAsync(string key, CancellationToken ct = default);

    /// <summary>Every definition, ordered by key.</summary>
    Task<IReadOnlyList<AlgorithmDefinitionRes>> ListAsync(CancellationToken ct = default);

    /// <summary>Revisions newest first.</summary>
    Task<IReadOnlyList<AlgorithmRevisionRes>> GetRevisionsAsync(string key, CancellationToken ct = default);

    /// <summary>One revision, or null.</summary>
    Task<AlgorithmRevisionRes?> GetRevisionAsync(string key, int revision, CancellationToken ct = default);

    /// <summary>Copies <paramref name="request" /> onto a new current revision. Does not edit the old row.</summary>
    Task<AlgorithmDefinitionRes> RevertToRevisionAsync(string key, AlgorithmRevertReq request, CancellationToken ct = default);

    /// <summary>Points <paramref name="label" /> at <paramref name="request" />. Immutable labels refuse a different revision.</summary>
    Task<AlgorithmLabelRes> SetLabelAsync(string key, string label, AlgorithmLabelReq request, CancellationToken ct = default);

    /// <summary>Label row, or null.</summary>
    Task<AlgorithmLabelRes?> GetLabelAsync(string key, string label, CancellationToken ct = default);

    /// <summary>Removes the definition, its revisions, and its labels. Returns false when the key is absent.</summary>
    Task<bool> DeleteAsync(string key, CancellationToken ct = default);
}