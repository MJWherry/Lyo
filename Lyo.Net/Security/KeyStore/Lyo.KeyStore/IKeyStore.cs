namespace Lyo.KeyStore;

/// <summary>
/// Store and retrieve Key Encryption Keys (KEK) by key ID and version. Supports multi-tenant setups where each client/tenant has its own keys. Each
/// key ID can have multiple versions for rotation.
/// </summary>
public interface IKeyStore
{
    /// <summary>Returns the Key Encryption Key (KEK) for a given key ID and version.</summary>
    /// <param name="keyId">Key identifier (e.g., client ID, tenant ID)</param>
    /// <param name="version">Key version. When null, uses the current version for this key ID.</param>
    /// <returns>KEK bytes, or null if not found</returns>
    byte[]? GetKey(string keyId, string? version = null);

    /// <summary>Returns the Key Encryption Key (KEK) for a given key ID and version asynchronously.</summary>
    /// <param name="keyId">Key identifier</param>
    /// <param name="version">Key version. When null, uses the current version for this key ID.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>KEK bytes, or null if not found</returns>
    Task<byte[]?> GetKeyAsync(string keyId, string? version = null, CancellationToken ct = default);

    /// <summary>Returns the current key for a given key ID.</summary>
    /// <param name="keyId">Key identifier</param>
    /// <returns>Current KEK bytes for this key ID, or null if not found</returns>
    byte[]? GetCurrentKey(string keyId);

    /// <summary>Returns the current key for a given key ID asynchronously.</summary>
    /// <param name="keyId">Key identifier</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Current KEK bytes for this key ID, or null if not found</returns>
    Task<byte[]?> GetCurrentKeyAsync(string keyId, CancellationToken ct = default);

    /// <summary>Returns the current version for a given key ID.</summary>
    /// <param name="keyId">Key identifier</param>
    /// <returns>Current version number, or 0 if no keys exist for this key ID</returns>
    string? GetCurrentVersion(string keyId);

    /// <summary>Returns the current version for a given key ID asynchronously.</summary>
    /// <param name="keyId">Key identifier</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Current version number, or 0 if no keys exist for this key ID</returns>
    Task<string?> GetCurrentVersionAsync(string keyId, CancellationToken ct = default);

    /// <summary>Adds or replaces a Key Encryption Key (KEK) for a given key ID and version.</summary>
    /// <param name="keyId">Key identifier</param>
    /// <param name="version">Key version</param>
    /// <param name="key">KEK bytes</param>
    void AddKey(string keyId, string version, byte[] key);

    /// <summary>Adds or replaces a Key Encryption Key (KEK) for a given key ID and version asynchronously.</summary>
    /// <param name="keyId">Key identifier</param>
    /// <param name="version">Key version</param>
    /// <param name="key">KEK bytes</param>
    /// <param name="ct">Cancellation token</param>
    Task AddKeyAsync(string keyId, string version, byte[] key, CancellationToken ct = default);

    /// <summary>
    /// Adds or replaces a Key Encryption Key (KEK) for a given key ID and version from a string. The string is derived to a key using the same method as the encryption
    /// service.
    /// </summary>
    /// <param name="keyId">Key identifier</param>
    /// <param name="version">Key version</param>
    /// <param name="keyString">KEK string to derive</param>
    void AddKeyFromString(string keyId, string version, string keyString);

    /// <summary>Adds or replaces a Key Encryption Key (KEK) for a given key ID and version from a string asynchronously.</summary>
    /// <param name="keyId">Key identifier</param>
    /// <param name="version">Key version</param>
    /// <param name="keyString">KEK string to derive</param>
    /// <param name="ct">Cancellation token</param>
    Task AddKeyFromStringAsync(string keyId, string version, string keyString, CancellationToken ct = default);

    /// <summary>Marks a version as current for a given key ID.</summary>
    /// <param name="keyId">Key identifier</param>
    /// <param name="version">Key version to set as current</param>
    void SetCurrentVersion(string keyId, string version);

    /// <summary>Marks a version as current for a given key ID asynchronously.</summary>
    /// <param name="keyId">Key identifier</param>
    /// <param name="version">Key version to set as current</param>
    /// <param name="ct">Cancellation token</param>
    Task SetCurrentVersionAsync(string keyId, string version, CancellationToken ct = default);

