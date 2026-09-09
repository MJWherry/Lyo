using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Lyo.Common.Core.Extensions;
using Lyo.Exceptions;
using Lyo.KeyStore.KeyDerivation;

namespace Lyo.KeyStore;

/// <summary>
/// Local in-memory key store for Key Encryption Keys (KEK) by key ID and version. Fine for development and single-instance use. SECURITY
/// WARNING: keys sit in plain memory with no encryption. They show up in memory dumps and process memory. For production, consider: - a secure
/// key management service (e.g., Azure Key Vault, AWS KMS, HashiCorp Vault) - encrypted storage for keys at rest - hardware security modules (HSM) for key storage
/// This implementation uses a deterministic salt so the same password yields the same key across application restarts. That is a security trade-off for
/// development convenience. Production keystores should persist random salts.
/// </summary>
public class LocalKeyStore : IKeyStore, IKeyInventoryStore
{
    private const string SaltMetadataKey = "Pbkdf2Salt";

    // keyId -> current version
    private readonly ConcurrentDictionary<string, string> _currentVersions = new();

    // keyId -> version -> key bytes
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte[]>> _keys = new();

    // keyId -> version -> metadata
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, KeyMetadata>> _metadata = new();

    public Task<IReadOnlyList<string>> GetAvailableKeyIdsAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<string>>(GetAvailableKeyIds().ToList());
    }

    public Task<IReadOnlyList<string>> GetAvailableVersionsAsync(string keyId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<string>>(GetAvailableVersions(keyId).ToList());
    }

    /// <inheritdoc />
    public byte[]? GetKey(string keyId, string? version = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyId);
        var actualVersion = version ?? GetCurrentVersion(keyId);
        if (actualVersion.IsNullOrWhitespace())
            return null;

        if (!_keys.TryGetValue(keyId, out var versions) || !versions.TryGetValue(actualVersion, out var key))
            return null;

        // Reject expired keys
        var metadata = GetKeyMetadata(keyId, actualVersion);
        OperationHelpers.ThrowIfNull(metadata, $"Key '{keyId}' version {actualVersion} has no metadata.");
        OperationHelpers.ThrowIf(metadata.IsExpired, $"Key '{keyId}' version {actualVersion} has expired (expired at {metadata.ExpiresAt:O}).");
        return key;
    }

    /// <inheritdoc />
    public Task<byte[]?> GetKeyAsync(string keyId, string? version = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(GetKey(keyId, version));
    }

    /// <inheritdoc />
    public byte[]? GetCurrentKey(string keyId)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyId);
        return GetKey(keyId);
    }

    /// <inheritdoc />
    public Task<byte[]?> GetCurrentKeyAsync(string keyId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(GetCurrentKey(keyId));
    }

    /// <inheritdoc />
    public string? GetCurrentVersion(string keyId)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyId);
        return _currentVersions.TryGetValue(keyId, out var ver) ? ver : null;
    }

    /// <inheritdoc />
    public Task<string?> GetCurrentVersionAsync(string keyId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(GetCurrentVersion(keyId));
    }

    /// <inheritdoc />
    public void AddKey(string keyId, string version, byte[] key)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyId);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(version);
        ArgumentHelpers.ThrowIfNullOrEmpty(key);
        var versions = _keys.GetOrAdd(keyId, _ => new());
        var keyCopy = new byte[key.Length];
        Array.Copy(key, keyCopy, key.Length);
        versions[version] = keyCopy;

        // Create metadata if missing
        var metadataDict = _metadata.GetOrAdd(keyId, _ => new());
        if (!metadataDict.ContainsKey(version))
            metadataDict[version] = new() { CreatedAt = DateTime.UtcNow };

        // Make this current when it is the first version for this key ID
        if (!_currentVersions.ContainsKey(keyId))
            _currentVersions[keyId] = version;
    }

    /// <inheritdoc />
    public Task AddKeyAsync(string keyId, string version, byte[] key, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        AddKey(keyId, version, key);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    /// <remarks>
    /// For LocalKeyStore (in-memory), uses a deterministic salt so the same password yields the same key across application restarts. For persistent keystores, salt lives in
    /// metadata.
    /// </remarks>
    public void AddKeyFromString(string keyId, string version, string keyString)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyId);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyString);

        // Load metadata once up front
        var existingMetadata = GetKeyMetadata(keyId, version);

        // If the key already exists, do not regenerate it (important for in-memory stores that lose metadata on restart)
        // That way a key added earlier in this session is not overwritten with a different derived key
        var versions = _keys.GetOrAdd(keyId, _ => new());
        if (versions.ContainsKey(version)) {
            // Key already exists; just make sure metadata is in place if needed
            if (existingMetadata == null) {
                var metadataDict = _metadata.GetOrAdd(keyId, _ => new());
                metadataDict[version] = new() { CreatedAt = DateTime.UtcNow };
            }

            return;
        }

        var derivationService = new Pbkdf2KeyDerivationService();

        // For LocalKeyStore (in-memory), always use a deterministic salt so
        // the same password yields the same key across application restarts.
        // Required because LocalKeyStore does not persist metadata between runs.
        // 
        // For persistent keystores (e.g., Postgres), check metadata for a stored salt first.
        // If salt exists in metadata, use it; otherwise generate a random salt and store it.
        byte[]? salt = null;

        // Try metadata first (persistent keystores that keep metadata)
        if (existingMetadata?.AdditionalData != null && existingMetadata.AdditionalData.TryGetValue(SaltMetadataKey, out var saltBase64)) {
            try {
                salt = Convert.FromBase64String(saltBase64);
            }
            catch (FormatException) {
                // Invalid base64 — fall through to deterministic salt
                salt = null;
            }
        }

        byte[] derivedKey;
        if (salt != null) {
            // Use the salt already in metadata (persistent keystores)
            derivedKey = derivationService.DeriveKey(keyString, salt);
        }
        else {
            // No salt in metadata — use deterministic salt for LocalKeyStore compatibility
            // Same password always yields the same key, even after an app restart
            // LocalKeyStore therefore always uses deterministic salt.
            // Persistent keystores should generate a random salt and store it in metadata.
            // SECURITY NOTE: Deterministic salt is weaker but required for LocalKeyStore
            // to survive application restarts. Production keystores should use random salts.
            using var sha256 = SHA256.Create();
            var passwordHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(keyString));
            var deterministicSalt = new byte[32]; // 32-byte salt (256 bits) for better security
            Array.Copy(passwordHash, deterministicSalt, Math.Min(32, passwordHash.Length));
            // If passwordHash is shorter than 32 bytes, pad with extra hash iterations
            if (passwordHash.Length < 32) {
                var secondHash = sha256.ComputeHash(passwordHash);
                Array.Copy(secondHash, 0, deterministicSalt, passwordHash.Length, 32 - passwordHash.Length);
            }

            derivedKey = derivationService.DeriveKey(keyString, deterministicSalt);
            salt = deterministicSalt;
        }

        AddKey(keyId, version, derivedKey);

        // Persist salt in metadata (also stored in FileStoreResult)
        var metadata = existingMetadata ?? new KeyMetadata { CreatedAt = DateTime.UtcNow };
        var additionalData = metadata.AdditionalData != null ? new(metadata.AdditionalData) : new Dictionary<string, string>();
        additionalData[SaltMetadataKey] = Convert.ToBase64String(salt);
        SetKeyMetadata(keyId, version, metadata with { AdditionalData = additionalData });
    }

    /// <inheritdoc />
    public Task AddKeyFromStringAsync(string keyId, string version, string keyString, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        AddKeyFromString(keyId, version, keyString);
        return Task.CompletedTask;
    }

    /// <summary>Returns the salt used for key derivation for a given key ID and version. Comes from metadata when present, otherwise null.</summary>
    public byte[]? GetSaltForVersion(string keyId, string version)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyId);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(version);
        var metadata = GetKeyMetadata(keyId, version);
        if (metadata?.AdditionalData != null && metadata.AdditionalData.TryGetValue(SaltMetadataKey, out var saltBase64)) {
            try {
                return Convert.FromBase64String(saltBase64);
            }
            catch (FormatException) {
                return null;
            }
        }

        return null;
    }

    public Task<byte[]?> GetSaltForVersionAsync(string keyId, string version, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(GetSaltForVersion(keyId, version));
    }

    public void SetCurrentVersion(string keyId, string version)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyId);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(version);
        OperationHelpers.ThrowIf(!HasKey(keyId, version), $"Key '{keyId}' version {version} does not exist. Add the key before setting it as current.");
        _currentVersions[keyId] = version;
    }

    public Task SetCurrentVersionAsync(string keyId, string version, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        SetCurrentVersion(keyId, version);
        return Task.CompletedTask;
    }

    public bool HasKey(string keyId, string? version = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyId);
        if (!_keys.TryGetValue(keyId, out var versions))
            return false;

        if (version == null)
            return versions.Count > 0;

        return versions.ContainsKey(version);
    }

    public Task<bool> HasKeyAsync(string keyId, string? version = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(HasKey(keyId, version));
    }

    public KeyMetadata? GetKeyMetadata(string keyId, string version)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyId);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(version);
        if (!_metadata.TryGetValue(keyId, out var versions))
            return null;

        return versions.TryGetValue(version, out var meta) ? meta : null;
    }

    public Task<KeyMetadata?> GetKeyMetadataAsync(string keyId, string version, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(GetKeyMetadata(keyId, version));
    }

    public void SetKeyMetadata(string keyId, string version, KeyMetadata metadata)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyId);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(version);
        ArgumentHelpers.ThrowIfNull(metadata);
        OperationHelpers.ThrowIf(!HasKey(keyId, version), $"Key '{keyId}' version {version} does not exist. Add the key before setting metadata.");
        var metadataDict = _metadata.GetOrAdd(keyId, _ => new());
        metadataDict[version] = metadata;
    }

    public Task SetKeyMetadataAsync(string keyId, string version, KeyMetadata metadata, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        SetKeyMetadata(keyId, version, metadata);
        return Task.CompletedTask;
    }

    public string UpdateKey(string keyId, byte[] key)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyId);
        var newVersion = NewKeyVersionString();
        AddKey(keyId, newVersion, key);
        SetCurrentVersion(keyId, newVersion);
        return newVersion;
    }

    public Task<string> UpdateKeyAsync(string keyId, byte[] key, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(UpdateKey(keyId, key));
    }

    public string UpdateKeyFromString(string keyId, string keyString)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyId);
        var newVersion = NewKeyVersionString();
        AddKeyFromString(keyId, newVersion, keyString);
        SetCurrentVersion(keyId, newVersion);
        return newVersion;
    }

    public Task<string> UpdateKeyFromStringAsync(string keyId, string keyString, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(UpdateKeyFromString(keyId, keyString));
    }

    /// <summary>All available key versions for a given key ID.</summary>
    /// <param name="keyId">Key identifier</param>
    /// <returns>Collection of available key versions</returns>
    public IEnumerable<string> GetAvailableVersions(string keyId)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyId);
        if (!_keys.TryGetValue(keyId, out var versions))
            return Array.Empty<string>();

        return versions.Keys.OrderBy(v => v);
    }

    /// <summary>All available key IDs.</summary>
    public IEnumerable<string> GetAvailableKeyIds() => _keys.Keys.OrderBy(keyId => keyId);

    /// <summary>Removes a key version from the store.</summary>
    /// <param name="keyId">Key identifier</param>
    /// <param name="version">Key version to remove</param>
    /// <returns>True if the key was removed, false if it did not exist</returns>
    public bool RemoveKey(string keyId, string version)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyId);
        var currentVersion = GetCurrentVersion(keyId);
        OperationHelpers.ThrowIf(
            version == currentVersion && !string.IsNullOrWhiteSpace(currentVersion),
            $"Cannot remove the current key version for '{keyId}'. Set a different version as current first.");

        if (!_keys.TryGetValue(keyId, out var versions))
            return false;

        // Drop metadata and key
        if (_metadata.TryGetValue(keyId, out var metadataDict))
            metadataDict.TryRemove(version, out var _);

        return versions.TryRemove(version, out var _);
    }

    private static string NewKeyVersionString()
    {
#if NET10_0_OR_GREATER
        return Guid.CreateVersion7().ToString();
#else
        return Guid.NewGuid().ToString();
#endif
    }
}