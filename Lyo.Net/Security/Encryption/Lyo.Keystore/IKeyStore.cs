namespace Lyo.Keystore;

/// <summary>
/// Interface for storing and retrieving Key Encryption Keys (KEK) by key ID and version. Supports multi-tenant scenarios where each client/tenant has their own keys. Each
/// key ID can have multiple versions for key rotation.
/// </summary>
public interface IKeyStore
{
    /// <summary> Gets the Key Encryption Key (KEK) for a specific key ID and version. </summary>
    /// <param name="keyId">The key identifier (e.g., client ID, tenant ID)</param>
    /// <param name="version">The key version. If null, gets the current version for this key ID.</param>
    /// <returns>The KEK bytes, or null if not found</returns>
    byte[]? GetKey(string keyId, string? version = null);

    /// <summary> Gets the Key Encryption Key (KEK) for a specific key ID and version asynchronously. </summary>
    /// <param name="keyId">The key identifier</param>
    /// <param name="version">The key version. If null, gets the current version for this key ID.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The KEK bytes, or null if not found</returns>
    Task<byte[]?> GetKeyAsync(string keyId, string? version = null, CancellationToken ct = default);

    /// <summary> Gets the current key for a specific key ID. </summary>
    /// <param name="keyId">The key identifier</param>
    /// <returns>The current KEK bytes for this key ID, or null if not found</returns>
    byte[]? GetCurrentKey(string keyId);

    /// <summary> Gets the current key for a specific key ID asynchronously. </summary>
    /// <param name="keyId">The key identifier</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The current KEK bytes for this key ID, or null if not found</returns>
    Task<byte[]?> GetCurrentKeyAsync(string keyId, CancellationToken ct = default);

    /// <summary> Gets the current version for a specific key ID. </summary>
    /// <param name="keyId">The key identifier</param>
    /// <returns>The current version number, or 0 if no keys exist for this key ID</returns>
    string? GetCurrentVersion(string keyId);

    /// <summary> Gets the current version for a specific key ID asynchronously. </summary>
    /// <param name="keyId">The key identifier</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The current version number, or 0 if no keys exist for this key ID</returns>
    Task<string?> GetCurrentVersionAsync(string keyId, CancellationToken ct = default);

    /// <summary> Adds or updates a Key Encryption Key (KEK) for a specific key ID and version. </summary>
    /// <param name="keyId">The key identifier</param>
    /// <param name="version">The key version</param>
    /// <param name="key">The KEK bytes</param>
    void AddKey(string keyId, string version, byte[] key);

    /// <summary> Adds or updates a Key Encryption Key (KEK) for a specific key ID and version asynchronously. </summary>
    /// <param name="keyId">The key identifier</param>
    /// <param name="version">The key version</param>
    /// <param name="key">The KEK bytes</param>
    /// <param name="ct">Cancellation token</param>
    Task AddKeyAsync(string keyId, string version, byte[] key, CancellationToken ct = default);

    /// <summary>
    /// Adds or updates a Key Encryption Key (KEK) for a specific key ID and version from a string. The string will be derived to a key using the same method as the encryption
    /// service.
    /// </summary>
    /// <param name="keyId">The key identifier</param>
    /// <param name="version">The key version</param>
    /// <param name="keyString">The KEK string to derive</param>
    void AddKeyFromString(string keyId, string version, string keyString);

    /// <summary> Adds or updates a Key Encryption Key (KEK) for a specific key ID and version from a string asynchronously. </summary>
    /// <param name="keyId">The key identifier</param>
    /// <param name="version">The key version</param>
    /// <param name="keyString">The KEK string to derive</param>
    /// <param name="ct">Cancellation token</param>
    Task AddKeyFromStringAsync(string keyId, string version, string keyString, CancellationToken ct = default);

    /// <summary> Sets the current version for a specific key ID. </summary>
    /// <param name="keyId">The key identifier</param>
    /// <param name="version">The key version to set as current</param>
    void SetCurrentVersion(string keyId, string version);

    /// <summary> Sets the current version for a specific key ID asynchronously. </summary>
    /// <param name="keyId">The key identifier</param>
    /// <param name="version">The key version to set as current</param>
    /// <param name="ct">Cancellation token</param>
    Task SetCurrentVersionAsync(string keyId, string version, CancellationToken ct = default);

