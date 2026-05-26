namespace Lyo.FileStorage.S3.Tests;

/// <summary>Exercises the canonical key builder shared by S3 and Blob backends.</summary>
public sealed class CloudObjectKeyBuilderTests
{
    [Fact]
    public void Build_DefaultShape_TwoCharShardThenIdAndExtension()
    {
        var id = new Guid("12345678-1234-1234-1234-123456789012");
        var idN = id.ToString("N");
        var key = CloudObjectKeyBuilder.Build(id, ".bin");
        // First 4 chars of N form -> 12345678 -> shards "12" / "34"
        Assert.Equal($"12/34/{idN}.bin", key);
    }

    [Fact]
    public void Build_WithKeyPrefix_PrependsAndKeepsShards()
    {
        var id = new Guid("ab345678-1234-1234-1234-1234567890aa");
        var key = CloudObjectKeyBuilder.Build(id, "", null, "/tenant-a/");
        Assert.StartsWith("tenant-a/", key, StringComparison.Ordinal);
        Assert.Contains("ab/34/", key, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_WithExplicitPathPrefix_SkipsShardSegments()
    {
        var id = new Guid("ab345678-1234-1234-1234-1234567890aa");
        var idN = id.ToString("N");
        var key = CloudObjectKeyBuilder.Build(id, ".gz", "incoming/x", null);
        Assert.Equal($"incoming/x/{idN}.gz", key);
    }

    [Fact]
    public void Build_PrefixAndPathPrefix_OrderIsPrefixThenPathThenFile()
    {
        var id = new Guid("ab345678-1234-1234-1234-1234567890aa");
        var idN = id.ToString("N");
        var key = CloudObjectKeyBuilder.Build(id, "", "p", "root");
        Assert.Equal($"root/p/{idN}", key);
    }
}