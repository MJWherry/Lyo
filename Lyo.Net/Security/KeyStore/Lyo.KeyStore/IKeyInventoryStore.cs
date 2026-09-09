namespace Lyo.KeyStore;

/// <summary>Optional key-inventory APIs for UI/workbench scenarios.</summary>
public interface IKeyInventoryStore
{
    /// <summary>All available key identifiers.</summary>
    Task<IReadOnlyList<string>> GetAvailableKeyIdsAsync(CancellationToken ct = default);

    /// <summary>All available versions for a given key identifier.</summary>
    Task<IReadOnlyList<string>> GetAvailableVersionsAsync(string keyId, CancellationToken ct = default);
}