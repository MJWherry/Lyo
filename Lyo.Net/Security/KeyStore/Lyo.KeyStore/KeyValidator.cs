using Lyo.Exceptions;
using Lyo.KeyStore.Exceptions;

namespace Lyo.KeyStore;

/// <summary>Helpers for validating encryption keys.</summary>
public static class KeyValidator
{
    /// <summary>True if a key has the expected size.</summary>
    /// <param name="key">Key to validate</param>
    /// <param name="expectedSize">Expected size in bytes</param>
    /// <returns>True if the key size matches, false otherwise</returns>
    public static bool IsValidKeySize(byte[]? key, int expectedSize) => key != null && key.Length == expectedSize;

    /// <summary>Checks that a key has the expected size, throwing if it does not.</summary>
    /// <param name="key">Key to validate</param>
    /// <param name="expectedSize">Expected size in bytes</param>
    /// <exception cref="ArgumentNullException">Thrown when key is null</exception>
    /// <exception cref="InvalidKeyException">Thrown when key size does not match</exception>
    public static void ValidateKeySizeOrThrow(byte[] key, int expectedSize)
    {
        ArgumentHelpers.ThrowIfNull(key);
        if (key.Length != expectedSize)
            throw new InvalidKeyException($"Key size ({key.Length} bytes) does not match expected size ({expectedSize} bytes).");
    }

    /// <summary>Basic entropy check — a key of all zeros or repeating patterns is treated as weak.</summary>
    /// <param name="key">Key to check</param>
    /// <returns>True if the key looks strong, false if it looks weak</returns>
    public static bool IsStrongKey(byte[]? key)
    {
        if (key == null || key.Length == 0)
            return false;

        // Check for all zeros
        if (key.All(b => b == 0))
            return false;

        // Check for all the same value
        var firstByte = key[0];
        if (key.All(b => b == firstByte))
            return false;

        // Check for simple patterns (every other byte the same)
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

    /// <summary>Checks that a key is strong, throwing if it looks weak.</summary>
    /// <param name="key">Key to validate</param>
    /// <exception cref="ArgumentNullException">Thrown when key is null</exception>
    /// <exception cref="InvalidKeyException">Thrown when the key looks weak</exception>
    public static void ValidateKeyStrengthOrThrow(byte[]? key)
    {
        ArgumentHelpers.ThrowIfNull(key);
        if (!IsStrongKey(key))
            throw new InvalidKeyException("Key appears to be weak (low entropy). Use a cryptographically secure random key generator.");
    }

    /// <summary>Validates a key for both size and strength.</summary>
    /// <param name="key">Key to validate</param>
    /// <param name="expectedSize">Expected size in bytes</param>
    /// <exception cref="ArgumentNullException">Thrown when key is null</exception>
    /// <exception cref="InvalidKeyException">Thrown when the key is invalid</exception>
    public static void ValidateKeyOrThrow(byte[] key, int expectedSize)
    {
        ValidateKeySizeOrThrow(key, expectedSize);
        ValidateKeyStrengthOrThrow(key);
    }
}