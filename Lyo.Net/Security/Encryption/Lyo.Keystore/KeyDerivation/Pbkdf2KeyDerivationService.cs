using System.Security.Cryptography;
using System.Text;
using Lyo.Exceptions;

namespace Lyo.Keystore.KeyDerivation;

/// <summary>
/// PBKDF2-based key derivation service implementation. Uses Rfc2898DeriveBytes (PBKDF2) with SHA-256 for secure key derivation. Follows OWASP 2023 recommendations: 600,000+
/// iterations and 32-byte salt.
/// </summary>
public class Pbkdf2KeyDerivationService : IKeyDerivationService
{
    private const int DefaultIterations = 600000; // OWASP 2023 recommendation (minimum 600,000)

    /// <summary>Gets the default salt size for PBKDF2 (32 bytes / 256 bits) per OWASP 2023 recommendation.</summary>
    public int DefaultSaltSize => 32;

    /// <summary>Gets the default key size (32 bytes / 256 bits).</summary>
    public int DefaultKeySize => 32;

    /// <summary>Gets or sets the default encoding used for string-to-byte conversions. Defaults to UTF-8.</summary>
    public Encoding DefaultEncoding { get; set; } = Encoding.UTF8;

    public byte[] DeriveKey(string password, byte[]? salt = null, int iterations = DefaultIterations, int? keySizeBytes = null, Encoding? encoding = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(password, nameof(password));
        var passwordBytes = (encoding ?? DefaultEncoding).GetBytes(password);
        return DeriveKey(passwordBytes, salt, iterations, keySizeBytes ?? DefaultKeySize);
    }

    public byte[] DeriveKey(byte[] password, byte[]? salt = null, int iterations = DefaultIterations, int? keySizeBytes = null)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(password, nameof(password));
        ArgumentHelpers.ThrowIfNullOrNotInRange(iterations, 600000, int.MaxValue, nameof(iterations));
        var actualKeySize = keySizeBytes ?? DefaultKeySize;
        ArgumentHelpers.ThrowIfNullOrNotInRange(actualKeySize, 16, 64, nameof(keySizeBytes));
        var actualSalt = salt ?? RandomNumberGenerator.GetBytes(DefaultSaltSize);
        return Rfc2898DeriveBytes.Pbkdf2(password, actualSalt, iterations, HashAlgorithmName.SHA256, actualKeySize);
    }

    public (byte[] Key, byte[] Salt) DeriveKeyWithSalt(string password, int iterations = DefaultIterations, int? keySizeBytes = null, Encoding? encoding = null)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(password, nameof(password));
        var passwordBytes = (encoding ?? DefaultEncoding).GetBytes(password);
        return DeriveKeyWithSalt(passwordBytes, iterations, keySizeBytes ?? DefaultKeySize);
    }

    public (byte[] Key, byte[] Salt) DeriveKeyWithSalt(byte[] password, int iterations = DefaultIterations, int? keySizeBytes = null)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(password, nameof(password));
        var actualKeySize = keySizeBytes ?? DefaultKeySize;
        var salt = RandomNumberGenerator.GetBytes(DefaultSaltSize);
        var key = DeriveKey(password, salt, iterations, actualKeySize);
        return (key, salt);
    }
}