    /// <summary>True if a key exists for the given key ID and version.</summary>
    /// <param name="keyId">Key identifier</param>
    /// <param name="version">Key version. When null, checks whether any version exists for this key ID.</param>
    /// <returns>True if the key exists, false otherwise</returns>
    bool HasKey(string keyId, string? version = null);

    /// <summary>True if a key exists for the given key ID and version asynchronously.</summary>
    /// <param name="keyId">Key identifier</param>
    /// <param name="version">Key version. When null, checks whether any version exists for this key ID.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if the key exists, false otherwise</returns>
    Task<bool> HasKeyAsync(string keyId, string? version = null, CancellationToken ct = default);

    /// <summary>Returns metadata for a given key ID and version.</summary>
    /// <param name="keyId">Key identifier</param>
    /// <param name="version">Key version</param>
    /// <returns>Key metadata, or null if not found</returns>
    KeyMetadata? GetKeyMetadata(string keyId, string version);

    /// <summary>Returns metadata for a given key ID and version asynchronously.</summary>
    /// <param name="keyId">Key identifier</param>
    /// <param name="version">Key version</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Key metadata, or null if not found</returns>
    Task<KeyMetadata?> GetKeyMetadataAsync(string keyId, string version, CancellationToken ct = default);

    /// <summary>Writes metadata for a given key ID and version.</summary>
    /// <param name="keyId">Key identifier</param>
    /// <param name="version">Key version</param>
    /// <param name="metadata">Metadata to store</param>
    void SetKeyMetadata(string keyId, string version, KeyMetadata metadata);

    /// <summary>Writes metadata for a given key ID and version asynchronously.</summary>
    /// <param name="keyId">Key identifier</param>
    /// <param name="version">Key version</param>
    /// <param name="metadata">Metadata to store</param>
    /// <param name="ct">Cancellation token</param>
    Task SetKeyMetadataAsync(string keyId, string version, KeyMetadata metadata, CancellationToken ct = default);

    /// <summary>Returns the salt used for key derivation for a given key ID and version.</summary>
    /// <param name="keyId">Key identifier</param>
    /// <param name="version">Key version</param>
    /// <returns>Salt bytes if found in metadata, or null if not found</returns>
    byte[]? GetSaltForVersion(string keyId, string version);

    /// <summary>Returns the salt used for key derivation for a given key ID and version asynchronously.</summary>
    /// <param name="keyId">Key identifier</param>
    /// <param name="version">Key version</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Salt bytes if found in metadata, or null if not found</returns>
    Task<byte[]?> GetSaltForVersionAsync(string keyId, string version, CancellationToken ct = default);

    /// <summary>Updates a key for a given key ID by incrementing the version and making it current. If no key exists for this keyId, starts at version 1.</summary>
    /// <param name="keyId">Key identifier</param>
    /// <param name="key">New KEK bytes</param>
    /// <returns>The new version number</returns>
    string UpdateKey(string keyId, byte[] key);

    /// <summary>Updates a key for a given key ID by incrementing the version and making it current asynchronously. If no key exists for this keyId, starts at version 1.</summary>
    /// <param name="keyId">Key identifier</param>
    /// <param name="key">New KEK bytes</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The new version number</returns>
    Task<string> UpdateKeyAsync(string keyId, byte[] key, CancellationToken ct = default);

    /// <summary>
    /// Updates a key for a given key ID by incrementing the version and making it current from a string. The string is derived to a key using the same method as the
    /// encryption service. If no key exists for this keyId, starts at version 1.
    /// </summary>
    /// <param name="keyId">Key identifier</param>
    /// <param name="keyString">New KEK string to derive</param>
    /// <returns>The new version number</returns>
    string UpdateKeyFromString(string keyId, string keyString);

    /// <summary>
    /// Updates a key for a given key ID by incrementing the version and making it current from a string asynchronously. The string is derived to a key using the same
    /// method as the encryption service. If no key exists for this keyId, starts at version 1.
    /// </summary>
    /// <param name="keyId">Key identifier</param>
    /// <param name="keyString">New KEK string to derive</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The new version number</returns>
    Task<string> UpdateKeyFromStringAsync(string keyId, string keyString, CancellationToken ct = default);
}