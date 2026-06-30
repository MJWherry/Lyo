namespace Lyo.Hashing;

/// <summary>
/// Non-cryptographic checksums surfaced by <see cref="Checksummer" /> and <see cref="IHashingService" />. These detect accidental corruption only — they are
/// <strong>not</strong> suitable for security, signatures, or tamper detection.
/// </summary>
public enum ChecksumAlgorithm
{
    /// <summary>CRC-32 (IEEE 802.3 / ITU-T V.42, the variant used by zip, gzip, and PNG). 32-bit. Check value of <c>"123456789"</c> is <c>0xCBF43926</c>.</summary>
    Crc32,

    /// <summary>CRC-32C (Castagnoli; iSCSI, ext4, SSE4.2). 32-bit. Check value of <c>"123456789"</c> is <c>0xE3069283</c>.</summary>
    Crc32C,

    /// <summary>CRC-64/ECMA-182 (as implemented by <c>System.IO.Hashing.Crc64</c>). 64-bit. Check value of <c>"123456789"</c> is <c>0x6C40DF5F0B497347</c>.</summary>
    Crc64,

    /// <summary>Adler-32 (zlib). 32-bit. Check value of <c>"123456789"</c> is <c>0x091E01DE</c>.</summary>
    Adler32
}
