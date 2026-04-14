using System.Security.Cryptography;
using System.Text;
using Lyo.Exceptions;

namespace Lyo.Keystore;

/// <summary> Utilities for generating secure encryption keys. </summary>
public static class SecureKeyGenerator
{
    /// <summary> Generates a cryptographically secure random key. </summary>
    /// <param name="sizeInBytes">Size of the key in bytes. Default is 32 (256 bits).</param>
    /// <returns>A securely generated random key</returns>
    public static byte[] GenerateKey(int sizeInBytes = 32)
    {
        ArgumentHelpers.ThrowIfNullOrNotInRange(sizeInBytes, 16, 64, nameof(sizeInBytes));
        return RandomNumberGenerator.GetBytes(sizeInBytes);
    }

    /// <summary> Generates a cryptographically secure random key string. </summary>
    /// <param name="length">Length of the key string. Default is 32 characters.</param>
    /// <param name="includeSpecialChars">Whether to include special characters. Default is true.</param>
    /// <returns>A securely generated random key string</returns>
    public static string GenerateKeyString(int length = 32, bool includeSpecialChars = true)
    {
        ArgumentHelpers.ThrowIfNullOrNotInRange(length, 16, int.MaxValue, nameof(length));
        const string alphanumeric = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        const string special = "!@#$%^&*()_+-=[]{}|;:,.<>?";
        var chars = includeSpecialChars ? alphanumeric + special : alphanumeric;
        var result = new StringBuilder(length);
        for (var i = 0; i < length; i++) {
            var randomIndex = RandomNumberGenerator.GetInt32(0, chars.Length);
            result.Append(chars[randomIndex]);
        }

        return result.ToString();
    }

    /// <summary> Generates a key with an associated salt for key derivation. </summary>
    /// <param name="sizeInBytes">Size of the key in bytes. Default is 32 (256 bits).</param>
    /// <param name="saltSizeInBytes">Size of the salt in bytes. Default is 16 (128 bits).</param>
    /// <returns>A tuple containing the generated key and salt</returns>
    public static (byte[] Key, byte[] Salt) GenerateKeyWithSalt(int sizeInBytes = 32, int saltSizeInBytes = 16)
    {
        var key = GenerateKey(sizeInBytes);
        var salt = RandomNumberGenerator.GetBytes(saltSizeInBytes);
        return (key, salt);
    }

    /// <summary> Generates a nonce (number used once) for encryption operations. </summary>
    /// <param name="sizeInBytes">Size of the nonce in bytes. Default is 12 (96 bits).</param>
    /// <returns>A securely generated random nonce</returns>
    public static byte[] GenerateNonce(int sizeInBytes = 12)
    {
        ArgumentHelpers.ThrowIfNullOrNotInRange(sizeInBytes, 8, int.MaxValue, nameof(sizeInBytes));
        return RandomNumberGenerator.GetBytes(sizeInBytes);
    }
}