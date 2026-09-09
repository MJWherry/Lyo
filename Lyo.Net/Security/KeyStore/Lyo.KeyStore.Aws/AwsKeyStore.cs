using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Lyo.Exceptions;
using Lyo.KeyStore.KeyDerivation;

namespace Lyo.KeyStore.Aws;

/// <summary>AWS Secrets Manager <see cref="IKeyStore" />. Stores Key Encryption Keys (KEK) in Secrets Manager with key ID and versioning.</summary>
public class AwsKeyStore : IKeyStore, IKeyInventoryStore
{
    private readonly SemaphoreSlim _cacheLock = new(1, 1);
    private readonly ConcurrentDictionary<string, string?> _cachedCurrentVersions = new();

    private readonly string _secretNamePrefix;
    private readonly IAmazonSecretsManager _secretsManager;

    /// <summary>Builds a new AwsKeyStore.</summary>
    /// <param name="secretsManager">AWS Secrets Manager client</param>
    /// <param name="secretNamePrefix">Prefix for secret names (e.g., "myapp/kek")</param>
    public AwsKeyStore(IAmazonSecretsManager secretsManager, string secretNamePrefix = "lyo/kek")
    {
        ArgumentHelpers.ThrowIfNull(secretsManager);
        ArgumentHelpers.ThrowIfNull(secretNamePrefix);
        _secretsManager = secretsManager;
        _secretNamePrefix = secretNamePrefix;
    }

    public async Task<IReadOnlyList<string>> GetAvailableKeyIdsAsync(CancellationToken ct = default)
    {
        try {
            var response = await _secretsManager.GetSecretValueAsync(new() { SecretId = _secretNamePrefix }, ct).ConfigureAwait(false);
            var secretJson = JsonSerializer.Deserialize<Dictionary<string, string>>(response.SecretString);
            var keyIds = (IEnumerable<string>?)secretJson?.Keys ?? [];
            return keyIds.OrderBy(keyId => keyId).ToList();
        }
        catch (ResourceNotFoundException) {
            return [];
        }
    }

    public async Task<IReadOnlyList<string>> GetAvailableVersionsAsync(string keyId, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyId);
        var versions = await GetVersionMappingAsync(keyId, ct).ConfigureAwait(false);
        if (versions?.Count > 0)
            return versions.Keys.OrderBy(version => version).ToList();

