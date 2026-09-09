using System.Runtime.CompilerServices;
#if NET10_0_OR_GREATER
using System.Security.Cryptography;
#endif

namespace Lyo.Encryption.Security;

/// <summary>Helpers for wiping memory and comparing secrets in constant time.</summary>
public static class SecurityUtilities
{
    /// <summary>
    /// Overwrites a byte array with zeros. Shrinks the exposure window; GC and memory dumps can still retain copies, so this is not a complete
    /// guarantee of removal.
    /// </summary>
    /// <param name="data">Array to wipe</param>
    [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
    public static void Clear(byte[]? data)
    {
        if (data == null || data.Length == 0)
            return;

#if NET10_0_OR_GREATER
        CryptographicOperations.ZeroMemory(data);
#else
        ZeroMemoryNs2(data);
#endif
    }

    /// <summary>Overwrites a span of bytes with zeros.</summary>
    /// <param name="data">Span to wipe</param>
    [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
    public static void Clear(Span<byte> data)
    {
        if (data.IsEmpty)
            return;

#if NET10_0_OR_GREATER
        CryptographicOperations.ZeroMemory(data);
#else
        for (var i = 0; i < data.Length; i++)
            data[i] = 0;
#endif
    }

    /// <summary>
    /// Constant-time equality of two byte arrays. Always scans the full length so timing does not leak whether the arrays
    /// match.
    /// </summary>
    /// <param name="a">Left array</param>
    /// <param name="b">Right array</param>
    /// <returns>True when the arrays are equal</returns>
    public static bool ConstantTimeEquals(byte[]? a, byte[]? b)
    {
        if (a == null && b == null)
            return true;

        if (a == null || b == null)
            return false;

        if (a.Length != b.Length)
            return false;

#if NET10_0_OR_GREATER
        return CryptographicOperations.FixedTimeEquals(a, b);
#else
        return FixedTimeEqualsNs2(a, b);
#endif
    }

    /// <summary>Constant-time equality of two spans.</summary>
    /// <param name="a">Left span</param>
    /// <param name="b">Right span</param>
    /// <returns>True when the spans are equal</returns>
    public static bool ConstantTimeEquals(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
#if NET10_0_OR_GREATER
        return a.Length == b.Length && CryptographicOperations.FixedTimeEquals(a, b);
#else
        return a.Length == b.Length && FixedTimeEqualsNs2(a, b);
#endif
    }

#if !NET10_0_OR_GREATER
    private static void ZeroMemoryNs2(byte[] data)
    {
        for (var i = 0; i < data.Length; i++)
            data[i] = 0;
    }

    private static bool FixedTimeEqualsNs2(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        if (a.Length != b.Length)
            return false;

        var diff = 0;
        for (var i = 0; i < a.Length; i++)
            diff |= a[i] ^ b[i];

        return diff == 0;
    }

    private static bool FixedTimeEqualsNs2(byte[] a, byte[] b)
    {
        var diff = 0;
        for (var i = 0; i < a.Length; i++)
            diff |= a[i] ^ b[i];

        return diff == 0;
    }
#endif
}