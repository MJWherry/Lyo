namespace Lyo.PackageMetadata.Tests;

public sealed class PackageArtifactDigestStreamTests
{
    [Fact]
    public void ComputeHex_bytes_matches_stream()
    {
        var payload = "nupkg-bytes-parity"u8.ToArray();
        var bytesHex = PackageArtifactDigest.ComputeHexSha512(payload);
        using var ms = new MemoryStream(payload);
        var streamHex = PackageArtifactDigest.ComputeHex(ArtifactDigestAlgorithm.Sha512, ms, true);
        Assert.Equal(bytesHex, streamHex);
    }
}