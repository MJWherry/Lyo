namespace Lyo.FileMetadataStore.Models;

/// <summary>Represents the hash algorithm used for file integrity verification and duplicate detection.</summary>
public enum HashAlgorithm
{
    /// <summary>SHA-256 (Secure Hash Algorithm 256-bit). Default and recommended for most use cases.</summary>
    Sha256,

    /// <summary>SHA-384 (Secure Hash Algorithm 384-bit). Stronger than SHA-256 with larger hash size.</summary>
    Sha384,

    /// <summary>SHA-512 (Secure Hash Algorithm 512-bit). Most secure of the SHA-2 family with largest hash size.</summary>
    Sha512,

    /// <summary>MD5 (Message Digest 5). Faster but cryptographically broken. Use only when compatibility with legacy systems is required.</summary>
    Md5,

    /// <summary>SHA-1 (Secure Hash Algorithm 1). Legacy algorithm, cryptographically weakened. Not recommended for new systems.</summary>
    Sha1
}