        var currentVersion = await GetCurrentVersionAsync(keyId, ct).ConfigureAwait(false);
        return string.IsNullOrWhiteSpace(currentVersion) ? Array.Empty<string>() : [currentVersion];
    }

    public byte[]? GetKey(string keyId, string? version = null) => GetKeyAsync(keyId, version).GetAwaiter().GetResult();

    public async Task<byte[]?> GetKeyAsync(string keyId, string? version = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyId);
        try {
            // Use the prefix as the secret name (e.g., "dev/FileStore")
            var secretName = _secretNamePrefix;
            var request = new GetSecretValueRequest { SecretId = secretName };
            if (version != null) {
                // Specific version requested — resolve its AWS VersionId
                var awsVersionId = await GetAwsVersionIdAsync(keyId, version, ct).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(awsVersionId) && awsVersionId.Length >= 32)
                    request.VersionId = awsVersionId;
                else {
                    // Invalid version — return null
                    return null;
                }
            }
            else {
                // No version specified — use the current version from tracking or the AWS default
                var currentVersion = await GetCurrentVersionAsync(keyId, ct).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(currentVersion) && currentVersion.Length >= 32) {
                    var awsVersionId = await GetAwsVersionIdAsync(keyId, currentVersion, ct).ConfigureAwait(false);
                    if (!string.IsNullOrEmpty(awsVersionId) && awsVersionId.Length >= 32)
                        request.VersionId = awsVersionId;
                    // If no valid version is found, use the AWSCURRENT stage (default)
                }
                else {
                    // No current version tracked — use the AWSCURRENT stage (default)
                    request.VersionStage = "AWSCURRENT";
                }
            }

            var response = await _secretsManager.GetSecretValueAsync(request, ct).ConfigureAwait(false);

            // Parse the JSON secret value and pull the key by keyId
            var secretJson = JsonSerializer.Deserialize<Dictionary<string, string>>(response.SecretString);
            if (secretJson == null || !secretJson.TryGetValue(keyId, out var keyValue))
                return null;

            // Figure out which version is actually in use
            var actualVersion = version ?? await GetCurrentVersionAsync(keyId, ct).ConfigureAwait(false);

            // Derive a proper key from the string value
            // Covers keys stored as random strings in AWS Secrets Manager
            // Derivation always yields a proper 32-byte key for AES-GCM
            return await DeriveKeyFromStringAsync(keyId, actualVersion, keyValue, ct).ConfigureAwait(false);
        }
        catch (ResourceNotFoundException) {
            return null;
        }
    }

    public byte[]? GetCurrentKey(string keyId) => GetCurrentKeyAsync(keyId).GetAwaiter().GetResult();

    public async Task<byte[]?> GetCurrentKeyAsync(string keyId, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyId);
        return await GetKeyAsync(keyId, null, ct).ConfigureAwait(false);
    }

    public string? GetCurrentVersion(string keyId) => GetCurrentVersionAsync(keyId).GetAwaiter().GetResult();

    public async Task<string?> GetCurrentVersionAsync(string keyId, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyId);
        var version = await GetCurrentKeyVersionInternalAsync(keyId, ct).ConfigureAwait(false);

        // If no version is tracked, try the latest secret version
        if (!string.IsNullOrEmpty(version))
            return version;

        try {
            var secretName = _secretNamePrefix;
            var request = new GetSecretValueRequest { SecretId = secretName, VersionStage = "AWSCURRENT" };
            var response = await _secretsManager.GetSecretValueAsync(request, ct).ConfigureAwait(false);

            // Check whether this version has the keyId in the JSON
            var secretJson = JsonSerializer.Deserialize<Dictionary<string, string>>(response.SecretString);
            if (secretJson != null && secretJson.ContainsKey(keyId)) {
                // Use the VersionId from the response as the current version
                if (!string.IsNullOrEmpty(response.VersionId) && response.VersionId.Length >= 32) {
                    // Remember it as the current version for this keyId
                    await SetCurrentVersionAsync(keyId, response.VersionId, ct).ConfigureAwait(false);
                    return response.VersionId;
                }
            }
        }
        catch (ResourceNotFoundException) {
            // Secret does not exist
            return null;
        }

        return version;
    }

    public void AddKey(string keyId, string version, byte[] key) => AddKeyAsync(keyId, version, key).GetAwaiter().GetResult();

    public async Task AddKeyAsync(string keyId, string version, byte[] key, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyId);
        ArgumentHelpers.ThrowIfNullOrNotInRange(key, 1, long.MaxValue);

        // Use the prefix as the secret name (e.g., "dev/FileStore")
        var secretName = _secretNamePrefix;
        var keyValue = Convert.ToBase64String(key);
        string awsVersionId;

        // Load the current secret value (if it exists) so other keys are kept
        Dictionary<string, string> secretJson = new();
        try {
            var getRequest = new GetSecretValueRequest { SecretId = secretName };
            var getResponse = await _secretsManager.GetSecretValueAsync(getRequest, ct).ConfigureAwait(false);
            var existingJson = JsonSerializer.Deserialize<Dictionary<string, string>>(getResponse.SecretString);
            if (existingJson != null)
                secretJson = existingJson;
        }
        catch (ResourceNotFoundException) {
            // Secret does not exist yet — it will be created
        }

        // Update this key in the JSON
        secretJson[keyId] = keyValue;
        var secretValue = JsonSerializer.Serialize(secretJson);

        // If a version mapping exists, this version is already present
        var existingMapping = await GetVersionMappingAsync(keyId, ct).ConfigureAwait(false);
        if (existingMapping != null && existingMapping.TryGetValue(version, out var existingAwsVersionId)) {
            // Version already exists — update it
            var updateRequest = new UpdateSecretRequest { SecretId = secretName, SecretString = secretValue };
            var updateResponse = await _secretsManager.UpdateSecretAsync(updateRequest, ct).ConfigureAwait(false);
            awsVersionId = updateResponse.VersionId;

            // Update the mapping if AWS generated a new version ID
            if (awsVersionId != existingAwsVersionId) {
                existingMapping[version] = awsVersionId;
                await StoreVersionMappingAsync(keyId, awsVersionId, version, ct).ConfigureAwait(false);
            }
        }
        else {
            // Version does not exist — create or update the secret
            try {
                // Try updating an existing secret (creates a new version)
                var updateRequest = new UpdateSecretRequest { SecretId = secretName, SecretString = secretValue };
                var updateResponse = await _secretsManager.UpdateSecretAsync(updateRequest, ct).ConfigureAwait(false);
                awsVersionId = updateResponse.VersionId;
            }
            catch (ResourceNotFoundException) {
                // Create a new secret if it does not exist
                var createRequest = new CreateSecretRequest { Name = secretName, SecretString = secretValue, Description = "Key Encryption Keys" };
                var createResponse = await _secretsManager.CreateSecretAsync(createRequest, ct).ConfigureAwait(false);
                awsVersionId = createResponse.VersionId;
            }

            // Store version mapping: our version string -> AWS version ID
            await StoreVersionMappingAsync(keyId, awsVersionId, version, ct).ConfigureAwait(false);
        }

        // Create metadata if missing
        var existingMetadata = await GetKeyMetadataAsync(keyId, version, ct).ConfigureAwait(false);
        if (existingMetadata == null)
            await SetKeyMetadataAsync(keyId, version, new() { CreatedAt = DateTime.UtcNow }, ct).ConfigureAwait(false);

        // Set as current when this is the first version for this key ID
        var currentVersion = await GetCurrentVersionAsync(keyId, ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(currentVersion))
            await SetCurrentVersionAsync(keyId, version, ct).ConfigureAwait(false);
    }

    public void AddKeyFromString(string keyId, string version, string keyString) => AddKeyFromStringAsync(keyId, version, keyString).GetAwaiter().GetResult();

    public async Task AddKeyFromStringAsync(string keyId, string version, string keyString, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyId);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyString);
        var derivationService = new Pbkdf2KeyDerivationService();

        // If the key already exists, do not regenerate it
        var existingMetadata = await GetKeyMetadataAsync(keyId, version, ct).ConfigureAwait(false);
        if (await HasKeyAsync(keyId, version, ct).ConfigureAwait(false)) {
            // Key already exists; just make sure metadata is in place if needed
            if (existingMetadata == null)
                await SetKeyMetadataAsync(keyId, version, new() { CreatedAt = DateTime.UtcNow }, ct).ConfigureAwait(false);

            return;
        }

        byte[]? salt = null;
        byte[] derivedKey;

        // Try metadata for a stored salt (persistent keystores that keep metadata)
        if (existingMetadata?.AdditionalData != null && existingMetadata.AdditionalData.TryGetValue("Pbkdf2Salt", out var saltBase64)) {
            try {
                salt = Convert.FromBase64String(saltBase64);
            }
            catch (FormatException) {
                // Invalid base64 — fall through to deterministic salt
                salt = null;
            }
        }

        if (salt != null) {
            // Use the salt already in metadata (persistent keystores)
            derivedKey = derivationService.DeriveKey(keyString, salt);
        }
        else {
            // No salt in metadata — use deterministic salt to match LocalKeyStore
            // Same password always yields the same key, even after an app restart
            // SECURITY NOTE: Deterministic salt is weaker but keeps results consistent.
            // For AWS Secrets Manager the keys are stored securely, so deterministic salt is acceptable.
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

        await AddKeyAsync(keyId, version, derivedKey, ct).ConfigureAwait(false);

        // Persist salt in metadata (also stored in FileStoreResult)
        var metadata = existingMetadata ?? new KeyMetadata { CreatedAt = DateTime.UtcNow };
        var additionalData = metadata.AdditionalData != null ? new(metadata.AdditionalData) : new Dictionary<string, string>();
        additionalData["Pbkdf2Salt"] = Convert.ToBase64String(salt);
        await SetKeyMetadataAsync(keyId, version, metadata with { AdditionalData = additionalData }, ct).ConfigureAwait(false);
    }

    public void SetCurrentVersion(string keyId, string version) => SetCurrentVersionAsync(keyId, version).GetAwaiter().GetResult();

    public async Task SetCurrentVersionAsync(string keyId, string version, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyId);
        var hasKeyForCurrent = await HasKeyAsync(keyId, version, ct).ConfigureAwait(false);
        OperationHelpers.ThrowIf(!hasKeyForCurrent, $"Key '{keyId}' version {version} does not exist. Add the key before setting it as current.");
        var versionString = version;
        var currentVersionSecretName = GetCurrentVersionSecretName(keyId);
        try {
            var updateRequest = new UpdateSecretRequest { SecretId = currentVersionSecretName, SecretString = versionString };
            await _secretsManager.UpdateSecretAsync(updateRequest, ct).ConfigureAwait(false);
        }
        catch (ResourceNotFoundException) {
            var createRequest = new CreateSecretRequest { Name = currentVersionSecretName, SecretString = versionString, Description = $"Current KEK version for '{keyId}'" };
            await _secretsManager.CreateSecretAsync(createRequest, ct).ConfigureAwait(false);
        }

        await _cacheLock.WaitAsync(ct).ConfigureAwait(false);
        try {
            _cachedCurrentVersions[keyId] = version;
        }
        finally {
            _cacheLock.Release();
        }
    }

    public bool HasKey(string keyId, string? version = null) => HasKeyAsync(keyId, version).GetAwaiter().GetResult();

    public async Task<bool> HasKeyAsync(string keyId, string? version = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyId);
        if (version == null) {
            // Check whether any version exists for this key ID
            var currentVersion = await GetCurrentVersionAsync(keyId, ct).ConfigureAwait(false);
            return !string.IsNullOrWhiteSpace(currentVersion);
        }

        try {
            var secretName = _secretNamePrefix;
            var awsVersionId = await GetAwsVersionIdAsync(keyId, version, ct).ConfigureAwait(false);
            if (string.IsNullOrEmpty(awsVersionId))
                return false;

            var request = new GetSecretValueRequest { SecretId = secretName, VersionId = awsVersionId };
            var response = await _secretsManager.GetSecretValueAsync(request, ct).ConfigureAwait(false);

            // Check whether the keyId exists in the JSON secret value
            var secretJson = JsonSerializer.Deserialize<Dictionary<string, string>>(response.SecretString);
            return secretJson != null && secretJson.ContainsKey(keyId);
        }
        catch (ResourceNotFoundException) {
            return false;
        }
    }

    public KeyMetadata? GetKeyMetadata(string keyId, string version) => GetKeyMetadataAsync(keyId, version).GetAwaiter().GetResult();

    public async Task<KeyMetadata?> GetKeyMetadataAsync(string keyId, string version, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyId);
        try {
            var metadataSecretName = GetMetadataSecretName(keyId, version);
            var request = new GetSecretValueRequest { SecretId = metadataSecretName };
            var response = await _secretsManager.GetSecretValueAsync(request, ct).ConfigureAwait(false);
            return JsonSerializer.Deserialize<KeyMetadata>(response.SecretString);
        }
        catch (ResourceNotFoundException) {
            return null;
        }
    }

    public void SetKeyMetadata(string keyId, string version, KeyMetadata metadata) => SetKeyMetadataAsync(keyId, version, metadata).GetAwaiter().GetResult();

    public async Task SetKeyMetadataAsync(string keyId, string version, KeyMetadata metadata, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyId);
        ArgumentHelpers.ThrowIfNull(metadata);
        var hasKeyForMetadata = await HasKeyAsync(keyId, version, ct).ConfigureAwait(false);
        OperationHelpers.ThrowIf(!hasKeyForMetadata, $"Key '{keyId}' version {version} does not exist. Add the key before setting metadata.");
        var metadataSecretName = GetMetadataSecretName(keyId, version);
        var metadataJson = JsonSerializer.Serialize(metadata);
        try {
            var updateRequest = new UpdateSecretRequest { SecretId = metadataSecretName, SecretString = metadataJson };
            await _secretsManager.UpdateSecretAsync(updateRequest, ct).ConfigureAwait(false);
        }
        catch (ResourceNotFoundException) {
            var createRequest = new CreateSecretRequest { Name = metadataSecretName, SecretString = metadataJson, Description = $"Metadata for KEK '{keyId}' version {version}" };
            await _secretsManager.CreateSecretAsync(createRequest, ct).ConfigureAwait(false);
        }
    }

    public byte[]? GetSaltForVersion(string keyId, string version) => GetSaltForVersionAsync(keyId, version).GetAwaiter().GetResult();

    public async Task<byte[]?> GetSaltForVersionAsync(string keyId, string version, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyId);
        var metadata = await GetKeyMetadataAsync(keyId, version, ct).ConfigureAwait(false);
        if (metadata?.AdditionalData == null || !metadata.AdditionalData.TryGetValue("Pbkdf2Salt", out var saltBase64))
            return null;

        try {
            return Convert.FromBase64String(saltBase64);
        }
        catch (FormatException) {
            return null;
        }
    }

    public string UpdateKey(string keyId, byte[] key) => UpdateKeyAsync(keyId, key).GetAwaiter().GetResult();

    public async Task<string> UpdateKeyAsync(string keyId, byte[] key, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyId);
        // //var currentVersion = await GetCurrentVersionAsync(keyId, ct).ConfigureAwait(false);
        var newVersion = Guid.CreateVersion7().ToString();
        await AddKeyAsync(keyId, newVersion, key, ct).ConfigureAwait(false);
        await SetCurrentVersionAsync(keyId, newVersion, ct).ConfigureAwait(false);
        return newVersion;
    }

    public string UpdateKeyFromString(string keyId, string keyString) => UpdateKeyFromStringAsync(keyId, keyString).GetAwaiter().GetResult();

    public async Task<string> UpdateKeyFromStringAsync(string keyId, string keyString, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyId);
        //var currentVersion = await GetCurrentVersionAsync(keyId, ct).ConfigureAwait(false);
        var newVersion = Guid.CreateVersion7().ToString();
        await AddKeyFromStringAsync(keyId, newVersion, keyString, ct).ConfigureAwait(false);
        await SetCurrentVersionAsync(keyId, newVersion, ct).ConfigureAwait(false);
        return newVersion;
    }

    private async Task<byte[]> DeriveKeyFromStringAsync(string keyId, string? version, string keyString, CancellationToken ct)
    {
        var derivationService = new Pbkdf2KeyDerivationService();
        byte[]? salt = null;

        // Try metadata for a salt when a version is provided
        if (!string.IsNullOrEmpty(version)) {
            var metadata = await GetKeyMetadataAsync(keyId, version, ct).ConfigureAwait(false);
            if (metadata?.AdditionalData != null && metadata.AdditionalData.TryGetValue("Pbkdf2Salt", out var saltBase64)) {
                try {
                    salt = Convert.FromBase64String(saltBase64);
                }
                catch (FormatException) {
                    // Invalid base64 — fall through to deterministic salt
                    salt = null;
                }
            }
        }

        if (salt != null) {
            // Use the salt already in metadata
            return derivationService.DeriveKey(keyString, salt);
        }

        // No salt in metadata — use deterministic salt for consistency
        // Same password always yields the same key, even after an app restart
        using var sha256 = SHA256.Create();
        var passwordHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(keyString));
        var deterministicSalt = new byte[32]; // 32-byte salt (256 bits) for better security
        Array.Copy(passwordHash, deterministicSalt, Math.Min(32, passwordHash.Length));
        // If passwordHash is shorter than 32 bytes, pad with extra hash iterations
        if (passwordHash.Length < 32) {
            var secondHash = sha256.ComputeHash(passwordHash);
            Array.Copy(secondHash, 0, deterministicSalt, passwordHash.Length, 32 - passwordHash.Length);
        }

        return derivationService.DeriveKey(keyString, deterministicSalt);
    }

    private async Task<string?> GetCurrentKeyVersionInternalAsync(string keyId, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyId);
        await _cacheLock.WaitAsync(ct).ConfigureAwait(false);
        try {
            if (_cachedCurrentVersions.TryGetValue(keyId, out var cachedVersion) && !string.IsNullOrEmpty(cachedVersion)) {
                // Validate the cached version format
                if (cachedVersion.Length >= 32)
                    return cachedVersion;

                // Invalid cached version — clear it and continue
                _cachedCurrentVersions[keyId] = null;
            }

            var currentVersionSecretName = GetCurrentVersionSecretName(keyId);
            try {
                var request = new GetSecretValueRequest { SecretId = currentVersionSecretName };
                var response = await _secretsManager.GetSecretValueAsync(request, ct).ConfigureAwait(false);
                var version = response.SecretString?.Trim();

                // Version format must be at least 32 characters (UUID)
                if (!string.IsNullOrEmpty(version) && version.Length >= 32) {
                    _cachedCurrentVersions[keyId] = version;
                    return version;
                }

                // Invalid version format — clear cache and return null
                _cachedCurrentVersions[keyId] = null;
                return null;
            }
            catch (ResourceNotFoundException) {
                _cachedCurrentVersions[keyId] = null;
                return null;
            }
        }
        finally {
            _cacheLock.Release();
        }
    }

    private string GetVersionMappingSecretName(string keyId)
    {
        // Store version mappings in a separate secret per keyId
        var sanitizedKeyId = keyId.Replace('/', '-').Replace('\\', '-').Replace(' ', '-');
        return $"{_secretNamePrefix}/{sanitizedKeyId}/version-mapping";
    }

    private async Task StoreVersionMappingAsync(string keyId, string awsVersionId, string ourVersion, CancellationToken ct)
    {
        var mappingSecretName = GetVersionMappingSecretName(keyId);
        Dictionary<string, string> existingMapping;
        try {
            var request = new GetSecretValueRequest { SecretId = mappingSecretName };
            var response = await _secretsManager.GetSecretValueAsync(request, ct).ConfigureAwait(false);
            existingMapping = JsonSerializer.Deserialize<Dictionary<string, string>>(response.SecretString) ?? new Dictionary<string, string>();
        }
        catch (ResourceNotFoundException) {
            existingMapping = new();
        }

        existingMapping[ourVersion] = awsVersionId;
        var mappingJson = JsonSerializer.Serialize(existingMapping);
        try {
            var updateRequest = new UpdateSecretRequest { SecretId = mappingSecretName, SecretString = mappingJson };
            await _secretsManager.UpdateSecretAsync(updateRequest, ct).ConfigureAwait(false);
        }
        catch (ResourceNotFoundException) {
            var createRequest = new CreateSecretRequest { Name = mappingSecretName, SecretString = mappingJson, Description = $"Version mapping for KEK '{keyId}'" };
            await _secretsManager.CreateSecretAsync(createRequest, ct).ConfigureAwait(false);
        }
    }

    private async Task<Dictionary<string, string>?> GetVersionMappingAsync(string keyId, CancellationToken ct)
    {
        var mappingSecretName = GetVersionMappingSecretName(keyId);
        try {
            var request = new GetSecretValueRequest { SecretId = mappingSecretName };
            var response = await _secretsManager.GetSecretValueAsync(request, ct).ConfigureAwait(false);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(response.SecretString);
        }
        catch (ResourceNotFoundException) {
            return null;
        }
    }

    private async Task<string?> GetAwsVersionIdAsync(string keyId, string ourVersion, CancellationToken ct)
    {
        // Version format — AWS VersionId must be at least 32 characters
        if (string.IsNullOrEmpty(ourVersion) || ourVersion.Length < 32)
            return null;

        // First check whether we have a version mapping
        var mapping = await GetVersionMappingAsync(keyId, ct).ConfigureAwait(false);
        if (mapping != null && mapping.TryGetValue(ourVersion, out var awsVersionId)) {
            // Validate the mapped version ID
            if (!string.IsNullOrEmpty(awsVersionId) && awsVersionId.Length >= 32)
                return awsVersionId;
        }

        // If no mapping is found, try using our version as the AWS VersionId directly
        // Covers versions created with the UUID as the VersionId
        // (e.g. if someone created versions by hand or used a different system)
        try {
            var secretName = _secretNamePrefix;
            var testRequest = new GetSecretValueRequest { SecretId = secretName, VersionId = ourVersion };
            await _secretsManager.GetSecretValueAsync(testRequest, ct).ConfigureAwait(false);
            // On success, our version IS the AWS VersionId
            return ourVersion;
        }
        catch (ResourceNotFoundException) {
            // Version does not exist — return null
            return null;
        }
        catch (AmazonSecretsManagerException ex) when (ex.Message.Contains("validation error")) {
            // Invalid version format — return null
            return null;
        }
    }

    private string GetMetadataSecretName(string keyId, string version)
    {
        var sanitizedKeyId = keyId.Replace('/', '-').Replace('\\', '-').Replace(' ', '-');

        // Store metadata in a separate secret with a version suffix (metadata is small)
        return $"{_secretNamePrefix}/{sanitizedKeyId}/v{version}/metadata";
    }

    private string GetCurrentVersionSecretName(string keyId)
    {
        var sanitizedKeyId = keyId.Replace('/', '-').Replace('\\', '-').Replace(' ', '-');
        return $"{_secretNamePrefix}/{sanitizedKeyId}/current-version";
    }
}