using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Lyo.Exceptions;
using Lyo.Keystore.KeyDerivation;

namespace Lyo.Keystore.Aws;

/// <summary>AWS Secrets Manager implementation of IKeyStore. Stores Key Encryption Keys (KEK) in AWS Secrets Manager with key ID and versioning support.</summary>
public class AwsKeyStore : IKeyStore, IKeyInventoryStore
{
    private readonly ConcurrentDictionary<string, string?> _cachedCurrentVersions = new();

    private readonly SemaphoreSlim _cacheLock = new(1, 1);

    private readonly string _secretNamePrefix;
    private readonly IAmazonSecretsManager _secretsManager;

    /// <summary> Initializes a new instance of the AwsKeyStore class. </summary>
    /// <param name="secretsManager">The AWS Secrets Manager client</param>
    /// <param name="secretNamePrefix">Prefix for secret names (e.g., "myapp/kek")</param>
    public AwsKeyStore(IAmazonSecretsManager secretsManager, string secretNamePrefix = "lyo/kek")
    {
        ArgumentHelpers.ThrowIfNull(secretsManager, nameof(secretsManager));
        ArgumentHelpers.ThrowIfNull(secretNamePrefix, nameof(secretNamePrefix));
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
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyId, nameof(keyId));
        var versions = await GetVersionMappingAsync(keyId, ct).ConfigureAwait(false);
        if (versions?.Count > 0)
            return versions.Keys.OrderBy(version => version).ToList();

