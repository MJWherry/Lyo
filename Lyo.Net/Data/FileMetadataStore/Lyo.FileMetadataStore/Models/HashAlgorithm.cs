namespace Lyo.FileMetadataStore.Models;

/// <summary>Hash used for file integrity checks and duplicate detection.</summary>
public enum HashAlgorithm
{
    /// <summary>SHA-256 (256-bit). Starts as the recommended default for most cases.</summary>
    Sha256,

    /// <summary>SHA-384 (384-bit). Stronger than SHA-256 with a larger digest.</summary>
    Sha384,

    /// <summary>SHA-512 (512-bit). Strongest SHA-2 variant here, largest digest.</summary>
    Sha512,

    /// <summary>MD5. Faster but cryptographically broken. Use only when a legacy system requires it.</summary>
    Md5,

    /// <summary>SHA-1. Legacy and cryptographically weakened. Avoid for new systems.</summary>
    Sha1
}