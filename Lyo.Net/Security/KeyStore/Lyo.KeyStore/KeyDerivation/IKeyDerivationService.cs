using System.Text;

namespace Lyo.KeyStore.KeyDerivation;

/// <summary>Derives encryption keys from passwords or other input material. Implementations use PBKDF2 or another secure algorithm.</summary>
public interface IKeyDerivationService
{
    /// <summary>Default salt size in bytes for this service. Each implementation picks a salt size that matches its algorithm.</summary>
    int DefaultSaltSize { get; }

    /// <summary>Default key size in bytes for this service. Typically 32 bytes (256 bits).</summary>
    int DefaultKeySize { get; }

    /// <summary>Default encoding for string-to-byte conversions. Defaults to UTF-8.</summary>
    Encoding DefaultEncoding { get; set; }

    /// <summary>Derives a key from a password string.</summary>
    /// <param name="password">Password to derive the key from</param>
    /// <param name="salt">Optional salt. When null, a random salt is generated.</param>
    /// <param name="iterations">PBKDF2 iteration count. Default is 600,000 (OWASP 2023 recommendation).</param>
    /// <param name="keySizeBytes">Derived key size in bytes. When null, uses the implementation DefaultKeySize.</param>
    /// <param name="encoding">Optional encoding. When null, uses DefaultEncoding.</param>
    /// <returns>The derived key</returns>
    byte[] DeriveKey(string password, byte[]? salt = null, int iterations = 600000, int? keySizeBytes = null, Encoding? encoding = null);

    /// <summary>Derives a key from a password byte array.</summary>
    /// <param name="password">Password bytes to derive the key from</param>
    /// <param name="salt">Optional salt. When null, a random salt is generated.</param>
    /// <param name="iterations">PBKDF2 iteration count. Default is 600,000 (OWASP 2023 recommendation).</param>
    /// <param name="keySizeBytes">Derived key size in bytes. When null, uses the implementation DefaultKeySize.</param>
    /// <returns>The derived key</returns>
    byte[] DeriveKey(byte[] password, byte[]? salt = null, int iterations = 600000, int? keySizeBytes = null);

    /// <summary>Derives a key from a password and returns both the key and salt.</summary>
    /// <param name="password">Password to derive the key from</param>
    /// <param name="iterations">PBKDF2 iteration count. Default is 600,000 (OWASP 2023 recommendation).</param>
    /// <param name="keySizeBytes">Derived key size in bytes. When null, uses the implementation DefaultKeySize.</param>
    /// <param name="encoding">Optional encoding. When null, uses DefaultEncoding.</param>
    /// <returns>A tuple of the derived key and the salt used</returns>
    (byte[] Key, byte[] Salt) DeriveKeyWithSalt(string password, int iterations = 600000, int? keySizeBytes = null, Encoding? encoding = null);

    /// <summary>Derives a key from a password byte array and returns both the key and salt.</summary>
    /// <param name="password">Password bytes to derive the key from</param>
    /// <param name="iterations">PBKDF2 iteration count. Default is 600,000 (OWASP 2023 recommendation).</param>
    /// <param name="keySizeBytes">Derived key size in bytes. When null, uses the implementation DefaultKeySize.</param>
    /// <returns>A tuple of the derived key and the salt used</returns>
    (byte[] Key, byte[] Salt) DeriveKeyWithSalt(byte[] password, int iterations = 600000, int? keySizeBytes = null);
}