namespace Lyo.Encryption.AesSiv;

/// <summary>RFC 5297 AES-SIV key widths (Dorssel.Security.Cryptography.AesExtra): 256, 384, or 512 bits (32, 48, or 64 bytes).</summary>
public enum AesSivKeySizeBits
{
    Bits256 = 256,
    Bits384 = 384,
    Bits512 = 512
}

public static class AesSivKeySizeBitsExtensions
{
    public static int GetKeyLengthBytes(this AesSivKeySizeBits bits)
        => bits switch {
            AesSivKeySizeBits.Bits256 => 32,
            AesSivKeySizeBits.Bits384 => 48,
            AesSivKeySizeBits.Bits512 => 64,
            var _ => throw new ArgumentOutOfRangeException(nameof(bits), bits, null)
        };
}