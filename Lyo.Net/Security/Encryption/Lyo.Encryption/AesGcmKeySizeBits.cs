namespace Lyo.Encryption;

/// <summary>AES-GCM key sizes supported by <see cref="AesGcm.AesGcmEncryptionService"/> (AES block size is always 128 bits).</summary>
public enum AesGcmKeySizeBits
{
    /// <summary>128-bit AES key (16 bytes).</summary>
    Bits128 = 128,

    /// <summary>192-bit AES key (24 bytes).</summary>
    Bits192 = 192,

    /// <summary>256-bit AES key (32 bytes).</summary>
    Bits256 = 256
}

/// <summary>Maps AES-GCM key sizes to byte length for <see cref="System.Security.Cryptography.AesGcm"/>.</summary>
public static class AesGcmKeySizeBitsExtensions
{
    /// <summary>Returns key length in bytes for the given AES-GCM key size.</summary>
    public static int GetKeyLengthBytes(this AesGcmKeySizeBits bits)
        => bits switch {
            AesGcmKeySizeBits.Bits128 => 16,
            AesGcmKeySizeBits.Bits192 => 24,
            AesGcmKeySizeBits.Bits256 => 32,
            _ => throw new ArgumentOutOfRangeException(nameof(bits), bits, null)
        };
}
