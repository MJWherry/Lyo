using Lyo.FileMetadataStore.Models;

namespace Lyo.Api.FileStorage.Tests;

public sealed class FileContentDigestTests
{
    [Fact]
    public void ForDownload_Sha256_UsesRfc9530Dictionary()
    {
        var hash = new byte[] { 1, 2, 3, 4 };
        var digest = FileContentDigest.ForDownload(false, HashAlgorithm.Sha256, hash);
        Assert.Equal($"sha-256=:{Convert.ToBase64String(hash)}:", digest);
    }

    [Theory]
    [InlineData(HashAlgorithm.Sha384, "sha-384")]
    [InlineData(HashAlgorithm.Sha512, "sha-512")]
    [InlineData(HashAlgorithm.Sha1, "sha-1")]
    [InlineData(HashAlgorithm.Md5, "md5")]
    public void ForDownload_MapsEachStoredAlgorithm(HashAlgorithm algorithm, string token)
    {
        var hash = new byte[] { 9 };
        var digest = FileContentDigest.ForDownload(false, algorithm, hash);
        Assert.Equal($"{token}=:{Convert.ToBase64String(hash)}:", digest);
    }

    [Fact]
    public void ForDownload_RangeRequest_OmitsHeader()
    {
        var digest = FileContentDigest.ForDownload(true, HashAlgorithm.Sha256, [1, 2, 3]);
        Assert.Null(digest);
    }

    [Fact]
    public void ForDownload_MissingHash_OmitsHeader()
    {
        Assert.Null(FileContentDigest.ForDownload(false, HashAlgorithm.Sha256, null));
        Assert.Null(FileContentDigest.ForDownload(false, HashAlgorithm.Sha256, []));
    }
}