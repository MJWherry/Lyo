using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Lyo.Encryption.Security;

/// <summary>Security utilities for secure memory operations and constant-time comparisons.</summary>
public static class SecurityUtilities
{
    /// <summary>
    /// Securely clears a byte array by overwriting it with zeros. Note: This helps reduce the window of exposure, but cannot guarantee complete removal from memory due to GC
    /// behavior and memory dumps.
    /// </summary>
    /// <param name="data">The byte array to clear</param>
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

    /// <summary>Securely clears a span of bytes.</summary>
    /// <param name="data">The span to clear</param>
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
    /// Performs a constant-time comparison of two byte arrays. Returns true if arrays are equal, false otherwise. This prevents timing attacks by ensuring comparison always
    /// takes the same time.
    /// </summary>
    /// <param name="a">First byte array</param>
    /// <param name="b">Second byte array</param>
    /// <returns>True if arrays are equal, false otherwise</returns>
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

    /// <summary>Performs a constant-time comparison of two spans.</summary>
    /// <param name="a">First span</param>
    /// <param name="b">Second span</param>
    /// <returns>True if spans are equal, false otherwise</returns>
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
