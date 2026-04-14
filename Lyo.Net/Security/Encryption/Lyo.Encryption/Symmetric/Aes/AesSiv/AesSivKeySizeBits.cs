namespace Lyo.Encryption.Symmetric.Aes.AesSiv;

/// <summary>AES-SIV key sizes per RFC 5297 (Dorssel.Security.Cryptography.AesExtra): 256/384/512-bit keys (32/48/64 bytes).</summary>
public enum AesSivKeySizeBits
{
    Bits256 = 256,

    Bits384 = 384,

    Bits512 = 512
}

public static class AesSivKeySizeBitsExtensions
{
    public static int GetKeyLengthBytes(this AesSivKeySizeBits bits) =>
        bits switch {
            AesSivKeySizeBits.Bits256 => 32,
            AesSivKeySizeBits.Bits384 => 48,
            AesSivKeySizeBits.Bits512 => 64,
            _ => throw new ArgumentOutOfRangeException(nameof(bits), bits, null)
        };
}
