using System.Text;
using Konscious.Security.Cryptography;
using Lyo.Common.Core.Security;
using Lyo.Exceptions;

namespace Lyo.KeyStore.KeyDerivation;

/// <summary>
/// Argon2id key-derivation service. Argon2 won the Password Hashing Competition and is the usual choice for password-based
/// key derivation because it is memory-hard, which resists GPU and ASIC cracking. Uses the Argon2id variant (recommended for most cases), which
/// balances side-channel resistance and GPU cracking. Note: the 'iterations' parameter maps to Argon2's time cost. Argon2 also uses memory size and
/// parallelism, set via the constructor.
/// </summary>
public class Argon2KeyDerivationService : IKeyDerivationService
{
    private readonly int _defaultDegreeOfParallelism;

    private readonly int _defaultIterations;

    // RFC 9106 recommended minimums for sensitive data:
    // Memory: 64MB (65536 KB), Iterations: 3, Parallelism: 4
    // For production, consider: Memory: 65536 KB, Iterations: 3-4, Parallelism: 4-8
    private readonly int _defaultMemorySize; // in KB

    /// <summary>Builds a new Argon2KeyDerivationService.</summary>
    /// <param name="memorySize">Memory size in KB. Default is 65536 KB (64 MB) per RFC 9106.</param>
    /// <param name="defaultIterations">Default iteration count (time cost). Default is 3 per RFC 9106.</param>
    /// <param name="degreeOfParallelism">Parallelism (thread count). Default is 4 per RFC 9106.</param>
    public Argon2KeyDerivationService(int memorySize = 65536, int defaultIterations = 3, int degreeOfParallelism = 4)
    {
        ArgumentHelpers.ThrowIfNullOrNotInRange(memorySize, 8, int.MaxValue);
        ArgumentHelpers.ThrowIfNullOrNotInRange(defaultIterations, 1, int.MaxValue);
        ArgumentHelpers.ThrowIfNullOrNotInRange(degreeOfParallelism, 1, int.MaxValue);
        _defaultMemorySize = memorySize;
        _defaultIterations = defaultIterations;
        _defaultDegreeOfParallelism = degreeOfParallelism;
    }

    /// <summary>Default salt size for Argon2 (16 bytes / 128 bits) per the RFC 9106 minimum.</summary>
    public int DefaultSaltSize => 16;

    /// <summary>Default key size (32 bytes / 256 bits).</summary>
    public int DefaultKeySize => 32;

    /// <summary>Default encoding for string-to-byte conversions. Defaults to UTF-8.</summary>
    public Encoding DefaultEncoding { get; set; } = Encoding.UTF8;

    /// <summary>Derives a key from a password string using Argon2id. The 'iterations' parameter maps to Argon2's time cost.</summary>
    public byte[] DeriveKey(string password, byte[]? salt = null, int iterations = 600000, int? keySizeBytes = null, Encoding? encoding = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(password);
        var passwordBytes = (encoding ?? DefaultEncoding).GetBytes(password);
        return DeriveKey(passwordBytes, salt, iterations, keySizeBytes ?? DefaultKeySize);
    }

    /// <summary>
    /// Derives a key from a password byte array using Argon2id. The 'iterations' parameter maps to Argon2's time cost. Typical Argon2 iteration values are
    /// 3-4 (not 600,000 like PBKDF2). If iterations &gt;= 100, it is treated as a legacy PBKDF2-style value and mapped into Argon2's range.
    /// </summary>
    public byte[] DeriveKey(byte[] password, byte[]? salt = null, int iterations = 600000, int? keySizeBytes = null)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(password);
        var actualKeySize = keySizeBytes ?? DefaultKeySize;
        ArgumentHelpers.ThrowIfNullOrNotInRange(actualKeySize, 16, 64, nameof(keySizeBytes));

        // Argon2 uses much lower iteration counts than PBKDF2 (typically 3-4)
        // Map high iteration values (from PBKDF2-style usage) into Argon2's range
        var argon2Iterations = MapIterationsToArgon2Range(iterations);
        var actualSalt = salt ?? CryptographicRandom.GetBytes(DefaultSaltSize);
        ArgumentHelpers.ThrowIfNullOrNotInRange(actualSalt, DefaultSaltSize, long.MaxValue, nameof(salt));
        using var argon2 = new Argon2i(password) {
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
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(password);
        var passwordBytes = (encoding ?? DefaultEncoding).GetBytes(password);
        return DeriveKeyWithSalt(passwordBytes, iterations, keySizeBytes ?? DefaultKeySize);
    }

    /// <summary>Derives a key from a password byte array and returns both the key and salt.</summary>
    public (byte[] Key, byte[] Salt) DeriveKeyWithSalt(byte[] password, int iterations = 600000, int? keySizeBytes = null)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(password);
        var actualKeySize = keySizeBytes ?? DefaultKeySize;
        var salt = CryptographicRandom.GetBytes(DefaultSaltSize);
        var key = DeriveKey(password, salt, iterations, actualKeySize);
        return (key, salt);
    }

    /// <summary>Maps PBKDF2-style iteration counts into Argon2's typical range (3-4). Argon2's memory-hardness means it needs fewer iterations than PBKDF2.</summary>
    private int MapIterationsToArgon2Range(int iterations)
    {
        // If iterations is in Argon2's typical range (1-10), use it as-is
        if (iterations >= 1 && iterations <= 10)
            return iterations;

        // If iterations is very high (PBKDF2-style), map into Argon2's recommended range
        // High security: 4 iterations, Standard: 3 iterations
        if (iterations >= 100000)
            return 4; // High security

        if (iterations >= 10000)
            return 3; // Standard security

        // For moderate values, use a reasonable default
        return _defaultIterations;
    }
}