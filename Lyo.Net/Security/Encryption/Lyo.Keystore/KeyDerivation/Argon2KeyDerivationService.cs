using System.Text;
using Konscious.Security.Cryptography;
using Lyo.Common.Security;
using Lyo.Exceptions;

namespace Lyo.Keystore.KeyDerivation;

/// <summary>
/// Argon2id-based key derivation service implementation. Argon2 is the winner of the Password Hashing Competition and is considered the industry standard for password-based
/// key derivation because it is "memory-hard," making it highly resistant to GPU and ASIC-based cracking attacks. Uses Argon2id variant (recommended for most use cases) which
/// balances resistance against side-channel attacks and GPU cracking. Note: The 'iterations' parameter maps to Argon2's time cost parameter. Argon2 also uses memory size and
/// parallelism parameters which are configured via constructor.
/// </summary>
public class Argon2KeyDerivationService : IKeyDerivationService
{
    private readonly int _defaultDegreeOfParallelism;

    private readonly int _defaultIterations;

    // RFC 9106 recommended minimums for sensitive data:
    // Memory: 64MB (65536 KB), Iterations: 3, Parallelism: 4
    // For production, consider: Memory: 65536 KB, Iterations: 3-4, Parallelism: 4-8
    private readonly int _defaultMemorySize; // in KB

    /// <summary>Initializes a new instance of the Argon2KeyDerivationService.</summary>
    /// <param name="memorySize">Memory size in KB. Default is 65536 KB (64 MB) per RFC 9106 recommendation.</param>
    /// <param name="defaultIterations">Default number of iterations (time cost). Default is 3 per RFC 9106 recommendation.</param>
    /// <param name="degreeOfParallelism">Degree of parallelism (number of threads). Default is 4 per RFC 9106 recommendation.</param>
    public Argon2KeyDerivationService(int memorySize = 65536, int defaultIterations = 3, int degreeOfParallelism = 4)
    {
        ArgumentHelpers.ThrowIfNullOrNotInRange(memorySize, 8, int.MaxValue, nameof(memorySize));
        ArgumentHelpers.ThrowIfNullOrNotInRange(defaultIterations, 1, int.MaxValue, nameof(defaultIterations));
        ArgumentHelpers.ThrowIfNullOrNotInRange(degreeOfParallelism, 1, int.MaxValue, nameof(degreeOfParallelism));
        _defaultMemorySize = memorySize;
        _defaultIterations = defaultIterations;
        _defaultDegreeOfParallelism = degreeOfParallelism;
    }

    /// <summary>Gets the default salt size for Argon2 (16 bytes / 128 bits) per RFC 9106 minimum recommendation.</summary>
    public int DefaultSaltSize => 16;

    /// <summary>Gets the default key size (32 bytes / 256 bits).</summary>
    public int DefaultKeySize => 32;

    /// <summary>Gets or sets the default encoding used for string-to-byte conversions. Defaults to UTF-8.</summary>
    public Encoding DefaultEncoding { get; set; } = Encoding.UTF8;

    /// <summary>Derives a key from a password string using Argon2id. Note: The 'iterations' parameter maps to Argon2's time cost parameter.</summary>
    public byte[] DeriveKey(string password, byte[]? salt = null, int iterations = 600000, int? keySizeBytes = null, Encoding? encoding = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(password, nameof(password));
        var passwordBytes = (encoding ?? DefaultEncoding).GetBytes(password);
        return DeriveKey(passwordBytes, salt, iterations, keySizeBytes ?? DefaultKeySize);
    }

    /// <summary>
    /// Derives a key from a password byte array using Argon2id. Note: The 'iterations' parameter maps to Argon2's time cost parameter. For Argon2, typical iteration values are
    /// 3-4 (not 600,000 like PBKDF2). If iterations >= 100, it's treated as a legacy PBKDF2-style value and mapped to Argon2's range.
    /// </summary>
    public byte[] DeriveKey(byte[] password, byte[]? salt = null, int iterations = 600000, int? keySizeBytes = null)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(password, nameof(password));
        var actualKeySize = keySizeBytes ?? DefaultKeySize;
        ArgumentHelpers.ThrowIfNullOrNotInRange(actualKeySize, 16, 64, nameof(keySizeBytes));

        // Argon2 uses much lower iteration counts than PBKDF2 (typically 3-4)
        // Map high iteration values (from PBKDF2-style usage) to Argon2's range
        var argon2Iterations = MapIterationsToArgon2Range(iterations);
        var actualSalt = salt ?? CryptographicRandom.GetBytes(DefaultSaltSize);
        ArgumentHelpers.ThrowIfNullOrNotInRange(actualSalt, DefaultSaltSize, long.MaxValue, nameof(salt));
        using var argon2 = new Argon2id(password) {
            Salt = actualSalt,
            DegreeOfParallelism = _defaultDegreeOfParallelism,
            Iterations = argon2Iterations,
            MemorySize = _defaultMemorySize
        };

        return argon2.GetBytes(actualKeySize);
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

    /// <summary>Maps PBKDF2-style iteration counts to Argon2's typical range (3-4 iterations). Argon2's memory-hardness means it doesn't need as many iterations as PBKDF2.</summary>
    private int MapIterationsToArgon2Range(int iterations)
    {
        // If iterations is in Argon2's typical range (1-10), use it directly
        if (iterations >= 1 && iterations <= 10)
            return iterations;

        // If iterations is very high (PBKDF2-style), map to Argon2's recommended range
        // High security: 4 iterations, Standard: 3 iterations
        if (iterations >= 100000)
            return 4; // High security

        if (iterations >= 10000)
            return 3; // Standard security

        // For moderate values, use a reasonable default
        return _defaultIterations;
    }
}