namespace Lyo.PackageMetadata;

/// <summary>Hash algorithm for <see cref="PackageMetadata.ArtifactDigestHex" /> over the canonical primary artifact bytes.</summary>
public enum ArtifactDigestAlgorithm
{
    None = 0,
    Sha512,
    Sha256,
    Sha1
}