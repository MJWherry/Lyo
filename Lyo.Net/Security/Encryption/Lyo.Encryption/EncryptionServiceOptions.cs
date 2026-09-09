namespace Lyo.Encryption;

/// <summary>Tunables that control encryption-service behavior.</summary>
public class EncryptionServiceOptions
{
    /// <summary>Format version written on encrypt. Null means the service does not version the format.</summary>
    public byte? CurrentFormatVersion { get; set; } = (byte)StreamFormatVersion.V1;

    /// <summary>Largest accepted input in bytes. Default is long.MaxValue.</summary>
    public long MaxInputSize { get; set; } = long.MaxValue;

    /// <summary>Smallest accepted input in bytes. Default is 1.</summary>
    public long MinInputSize { get; set; } = 1;

    /// <summary>Extension written on encrypted files (examples: ".ag", ".rsa", ".chacha"). Required.</summary>
    public string FileExtension { get; set; } = string.Empty;

    /// <summary>AES-GCM key width for <see cref="AesGcm.AesGcmEncryptionService" /> and hybrid AES paths. Unused by non-AES services.</summary>
    public AesGcmKeySizeBits AesGcmKeySize { get; set; } = AesGcmKeySizeBits.Bits256;
}