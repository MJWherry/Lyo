using Lyo.FileMetadataStore.Models;

namespace Lyo.FileMetadataStore.Postgres.Tests;

public class HashAlgorithmExtensionsTests
{
    [Theory]
    [InlineData(HashAlgorithm.Sha256, 32)]
    [InlineData(HashAlgorithm.Sha384, 48)]
    [InlineData(HashAlgorithm.Sha512, 64)]
    [InlineData(HashAlgorithm.Md5, 16)]
    [InlineData(HashAlgorithm.Sha1, 20)]
    public void Create_ProducesHashOfExpectedLength(HashAlgorithm algorithm, int expectedLength)
    {
        var testData = "test data"u8.ToArray();
        using var algo = algorithm.Create();
        var hash = algo.ComputeHash(testData);
        Assert.Equal(expectedLength, hash.Length);
    }

    [Fact]
    public void Create_UnknownValue_FallsBackToSha256()
    {
        var invalidValue = (HashAlgorithm)(-1);
        using var algo = invalidValue.Create();
        var hash = algo.ComputeHash("test"u8.ToArray());
        Assert.Equal(32, hash.Length); // SHA-256
    }
}