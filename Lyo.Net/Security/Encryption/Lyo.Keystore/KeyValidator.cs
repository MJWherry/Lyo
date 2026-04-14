using Lyo.Keystore.Exceptions;

namespace Lyo.Keystore;

/// <summary> Utilities for validating encryption keys. </summary>
public static class KeyValidator
{
    /// <summary> Validates that a key has the expected size. </summary>
    /// <param name="key">The key to validate</param>
    /// <param name="expectedSize">The expected size in bytes</param>
    /// <returns>True if the key size matches, false otherwise</returns>
    public static bool IsValidKeySize(byte[]? key, int expectedSize) => key != null && key.Length == expectedSize;

    /// <summary> Validates that a key has the expected size, throwing an exception if invalid. </summary>
    /// <param name="key">The key to validate</param>
    /// <param name="expectedSize">The expected size in bytes</param>
    /// <exception cref="ArgumentNullException">Thrown when key is null</exception>
    /// <exception cref="InvalidKeyException">Thrown when key size doesn't match</exception>
    public static void ValidateKeySizeOrThrow(byte[] key, int expectedSize)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));
        if (key.Length != expectedSize)
            throw new InvalidKeyException($"Key size ({key.Length} bytes) does not match expected size ({expectedSize} bytes).");
    }

    /// <summary>Checks if a key appears to have sufficient entropy (randomness). This is a basic check - a key with all zeros or repeating patterns is considered weak.</summary>
    /// <param name="key">The key to check</param>
    /// <returns>True if the key appears strong, false if it appears weak</returns>
    public static bool IsStrongKey(byte[]? key)
    {
        if (key == null || key.Length == 0)
            return false;

        // Check for all zeros
        if (key.All(b => b == 0))
            return false;

        // Check for all same value
        var firstByte = key[0];
        if (key.All(b => b == firstByte))
            return false;

        // Check for simple patterns (every other byte same)
        var hasPattern = true;
        for (var i = 0; i < key.Length - 1; i += 2) {
            if (key[i] == key[i + 1])
                continue;

            hasPattern = false;
            break;
        }

        if (hasPattern && key.Length > 2)
            return false;

        // Basic entropy check: count unique bytes
        var uniqueBytes = key.Distinct().Count();
        return uniqueBytes >= key.Length / 4; // Less than 25% unique bytes suggests low entropy
    }

    /// <summary> Validates that a key is strong, throwing an exception if it's weak. </summary>
    /// <param name="key">The key to validate</param>
    /// <exception cref="ArgumentNullException">Thrown when key is null</exception>
    /// <exception cref="InvalidKeyException">Thrown when key appears weak</exception>
    public static void ValidateKeyStrengthOrThrow(byte[]? key)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));
        if (!IsStrongKey(key))
            throw new InvalidKeyException("Key appears to be weak (low entropy). Use a cryptographically secure random key generator.");
    }

    /// <summary> Validates a key for both size and strength. </summary>
    /// <param name="key">The key to validate</param>
    /// <param name="expectedSize">The expected size in bytes</param>
    /// <exception cref="ArgumentNullException">Thrown when key is null</exception>
    /// <exception cref="InvalidKeyException">Thrown when key is invalid</exception>
    public static void ValidateKeyOrThrow(byte[] key, int expectedSize)
    {
        ValidateKeySizeOrThrow(key, expectedSize);
        ValidateKeyStrengthOrThrow(key);
    }
}