namespace Lyo.Keystore;

/// <summary>Provides optional key inventory capabilities for UI/workbench scenarios.</summary>
public interface IKeyInventoryStore
{
    /// <summary>Gets all available key identifiers.</summary>
    Task<IReadOnlyList<string>> GetAvailableKeyIdsAsync(CancellationToken ct = default);

    /// <summary>Gets all available versions for a specific key identifier.</summary>
    Task<IReadOnlyList<string>> GetAvailableVersionsAsync(string keyId, CancellationToken ct = default);
}