    /// <summary> Checks if a key exists for the specified key ID and version. </summary>
    /// <param name="keyId">The key identifier</param>
    /// <param name="version">The key version. If null, checks if any version exists for this key ID.</param>
    /// <returns>True if the key exists, false otherwise</returns>
    bool HasKey(string keyId, string? version = null);

    /// <summary> Checks if a key exists for the specified key ID and version asynchronously. </summary>
    /// <param name="keyId">The key identifier</param>
    /// <param name="version">The key version. If null, checks if any version exists for this key ID.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if the key exists, false otherwise</returns>
    Task<bool> HasKeyAsync(string keyId, string? version = null, CancellationToken ct = default);

    /// <summary> Gets metadata for a specific key ID and version. </summary>
    /// <param name="keyId">The key identifier</param>
    /// <param name="version">The key version</param>
    /// <returns>The key metadata, or null if not found</returns>
    KeyMetadata? GetKeyMetadata(string keyId, string version);

    /// <summary> Gets metadata for a specific key ID and version asynchronously. </summary>
    /// <param name="keyId">The key identifier</param>
    /// <param name="version">The key version</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The key metadata, or null if not found</returns>
    Task<KeyMetadata?> GetKeyMetadataAsync(string keyId, string version, CancellationToken ct = default);

    /// <summary> Sets metadata for a specific key ID and version. </summary>
    /// <param name="keyId">The key identifier</param>
    /// <param name="version">The key version</param>
    /// <param name="metadata">The metadata to set</param>
    void SetKeyMetadata(string keyId, string version, KeyMetadata metadata);

    /// <summary> Sets metadata for a specific key ID and version asynchronously. </summary>
    /// <param name="keyId">The key identifier</param>
    /// <param name="version">The key version</param>
    /// <param name="metadata">The metadata to set</param>
    /// <param name="ct">Cancellation token</param>
    Task SetKeyMetadataAsync(string keyId, string version, KeyMetadata metadata, CancellationToken ct = default);

    /// <summary> Gets the salt used for key derivation for a specific key ID and version. </summary>
    /// <param name="keyId">The key identifier</param>
    /// <param name="version">The key version</param>
    /// <returns>The salt bytes if found in metadata, or null if not found</returns>
    byte[]? GetSaltForVersion(string keyId, string version);

    /// <summary> Gets the salt used for key derivation for a specific key ID and version asynchronously. </summary>
    /// <param name="keyId">The key identifier</param>
    /// <param name="version">The key version</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The salt bytes if found in metadata, or null if not found</returns>
    Task<byte[]?> GetSaltForVersionAsync(string keyId, string version, CancellationToken ct = default);

    /// <summary>Updates a key for a specific key ID by incrementing the version and setting it as current. If no key exists for this keyId, starts at version 1.</summary>
    /// <param name="keyId">The key identifier</param>
    /// <param name="key">The new KEK bytes</param>
    /// <returns>The new version number</returns>
    string UpdateKey(string keyId, byte[] key);

    /// <summary>Updates a key for a specific key ID by incrementing the version and setting it as current asynchronously. If no key exists for this keyId, starts at version 1.</summary>
    /// <param name="keyId">The key identifier</param>
    /// <param name="key">The new KEK bytes</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The new version number</returns>
    Task<string> UpdateKeyAsync(string keyId, byte[] key, CancellationToken ct = default);

    /// <summary>
    /// Updates a key for a specific key ID by incrementing the version and setting it as current from a string. The string will be derived to a key using the same method as the
    /// encryption service. If no key exists for this keyId, starts at version 1.
    /// </summary>
    /// <param name="keyId">The key identifier</param>
    /// <param name="keyString">The new KEK string to derive</param>
    /// <returns>The new version number</returns>
    string UpdateKeyFromString(string keyId, string keyString);

    /// <summary>
    /// Updates a key for a specific key ID by incrementing the version and setting it as current from a string asynchronously. The string will be derived to a key using the same
    /// method as the encryption service. If no key exists for this keyId, starts at version 1.
    /// </summary>
    /// <param name="keyId">The key identifier</param>
    /// <param name="keyString">The new KEK string to derive</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The new version number</returns>
    Task<string> UpdateKeyFromStringAsync(string keyId, string keyString, CancellationToken ct = default);
}