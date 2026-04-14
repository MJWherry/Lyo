using Lyo.Encryption.Symmetric.Aes.AesSiv;

namespace Lyo.Encryption;

/// <summary>Options for configuring encryption service behavior.</summary>
public class EncryptionServiceOptions
{
    /// <summary>Gets or sets the current format version used for encryption. If null, the service doesn't use format versioning.</summary>
    public byte? CurrentFormatVersion { get; set; } = (byte)StreamFormatVersion.V1;

    /// <summary>Gets or sets the maximum allowed input size in bytes. Defaults to long.MaxValue.</summary>
    public long MaxInputSize { get; set; } = long.MaxValue;

    /// <summary>Gets or sets the minimum allowed input size in bytes. Defaults to 1.</summary>
    public long MinInputSize { get; set; } = 1;

    /// <summary>Gets or sets the file extension used for encrypted files (e.g., ".ag", ".rsa", ".chacha"). Required.</summary>
    public string FileExtension { get; set; } = string.Empty;

    /// <summary>AES-GCM key size for <see cref="AesGcm.AesGcmEncryptionService"/> / hybrid AES paths. Ignored by non-AES services.</summary>
    public AesGcmKeySizeBits AesGcmKeySize { get; set; } = AesGcmKeySizeBits.Bits256;

    /// <summary>AES-SIV key size (RFC 5297: 32/48/64-byte keys). Ignored by non-SIV services.</summary>
    public AesSivKeySizeBits AesSivKeySize { get; set; } = AesSivKeySizeBits.Bits256;
}