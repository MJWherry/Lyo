using System.Text;

namespace Lyo.Keystore.KeyDerivation;

/// <summary>Service for deriving encryption keys from passwords or other input material. Provides secure key derivation using PBKDF2 or other secure algorithms.</summary>
public interface IKeyDerivationService
{
    /// <summary>Gets the default salt size in bytes for this key derivation service. Each implementation defines its own salt size based on its algorithm's requirements.</summary>
    int DefaultSaltSize { get; }

    /// <summary>Gets the default key size in bytes for this key derivation service. Default is typically 32 bytes (256 bits).</summary>
    int DefaultKeySize { get; }

    /// <summary>Gets or sets the default encoding used for string-to-byte conversions. Defaults to UTF-8.</summary>
    Encoding DefaultEncoding { get; set; }

    /// <summary> Derives a key from a password string. </summary>
    /// <param name="password">The password to derive the key from</param>
    /// <param name="salt">Optional salt. If null, a random salt will be generated.</param>
    /// <param name="iterations">Number of PBKDF2 iterations. Default is 600,000 (OWASP 2023 recommendation).</param>
    /// <param name="keySizeBytes">Size of the derived key in bytes. If null, uses the implementation's DefaultKeySize property.</param>
    /// <param name="encoding">Optional encoding. If null, uses DefaultEncoding.</param>
    /// <returns>The derived key</returns>
    byte[] DeriveKey(string password, byte[]? salt = null, int iterations = 600000, int? keySizeBytes = null, Encoding? encoding = null);

    /// <summary> Derives a key from a password byte array. </summary>
    /// <param name="password">The password bytes to derive the key from</param>
    /// <param name="salt">Optional salt. If null, a random salt will be generated.</param>
    /// <param name="iterations">Number of PBKDF2 iterations. Default is 600,000 (OWASP 2023 recommendation).</param>
    /// <param name="keySizeBytes">Size of the derived key in bytes. If null, uses the implementation's DefaultKeySize property.</param>
    /// <returns>The derived key</returns>
    byte[] DeriveKey(byte[] password, byte[]? salt = null, int iterations = 600000, int? keySizeBytes = null);

    /// <summary> Derives a key from a password and returns both the key and salt. </summary>
    /// <param name="password">The password to derive the key from</param>
    /// <param name="iterations">Number of PBKDF2 iterations. Default is 600,000 (OWASP 2023 recommendation).</param>
    /// <param name="keySizeBytes">Size of the derived key in bytes. If null, uses the implementation's DefaultKeySize property.</param>
    /// <param name="encoding">Optional encoding. If null, uses DefaultEncoding.</param>
    /// <returns>A tuple containing the derived key and the salt used</returns>
    (byte[] Key, byte[] Salt) DeriveKeyWithSalt(string password, int iterations = 600000, int? keySizeBytes = null, Encoding? encoding = null);

    /// <summary> Derives a key from a password byte array and returns both the key and salt. </summary>
    /// <param name="password">The password bytes to derive the key from</param>
    /// <param name="iterations">Number of PBKDF2 iterations. Default is 600,000 (OWASP 2023 recommendation).</param>
    /// <param name="keySizeBytes">Size of the derived key in bytes. If null, uses the implementation's DefaultKeySize property.</param>
    /// <returns>A tuple containing the derived key and the salt used</returns>
    (byte[] Key, byte[] Salt) DeriveKeyWithSalt(byte[] password, int iterations = 600000, int? keySizeBytes = null);
}