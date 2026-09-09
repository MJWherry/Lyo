namespace Lyo.Encryption;

/// <summary>AES-GCM key widths accepted by <see cref="AesGcm.AesGcmEncryptionService" /> (AES block size stays 128 bits).</summary>
public enum AesGcmKeySizeBits
{
    /// <summary>AES-128 key (16 bytes).</summary>
    Bits128 = 128,

    /// <summary>AES-192 key (24 bytes).</summary>
    Bits192 = 192,

    /// <summary>AES-256 key (32 bytes).</summary>
    Bits256 = 256
}

/// <summary>Converts AES-GCM key-size enums to byte counts for <see cref="System.Security.Cryptography.AesGcm" />.</summary>
public static class AesGcmKeySizeBitsExtensions
{
    /// <summary>Byte length of the AES-GCM key for the given size.</summary>
    public static int GetKeyLengthBytes(this AesGcmKeySizeBits bits)
        => bits switch {
            AesGcmKeySizeBits.Bits128 => 16,
            AesGcmKeySizeBits.Bits192 => 24,
            AesGcmKeySizeBits.Bits256 => 32,
            var _ => throw new ArgumentOutOfRangeException(nameof(bits), bits, null)
        };
}