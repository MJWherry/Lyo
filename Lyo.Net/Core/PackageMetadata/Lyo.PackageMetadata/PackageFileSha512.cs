using Lyo.Common.Enums;
using Lyo.Exceptions;
using Lyo.Hashing;

namespace Lyo.PackageMetadata;

/// <summary>SHA-512 helpers for <c>.nupkg</c> integrity; NuGet package registrations use SHA512 for <c>packageHash</c> (this API uses lowercase hex for persistence).</summary>
public static class PackageFileSha512
{
    /// <summary>Computes SHA-512 of <paramref name="packageContents" /> and returns 128 lowercase hexadecimal characters.</summary>
    public static string ComputeHex(byte[] packageContents)
    {
        ArgumentHelpers.ThrowIfNull(packageContents);
        return HexEncoding.ToHexString(Hasher.ComputeSha512(packageContents), TextLetterCase.Lower);
    }

    /// <summary>Computes SHA-512 of the remainder of <paramref name="packageStream" /> from its current position.</summary>
    public static string ComputeHex(Stream packageStream, bool leaveOpen = false)
    {
        ArgumentHelpers.ThrowIfNull(packageStream);
        try {
            return HexEncoding.ToHexString(Hasher.ComputeSha512(packageStream), TextLetterCase.Lower);
        }
        finally {
            if (!leaveOpen)
                packageStream.Dispose();
        }
    }
}
