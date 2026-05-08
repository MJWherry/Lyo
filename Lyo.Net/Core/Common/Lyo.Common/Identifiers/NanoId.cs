using Lyo.Common.Security;
using Lyo.Exceptions;

namespace Lyo.Common.Identifiers;

/// <summary>
/// NanoID generator: a compact, URL-safe, cryptographically random string identifier. Uses rejection sampling against a power-of-2 bitmask for a perfectly uniform
/// distribution over any alphabet without bias.
/// </summary>
public static class NanoId
{
    /// <summary>Default URL-safe alphabet (64 characters: digits, lowercase, uppercase, '_', '-').</summary>
    public const string DefaultAlphabet = "_-0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

    /// <summary>Default length — 21 characters gives ~126 bits of entropy with the default 64-char alphabet.</summary>
    public const int DefaultSize = 21;

    /// <summary>Creates a NanoID with the default alphabet and default length.</summary>
    public static string Create() => Generate(DefaultAlphabet, DefaultSize);

    /// <summary>Creates a NanoID with the default alphabet and a custom <paramref name="size" />.</summary>
    public static string Create(int size) => Generate(DefaultAlphabet, size);

    /// <summary>Creates a NanoID with a custom <paramref name="alphabet" /> and <paramref name="size" />.</summary>
    public static string Create(string alphabet, int size) => Generate(alphabet, size);

    /// <summary>
    /// Generates <paramref name="count" /> NanoIDs with the default alphabet and default length in a single batch. Uses one pre-sized RNG fill for the whole batch; a refill only
    /// happens in the (extremely rare) event that rejection sampling exhausts the pre-allocated pool.
    /// </summary>
    public static string[] CreateBulk(int count) => GenerateBulk(DefaultAlphabet, DefaultSize, count);

    /// <summary>Generates <paramref name="count" /> NanoIDs with the default alphabet and a custom <paramref name="size" />.</summary>
    public static string[] CreateBulk(int count, int size) => GenerateBulk(DefaultAlphabet, size, count);

    /// <summary>Generates <paramref name="count" /> NanoIDs with a custom <paramref name="alphabet" /> and <paramref name="size" />.</summary>
    public static string[] CreateBulk(int count, string alphabet, int size) => GenerateBulk(alphabet, size, count);

    private static string Generate(string alphabet, int size)
    {
        ValidateArgs(alphabet, size);
        var mask = ComputeMask(alphabet.Length);
        var bufSize = PoolSize(size, mask, alphabet.Length);
        var buf = new byte[bufSize];
        var chars = new char[size];
        var filled = 0;
        while (filled < size) {
            CryptographicRandom.Fill(buf);
            for (var i = 0; i < buf.Length && filled < size; i++) {
                var idx = buf[i] & mask;
                if (idx < alphabet.Length)
                    chars[filled++] = alphabet[idx];
            }
        }

        return new(chars);
    }

    private static string[] GenerateBulk(string alphabet, int size, int count)
    {
        ArgumentHelpers.ThrowIfLessThanOrEqual(count, 0);
        ValidateArgs(alphabet, size);
        var mask = ComputeMask(alphabet.Length);
        var perIdBuf = PoolSize(size, mask, alphabet.Length);
        var totalBuf = count * perIdBuf; // pre-sized so refills are essentially impossible
        var buf = new byte[totalBuf];
        CryptographicRandom.Fill(buf);
        var result = new string[count];
        var bufPos = 0;
        for (var n = 0; n < count; n++) {
            var chars = new char[size];
            var filled = 0;
            while (filled < size) {
                // Refill only in the astronomically rare case of exhaustion due to rejection.
                if (bufPos == totalBuf) {
                    CryptographicRandom.Fill(buf);
                    bufPos = 0;
                }

                var idx = buf[bufPos++] & mask;
                if (idx < alphabet.Length)
                    chars[filled++] = alphabet[idx];
            }

            result[n] = new(chars);
        }

        return result;
    }

    private static void ValidateArgs(string alphabet, int size)
    {
        ArgumentHelpers.ThrowIfNegativeOrZero(size);
        ArgumentHelpers.ThrowIfNullOrEmpty(alphabet);
        ArgumentHelpers.ThrowIf(alphabet.Length > 255, "Alphabet must not exceed 255 characters.", nameof(alphabet));
    }

    private static int ComputeMask(int alphabetLength)
    {
        var mask = 1;
        while (mask < alphabetLength)
            mask = (mask << 1) | 1;

        return mask;
    }

    // ~1.6× overallocation factor accounts for worst-case rejection rate at a given mask/alphabet ratio.
    private static int PoolSize(int size, int mask, int alphabetLength) => Math.Max(size, (int)Math.Ceiling(size * 1.6 * mask / alphabetLength) + 1);
}