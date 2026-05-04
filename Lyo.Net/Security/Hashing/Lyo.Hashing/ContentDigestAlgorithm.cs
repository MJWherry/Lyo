namespace Lyo.Hashing;

/// <summary>Digests surfaced by <see cref="IHashingService" /> and <see cref="HashingService" />.</summary>
public enum ContentDigestAlgorithm
{
    /// <summary>SHA-256 (256-bit).</summary>
    Sha256 = 256,

    /// <summary>SHA-384 (384-bit).</summary>
    Sha384 = 384,

    /// <summary>SHA-512 (512-bit).</summary>
    Sha512 = 512,

    /// <summary>MD5 (128-bit). Not for security — compatibility and fingerprints only.</summary>
    Md5 = 5
}