        var currentVersion = await GetCurrentVersionAsync(keyId, ct).ConfigureAwait(false);
        return string.IsNullOrWhiteSpace(currentVersion) ? Array.Empty<string>() : [currentVersion];
    }

    public byte[]? GetKey(string keyId, string? version = null) => GetKeyAsync(keyId, version).GetAwaiter().GetResult();

    public async Task<byte[]?> GetKeyAsync(string keyId, string? version = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyId, nameof(keyId));
        try {
            // Use the prefix as the secret name (e.g., "dev/CourtCanary/FileStore")
            var secretName = _secretNamePrefix;
            var request = new GetSecretValueRequest { SecretId = secretName };
            if (version != null) {
                // Specific version requested - get the AWS VersionId for it
                var awsVersionId = await GetAwsVersionIdAsync(keyId, version, ct).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(awsVersionId) && awsVersionId.Length >= 32)
                    request.VersionId = awsVersionId;
                else {
                    // Invalid version, return null
                    return null;
                }
            }
            else {
                // No version specified - use current version from our tracking or AWS default
                var currentVersion = await GetCurrentVersionAsync(keyId, ct).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(currentVersion) && currentVersion.Length >= 32) {
                    var awsVersionId = await GetAwsVersionIdAsync(keyId, currentVersion, ct).ConfigureAwait(false);
                    if (!string.IsNullOrEmpty(awsVersionId) && awsVersionId.Length >= 32)
                        request.VersionId = awsVersionId;
                    // If no valid version found, use AWSCURRENT stage (default)
                }
                else {
                    // No current version tracked, use AWSCURRENT stage (default)
                    request.VersionStage = "AWSCURRENT";
                }
            }

            var response = await _secretsManager.GetSecretValueAsync(request, ct).ConfigureAwait(false);

            // Parse JSON secret value to extract the key by keyId
            var secretJson = JsonSerializer.Deserialize<Dictionary<string, string>>(response.SecretString);
            if (secretJson == null || !secretJson.TryGetValue(keyId, out var keyValue))
                return null;

            // Determine the actual version being used
            var actualVersion = version ?? await GetCurrentVersionAsync(keyId, ct).ConfigureAwait(false);
            
            // Derive a proper key from the string value
            // This handles the case where keys are stored as random strings in AWS Secrets Manager
            // The derivation ensures we always get a proper 32-byte key for AES-GCM
            return await DeriveKeyFromStringAsync(keyId, actualVersion, keyValue, ct).ConfigureAwait(false);
        }
        catch (ResourceNotFoundException) {
            return null;
        }
    }

    public byte[]? GetCurrentKey(string keyId) => GetCurrentKeyAsync(keyId).GetAwaiter().GetResult();

    public async Task<byte[]?> GetCurrentKeyAsync(string keyId, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyId, nameof(keyId));
        return await GetKeyAsync(keyId, null, ct).ConfigureAwait(false);
    }

    public string? GetCurrentVersion(string keyId) => GetCurrentVersionAsync(keyId).GetAwaiter().GetResult();

    public async Task<string?> GetCurrentVersionAsync(string keyId, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyId, nameof(keyId));
        var version = await GetCurrentKeyVersionInternalAsync(keyId, ct).ConfigureAwait(false);

        // If no version is tracked, try to get it from the latest secret version
        if (!string.IsNullOrEmpty(version))
            return version;

        try {
            var secretName = _secretNamePrefix;
            var request = new GetSecretValueRequest { SecretId = secretName, VersionStage = "AWSCURRENT" };
            var response = await _secretsManager.GetSecretValueAsync(request, ct).ConfigureAwait(false);

            // Check if this version has the keyId in the JSON
            var secretJson = JsonSerializer.Deserialize<Dictionary<string, string>>(response.SecretString);
            if (secretJson != null && secretJson.ContainsKey(keyId)) {
                // Use the VersionId from the response as the current version
                if (!string.IsNullOrEmpty(response.VersionId) && response.VersionId.Length >= 32) {
                    // Store it as the current version for this keyId
                    await SetCurrentVersionAsync(keyId, response.VersionId, ct).ConfigureAwait(false);
                    return response.VersionId;
                }
            }
        }
        catch (ResourceNotFoundException) {
            // Secret doesn't exist
            return null;
        }

        return version;
    }

    public void AddKey(string keyId, string version, byte[] key) => AddKeyAsync(keyId, version, key).GetAwaiter().GetResult();

    public async Task AddKeyAsync(string keyId, string version, byte[] key, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyId, nameof(keyId));
        ArgumentHelpers.ThrowIfNullOrNotInRange(key, 1, long.MaxValue, nameof(key));

        // Use the prefix as the secret name (e.g., "dev/CourtCanary/FileStore")
        var secretName = _secretNamePrefix;
        var keyValue = Convert.ToBase64String(key);
        string awsVersionId;

        // Get current secret value (if exists) to preserve other keys
        Dictionary<string, string> secretJson = new();
        try {
            var getRequest = new GetSecretValueRequest { SecretId = secretName };
            var getResponse = await _secretsManager.GetSecretValueAsync(getRequest, ct).ConfigureAwait(false);
            var existingJson = JsonSerializer.Deserialize<Dictionary<string, string>>(getResponse.SecretString);
            if (existingJson != null)
                secretJson = existingJson;
        }
        catch (ResourceNotFoundException) {
            // Secret doesn't exist yet, will create it
        }

        // Update the specific key in the JSON
        secretJson[keyId] = keyValue;
        var secretValue = JsonSerializer.Serialize(secretJson);

        // Check if version mapping exists - if so, this version already exists
        var existingMapping = await GetVersionMappingAsync(keyId, ct).ConfigureAwait(false);
        if (existingMapping != null && existingMapping.TryGetValue(version, out var existingAwsVersionId)) {
            // Version already exists, update it
            var updateRequest = new UpdateSecretRequest { SecretId = secretName, SecretString = secretValue };
            var updateResponse = await _secretsManager.UpdateSecretAsync(updateRequest, ct).ConfigureAwait(false);
            awsVersionId = updateResponse.VersionId;

            // Update mapping if AWS generated a new version ID
            if (awsVersionId != existingAwsVersionId) {
                existingMapping[version] = awsVersionId;
                await StoreVersionMappingAsync(keyId, awsVersionId, version, ct).ConfigureAwait(false);
            }
        }
        else {
            // Version doesn't exist, create or update the secret
            try {
                // Try to update existing secret (creates new version)
                var updateRequest = new UpdateSecretRequest { SecretId = secretName, SecretString = secretValue };
                var updateResponse = await _secretsManager.UpdateSecretAsync(updateRequest, ct).ConfigureAwait(false);
                awsVersionId = updateResponse.VersionId;
            }
            catch (ResourceNotFoundException) {
                // Create new secret if it doesn't exist
                var createRequest = new CreateSecretRequest { Name = secretName, SecretString = secretValue, Description = "Key Encryption Keys" };
                var createResponse = await _secretsManager.CreateSecretAsync(createRequest, ct).ConfigureAwait(false);
                awsVersionId = createResponse.VersionId;
            }

            // Store version mapping: our version string -> AWS version ID
            await StoreVersionMappingAsync(keyId, awsVersionId, version, ct).ConfigureAwait(false);
        }

        // Initialize metadata if not exists
        var existingMetadata = await GetKeyMetadataAsync(keyId, version, ct).ConfigureAwait(false);
        if (existingMetadata == null)
            await SetKeyMetadataAsync(keyId, version, new() { CreatedAt = DateTime.UtcNow }, ct).ConfigureAwait(false);

        // Set as current if it's the first version for this key ID
        var currentVersion = await GetCurrentVersionAsync(keyId, ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(currentVersion))
            await SetCurrentVersionAsync(keyId, version, ct).ConfigureAwait(false);
    }

    public void AddKeyFromString(string keyId, string version, string keyString) => AddKeyFromStringAsync(keyId, version, keyString).GetAwaiter().GetResult();

    public async Task AddKeyFromStringAsync(string keyId, string version, string keyString, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyId, nameof(keyId));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyString, nameof(keyString));
        var derivationService = new Pbkdf2KeyDerivationService();

        // Check if key already exists - if so, don't regenerate it
        var existingMetadata = await GetKeyMetadataAsync(keyId, version, ct).ConfigureAwait(false);
        if (await HasKeyAsync(keyId, version, ct).ConfigureAwait(false)) {
            // Key already exists, just ensure metadata is set up if needed
            if (existingMetadata == null)
                await SetKeyMetadataAsync(keyId, version, new() { CreatedAt = DateTime.UtcNow }, ct).ConfigureAwait(false);

            return;
        }

        byte[]? salt = null;
        byte[] derivedKey;

        // Try to get salt from metadata (for persistent keystores that persist metadata)
        if (existingMetadata?.AdditionalData != null && existingMetadata.AdditionalData.TryGetValue("Pbkdf2Salt", out var saltBase64)) {
            try {
                salt = Convert.FromBase64String(saltBase64);
            }
            catch (FormatException) {
                // Invalid base64, fall through to deterministic salt
                salt = null;
            }
        }

        if (salt != null) {
            // Use existing salt from metadata (for persistent keystores)
            derivedKey = derivationService.DeriveKey(keyString, salt);
        }
        else {
            // No salt in metadata - use deterministic salt for consistency with LocalKeyStore
            // This ensures same password always produces same key, even after app restart
            // SECURITY NOTE: Deterministic salt reduces security but ensures consistency.
            // For AWS Secrets Manager, the keys are stored securely, so deterministic salt is acceptable.
            using var sha256 = SHA256.Create();
            var passwordHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(keyString));
            var deterministicSalt = new byte[32]; // Use 32-byte salt (256 bits) for better security
            Array.Copy(passwordHash, deterministicSalt, Math.Min(32, passwordHash.Length));
            // If passwordHash is shorter than 32 bytes, pad with additional hash iterations
            if (passwordHash.Length < 32) {
                var secondHash = sha256.ComputeHash(passwordHash);
                Array.Copy(secondHash, 0, deterministicSalt, passwordHash.Length, 32 - passwordHash.Length);
            }

            derivedKey = derivationService.DeriveKey(keyString, deterministicSalt);
            salt = deterministicSalt;
        }

        await AddKeyAsync(keyId, version, derivedKey, ct).ConfigureAwait(false);

        // Store salt in metadata (salt is also stored in FileStoreResult for persistence)
        var metadata = existingMetadata ?? new KeyMetadata { CreatedAt = DateTime.UtcNow };
        var additionalData = metadata.AdditionalData != null ? new(metadata.AdditionalData) : new Dictionary<string, string>();
        additionalData["Pbkdf2Salt"] = Convert.ToBase64String(salt);
        await SetKeyMetadataAsync(keyId, version, metadata with { AdditionalData = additionalData }, ct).ConfigureAwait(false);
    }

    public void SetCurrentVersion(string keyId, string version) => SetCurrentVersionAsync(keyId, version).GetAwaiter().GetResult();

    public async Task SetCurrentVersionAsync(string keyId, string version, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyId, nameof(keyId));
        if (!await HasKeyAsync(keyId, version, ct).ConfigureAwait(false))
            throw new InvalidOperationException($"Key '{keyId}' version {version} does not exist. Add the key before setting it as current.");

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
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyId, nameof(keyId));
        if (version == null) {
            // Check if any version exists for this key ID
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

            // Check if the keyId exists in the JSON secret value
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
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyId, nameof(keyId));
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
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyId, nameof(keyId));
        ArgumentNullException.ThrowIfNull(metadata);
        if (!await HasKeyAsync(keyId, version, ct).ConfigureAwait(false))
            throw new InvalidOperationException($"Key '{keyId}' version {version} does not exist. Add the key before setting metadata.");

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
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyId, nameof(keyId));
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
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyId, nameof(keyId));
        // //var currentVersion = await GetCurrentVersionAsync(keyId, ct).ConfigureAwait(false);
        var newVersion = Guid.CreateVersion7().ToString();
        await AddKeyAsync(keyId, newVersion, key, ct).ConfigureAwait(false);
        await SetCurrentVersionAsync(keyId, newVersion, ct).ConfigureAwait(false);
        return newVersion;
    }

    public string UpdateKeyFromString(string keyId, string keyString) => UpdateKeyFromStringAsync(keyId, keyString).GetAwaiter().GetResult();

    public async Task<string> UpdateKeyFromStringAsync(string keyId, string keyString, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyId, nameof(keyId));
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

        // Try to get salt from metadata if version is provided
        if (!string.IsNullOrEmpty(version)) {
            var metadata = await GetKeyMetadataAsync(keyId, version, ct).ConfigureAwait(false);
            if (metadata?.AdditionalData != null && metadata.AdditionalData.TryGetValue("Pbkdf2Salt", out var saltBase64)) {
                try {
                    salt = Convert.FromBase64String(saltBase64);
                }
                catch (FormatException) {
                    // Invalid base64, fall through to deterministic salt
                    salt = null;
                }
            }
        }

        if (salt != null) {
            // Use existing salt from metadata
            return derivationService.DeriveKey(keyString, salt);
        }

        // No salt in metadata - use deterministic salt for consistency
        // This ensures same password always produces same key, even after app restart
        using var sha256 = SHA256.Create();
        var passwordHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(keyString));
        var deterministicSalt = new byte[32]; // Use 32-byte salt (256 bits) for better security
        Array.Copy(passwordHash, deterministicSalt, Math.Min(32, passwordHash.Length));
        // If passwordHash is shorter than 32 bytes, pad with additional hash iterations
        if (passwordHash.Length < 32) {
            var secondHash = sha256.ComputeHash(passwordHash);
            Array.Copy(secondHash, 0, deterministicSalt, passwordHash.Length, 32 - passwordHash.Length);
        }

        return derivationService.DeriveKey(keyString, deterministicSalt);
    }

    private async Task<string?> GetCurrentKeyVersionInternalAsync(string keyId, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(keyId, nameof(keyId));
        await _cacheLock.WaitAsync(ct).ConfigureAwait(false);
        try {
            if (_cachedCurrentVersions.TryGetValue(keyId, out var cachedVersion) && !string.IsNullOrEmpty(cachedVersion)) {
                // Validate cached version format
                if (cachedVersion.Length >= 32)
                    return cachedVersion;

                // Invalid cached version, clear it and continue
                _cachedCurrentVersions[keyId] = null;
            }

            var currentVersionSecretName = GetCurrentVersionSecretName(keyId);
            try {
                var request = new GetSecretValueRequest { SecretId = currentVersionSecretName };
                var response = await _secretsManager.GetSecretValueAsync(request, ct).ConfigureAwait(false);
                var version = response.SecretString?.Trim();

                // Validate version format - must be at least 32 characters (UUID format)
                if (!string.IsNullOrEmpty(version) && version.Length >= 32) {
                    _cachedCurrentVersions[keyId] = version;
                    return version;
                }

                // Invalid version format, clear cache and return null
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
        // Validate version format - AWS VersionId must be at least 32 characters
        if (string.IsNullOrEmpty(ourVersion) || ourVersion.Length < 32)
            return null;

        // First check if we have a version mapping
        var mapping = await GetVersionMappingAsync(keyId, ct).ConfigureAwait(false);
        if (mapping != null && mapping.TryGetValue(ourVersion, out var awsVersionId)) {
            // Validate the mapped version ID
            if (!string.IsNullOrEmpty(awsVersionId) && awsVersionId.Length >= 32)
                return awsVersionId;
        }

        // If no mapping found, try using our version as the AWS VersionId directly
        // This handles the case where versions were created with the UUID as the VersionId
        // (e.g., if someone manually created versions or used a different system)
        try {
            var secretName = _secretNamePrefix;
            var testRequest = new GetSecretValueRequest { SecretId = secretName, VersionId = ourVersion };
            await _secretsManager.GetSecretValueAsync(testRequest, ct).ConfigureAwait(false);
            // If successful, our version IS the AWS VersionId
            return ourVersion;
        }
        catch (ResourceNotFoundException) {
            // Version doesn't exist, return null
            return null;
        }
        catch (AmazonSecretsManagerException ex) when (ex.Message.Contains("validation error")) {
            // Invalid version format, return null
            return null;
        }
    }

    private string GetMetadataSecretName(string keyId, string version)
    {
        var sanitizedKeyId = keyId.Replace('/', '-').Replace('\\', '-').Replace(' ', '-');

        // Store metadata in a separate secret with version suffix since metadata is small
        return $"{_secretNamePrefix}/{sanitizedKeyId}/v{version}/metadata";
    }

    private string GetCurrentVersionSecretName(string keyId)
    {
        var sanitizedKeyId = keyId.Replace('/', '-').Replace('\\', '-').Replace(' ', '-');
        return $"{_secretNamePrefix}/{sanitizedKeyId}/current-version";
    }
}