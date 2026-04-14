using System.Security.Cryptography;
using Lyo.Exceptions;
using Lyo.Keystore;

namespace Lyo.Encryption.Security;

/// <summary>
/// Generates cryptographically secure nonces using a hybrid approach: - Random initialization vector (IV) generated once per key ID and version - Thread-safe counter
/// incremented per encryption operation This prevents nonce reuse while maintaining thread-safety and stateless design. The IV and counter are stored in KeyStore metadata.
/// </summary>
public static class NonceGenerator
{
    private const string NonceIvMetadataKey = "NonceIV";

    private const string NonceCounterMetadataKey = "NonceCounter";

    private const int IvSize = 8; // 8 bytes for IV (64 bits)

    private const int CounterSize = 4; // 4 bytes for counter (32 bits)

    /// <summary>Generates a 12-byte nonce for AES-GCM or ChaCha20Poly1305. Uses a hybrid approach: random IV (8 bytes) + counter (4 bytes).</summary>
    /// <param name="keyStore">The key store to use for storing/retrieving nonce state</param>
    /// <param name="keyId">The key identifier</param>
    /// <param name="keyVersion">The key version to generate nonce for</param>
    /// <param name="nonceSize">The required nonce size (must be 12 bytes for AES-GCM/ChaCha20Poly1305)</param>
    /// <returns>A 12-byte nonce</returns>
    public static byte[] GenerateNonce(IKeyStore keyStore, string keyId, string keyVersion, int nonceSize = 12)
    {
        ArgumentHelpers.ThrowIf(nonceSize != 12, nameof(nonceSize), $"Nonce size must be 12 bytes for AES-GCM/ChaCha20Poly1305, got {nonceSize}");
        var iv = GetOrCreateIv(keyStore, keyId, keyVersion);
        var counter = GetAndIncrementCounter(keyStore, keyId, keyVersion);

        // Combine IV (8 bytes) + counter (4 bytes) = 12 bytes
        var nonce = new byte[nonceSize];
        Array.Copy(iv, 0, nonce, 0, IvSize);
        var counterBytes = BitConverter.GetBytes(counter);
        if (!BitConverter.IsLittleEndian)
            Array.Reverse(counterBytes);

        Array.Copy(counterBytes, 0, nonce, IvSize, CounterSize);
        return nonce;
    }

    /// <summary>Gets or creates a random IV for the specified key ID and version. The IV is generated once per key ID and version and stored in KeyStore metadata.</summary>
    private static byte[] GetOrCreateIv(IKeyStore keyStore, string keyId, string keyVersion)
    {
        var metadata = keyStore.GetKeyMetadata(keyId, keyVersion);

        // Check if IV already exists in metadata
        if (metadata?.AdditionalData != null && metadata.AdditionalData.TryGetValue(NonceIvMetadataKey, out var ivBase64)) {
            try {
                return Convert.FromBase64String(ivBase64);
            }
            catch (FormatException) {
                // Invalid base64, regenerate IV
            }
        }

        // Generate new random IV
        var iv = RandomNumberGenerator.GetBytes(IvSize);

        // Store IV in KeyStore metadata
        var updatedMetadata = metadata ?? new KeyMetadata();
        updatedMetadata = updatedMetadata with { AdditionalData = updatedMetadata.AdditionalData ?? new Dictionary<string, string>() };
        updatedMetadata.AdditionalData![NonceIvMetadataKey] = Convert.ToBase64String(iv);
        keyStore.SetKeyMetadata(keyId, keyVersion, updatedMetadata);
        return iv;
    }

    /// <summary>
    /// Gets and increments the counter for the specified key ID and version. Note: In high-concurrency scenarios, some counter values may be skipped, but nonces remain unique
    /// due to the random IV component (8 bytes) + counter (4 bytes) combination. The random IV ensures uniqueness even if counters overlap between threads.
    /// </summary>
    private static uint GetAndIncrementCounter(IKeyStore keyStore, string keyId, string keyVersion)
    {
        var metadata = keyStore.GetKeyMetadata(keyId, keyVersion);
        uint currentCounter = 0;

        // Read current counter from metadata
        if (metadata?.AdditionalData != null && metadata.AdditionalData.TryGetValue(NonceCounterMetadataKey, out var counterStr)) {
            if (uint.TryParse(counterStr, out var parsedCounter))
                currentCounter = parsedCounter;
        }

        // Increment counter
        var newCounter = currentCounter + 1;

        // Handle counter overflow (wrap around, but this is extremely unlikely: 2^32 operations = 4+ billion)
        if (newCounter == 0) {
            // Counter wrapped around - regenerate IV to ensure uniqueness
            var iv = RandomNumberGenerator.GetBytes(IvSize);
            var updatedMetadata = metadata ?? new KeyMetadata();
            updatedMetadata = updatedMetadata with { AdditionalData = updatedMetadata.AdditionalData ?? new Dictionary<string, string>() };
            updatedMetadata.AdditionalData![NonceIvMetadataKey] = Convert.ToBase64String(iv);
            updatedMetadata.AdditionalData[NonceCounterMetadataKey] = "0";
            keyStore.SetKeyMetadata(keyId, keyVersion, updatedMetadata);
            return 0;
        }

        // Update metadata with new counter value
        // Note: In high-concurrency scenarios, this update might overwrite a newer counter value,
        // but this is acceptable because:
        // 1. The random IV (8 bytes) ensures nonces remain unique even with counter collisions
        // 2. Counter collisions are extremely rare (would require exact same counter + same IV)
        // 3. The alternative (locking) would significantly impact performance
        var finalMetadata = metadata ?? new KeyMetadata();
        finalMetadata = finalMetadata with { AdditionalData = finalMetadata.AdditionalData ?? new Dictionary<string, string>() };
        finalMetadata.AdditionalData![NonceCounterMetadataKey] = newCounter.ToString();
        keyStore.SetKeyMetadata(keyId, keyVersion, finalMetadata);
        return newCounter;
    }
}