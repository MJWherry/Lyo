namespace Lyo.FileStorage.Blob.Tests;

/// <summary>Exercises the canonical key builder shared by S3 and Blob backends.</summary>
public sealed class CloudObjectKeyBuilderTests
{
    [Fact]
    public void Build_BlobPrefix_PrependsCorrectly()
    {
        var id = new Guid("12345678-1234-1234-1234-123456789012");
        var key = CloudObjectKeyBuilder.Build(id, ".bin", null, "files");
        Assert.StartsWith("files/", key, StringComparison.Ordinal);
        Assert.Contains("12/34/", key, StringComparison.Ordinal);
        Assert.EndsWith(".bin", key, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_NoExtension_ReturnsKeyWithoutTrailingDot()
    {
        var id = new Guid("12345678-1234-1234-1234-123456789012");
        var key = CloudObjectKeyBuilder.Build(id);
        Assert.DoesNotContain('.', key);
    }

    [Fact]
    public void Build_WithExplicitPathPrefix_OverridesShardSplit()
    {
        var id = new Guid("12345678-1234-1234-1234-123456789012");
        var idN = id.ToString("N");
        var key = CloudObjectKeyBuilder.Build(id, ".gz", "tenant/alpha");
        Assert.Equal($"tenant/alpha/{idN}.gz", key);
    }
}