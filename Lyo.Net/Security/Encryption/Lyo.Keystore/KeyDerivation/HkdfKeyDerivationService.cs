using System.Security.Cryptography;
using System.Text;
using Lyo.Common.Security;
using Lyo.Exceptions;

namespace Lyo.Keystore.KeyDerivation;

/// <summary>
/// HKDF (HMAC-based Extract-and-Expand Key Derivation Function) based key derivation service implementation. Uses HKDF as defined in RFC 5869 for secure key derivation. HKDF
/// is a modern, efficient key derivation function that doesn't require iterations like PBKDF2. Note: The 'iterations' parameter is ignored for HKDF as it's a single-pass algorithm.
/// HKDF uses an 'info' parameter for context binding instead of iterations.
/// </summary>
public class HkdfKeyDerivationService : IKeyDerivationService
{
    private const string DefaultInfo = "Lyo.KeyDerivation.HKDF"; // Context string for key binding

    private readonly HashAlgorithmName _hashAlgorithm;

    /// <summary>Initializes a new instance of the HkdfKeyDerivationService.</summary>
    /// <param name="hashAlgorithm">The hash algorithm to use. Defaults to SHA-256.</param>
    public HkdfKeyDerivationService(HashAlgorithmName? hashAlgorithm = null)
    {
        _hashAlgorithm = hashAlgorithm ?? HashAlgorithmName.SHA256;
#if !NET10_0_OR_GREATER
        if (_hashAlgorithm != HashAlgorithmName.SHA256)
            throw new PlatformNotSupportedException("HKDF on this target framework only supports SHA-256. Use .NET 10+ for other hash algorithms.");
#endif
    }

    /// <summary>Gets the default salt size for HKDF (32 bytes / 256 bits).</summary>
    public int DefaultSaltSize => 32;

    /// <summary>Gets the default key size (32 bytes / 256 bits).</summary>
    public int DefaultKeySize => 32;

    /// <summary>Gets or sets the default encoding used for string-to-byte conversions. Defaults to UTF-8.</summary>
    public Encoding DefaultEncoding { get; set; } = Encoding.UTF8;

    /// <summary>Derives a key from a password string using HKDF. Note: The 'iterations' parameter is ignored for HKDF as it's a single-pass algorithm.</summary>
    public byte[] DeriveKey(string password, byte[]? salt = null, int iterations = 600000, int? keySizeBytes = null, Encoding? encoding = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(password, nameof(password));
        var passwordBytes = (encoding ?? DefaultEncoding).GetBytes(password);
        return DeriveKey(passwordBytes, salt, iterations, keySizeBytes ?? DefaultKeySize);
    }

    /// <summary>Derives a key from a password byte array using HKDF. Note: The 'iterations' parameter is ignored for HKDF as it's a single-pass algorithm.</summary>
    public byte[] DeriveKey(byte[] password, byte[]? salt = null, int iterations = 600000, int? keySizeBytes = null)
    {
        ArgumentHelpers.ThrowIfNull(password, nameof(password));
        ArgumentHelpers.ThrowIf(password.Length == 0, "Password cannot be empty", nameof(password));
        var actualKeySize = keySizeBytes ?? DefaultKeySize;
        ArgumentHelpers.ThrowIfNullOrNotInRange(actualKeySize, 16, 64, nameof(keySizeBytes));
        var actualSalt = salt ?? CryptographicRandom.GetBytes(DefaultSaltSize);

        // HKDF doesn't use iterations - it's a single-pass algorithm
        // Use the info parameter for context binding (RFC 5869)
        var info = Encoding.UTF8.GetBytes(DefaultInfo);

#if NET10_0_OR_GREATER
        var prk = HKDF.Extract(_hashAlgorithm, password, actualSalt);
        return HKDF.Expand(_hashAlgorithm, prk, actualKeySize, info);
#else
        var prk = HkdfRfc5869.Extract(actualSalt, password);
        return HkdfRfc5869.Expand(prk, actualKeySize, info);
#endif
    }

    /// <summary>Derives a key from a password and returns both the key and salt.</summary>
    public (byte[] Key, byte[] Salt) DeriveKeyWithSalt(string password, int iterations = 600000, int? keySizeBytes = null, Encoding? encoding = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(password, nameof(password));
        var passwordBytes = (encoding ?? DefaultEncoding).GetBytes(password);
        return DeriveKeyWithSalt(passwordBytes, iterations, keySizeBytes ?? DefaultKeySize);
    }

    /// <summary>Derives a key from a password byte array and returns both the key and salt.</summary>
    public (byte[] Key, byte[] Salt) DeriveKeyWithSalt(byte[] password, int iterations = 600000, int? keySizeBytes = null)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(password, nameof(password));
        var actualKeySize = keySizeBytes ?? DefaultKeySize;
        var salt = CryptographicRandom.GetBytes(DefaultSaltSize);
        var key = DeriveKey(password, salt, iterations, actualKeySize);
        return (key, salt);
    }
}