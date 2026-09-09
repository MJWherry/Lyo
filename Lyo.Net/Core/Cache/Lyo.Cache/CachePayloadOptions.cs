namespace Lyo.Cache;

/// <summary>Compress/encrypt settings for payload cache APIs.</summary>
public sealed class CachePayloadOptions
{
    /// <summary>When true, compresses payload bytes before caching once plaintext is at least <see cref="AutoCompressMinSizeBytes" />.</summary>
    public bool AutoCompress { get; set; }

    /// <summary>Smallest plaintext size, in bytes, at which compression is considered. Ignored when <see cref="AutoCompress" /> is false.</summary>
    public int AutoCompressMinSizeBytes { get; set; } = 1024;

    /// <summary>When true, encrypts payload bytes (after optional compression) before caching. Requires an <c>IEncryptionService</c> in DI.</summary>
    public bool AutoEncrypt { get; set; }

    /// <summary>Key id given to <see cref="Lyo.Encryption.IEncryptionService" /> when <see cref="AutoEncrypt" /> is true.</summary>
    public string? EncryptionKeyId { get; set; }
}