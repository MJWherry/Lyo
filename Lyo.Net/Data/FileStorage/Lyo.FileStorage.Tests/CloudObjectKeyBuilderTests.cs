namespace Lyo.FileStorage.Tests;

/// <summary>Canonical object-key layout used by local, S3, and blob backends, including metadata-derived expected paths.</summary>
public sealed class CloudObjectKeyBuilderTests
{
    private static readonly Guid FileId = new("12345678-1234-1234-1234-123456789012");
    private static readonly string FileIdN = FileId.ToString("N");

    [Fact]
    public void FromMetadata_NoPathPrefix_UsesTwoCharShard()
    {
        var key = CloudObjectKeyBuilder.FromMetadata(FileId, $"{FileIdN}.gz", null);
        Assert.Equal($"12/34/{FileIdN}.gz", key);
    }

    [Fact]
    public void FromMetadata_ExplicitPathPrefix_SkipsShard()
    {
        var key = CloudObjectKeyBuilder.FromMetadata(FileId, $"{FileIdN}.enc", "tenant/alpha");
        Assert.Equal($"tenant/alpha/{FileIdN}.enc", key);
    }

    [Fact]
    public void FromMetadata_EmptySourceFileName_UsesBareFileId()
    {
        var key = CloudObjectKeyBuilder.FromMetadata(FileId, "", null);
        Assert.Equal($"12/34/{FileIdN}", key);
    }

    [Fact]
    public void FromMetadata_NullSourceFileName_UsesBareFileId()
    {
        var key = CloudObjectKeyBuilder.FromMetadata(FileId, null, "incoming");
        Assert.Equal($"incoming/{FileIdN}", key);
    }

    [Fact]
    public void FromMetadata_DashedGuidPrefix_InfersSuffix()
    {
        var key = CloudObjectKeyBuilder.FromMetadata(FileId, $"{FileId:D}.gz.enc", "p");
        Assert.Equal($"p/{FileIdN}.gz.enc", key);
    }

    [Fact]
    public void InferTrailingSuffixAfterFileId_UnrelatedName_ReturnsEmpty()
        => Assert.Equal("", CloudObjectKeyBuilder.InferTrailingSuffixAfterFileId(FileId, "photo.jpg"));

    [Fact]
    public void FromMetadata_WithStoragePrefix_PrependsRoot()
    {
        var key = CloudObjectKeyBuilder.FromMetadata(FileId, $"{FileIdN}.bin", "docs", "files");
        Assert.Equal($"files/docs/{FileIdN}.bin", key);
    }
}
