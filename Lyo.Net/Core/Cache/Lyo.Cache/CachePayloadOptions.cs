namespace Lyo.Cache;

/// <summary>Options for byte payload encoding (compress / encrypt) when using payload cache APIs.</summary>
public sealed class CachePayloadOptions
{
    /// <summary>When true, compresses payload bytes before caching when plaintext size is at least <see cref="AutoCompressMinSizeBytes"/>.</summary>
    public bool AutoCompress { get; set; }

    /// <summary>Minimum plaintext size in bytes before compression is considered. Ignored when <see cref="AutoCompress"/> is false.</summary>
    public int AutoCompressMinSizeBytes { get; set; } = 1024;

    /// <summary>When true, encrypts payload bytes (after optional compression) before caching. Requires an <c>IEncryptionService</c> registration.</summary>
    public bool AutoEncrypt { get; set; }

    /// <summary>Key id passed to <see cref="Lyo.Encryption.IEncryptionService"/> when <see cref="AutoEncrypt"/> is true.</summary>
    public string? EncryptionKeyId { get; set; }
}
