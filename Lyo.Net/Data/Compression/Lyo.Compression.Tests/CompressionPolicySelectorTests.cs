using Lyo.Common.Enums;
using Lyo.Compression.LZ4;
using Lyo.Compression.Models;
using Lyo.Compression.Policy;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Compression.Tests;

public class CompressionPolicySelectorTests
{
    [Fact]
    public void Rule_TenantAndMinSize_SelectsAlgorithm()
    {
        var policy = new CompressionPolicyOptions { BuiltInDefaultsEnabled = false, Rules = [new() { Tenants = ["acme"], MinSizeBytes = 1000, Algorithm = "LZ4" }] };
        var selector = NewSelector(policy);
        var result = selector.ResolveForCompress(new() { ByteLength = 2000, ContentType = "application/json", TenantId = "acme" });
        Assert.True(result.ShouldCompress);
        Assert.Equal(Lz4CompressionAlgorithm.Instance, result.Algorithm);
    }

    [Fact]
    public void Rule_CompressFalse_SkipsCompression()
    {
        var policy = new CompressionPolicyOptions { BuiltInDefaultsEnabled = false, Rules = [new() { Categories = [FileTypeCategory.Images], Compress = false }] };
        var selector = NewSelector(policy);
        var result = selector.ResolveForCompress(new() { ByteLength = 100_000, ContentType = "image/png", OriginalFileName = "photo.png" });
        Assert.False(result.ShouldCompress);
        Assert.Null(result.Algorithm);
    }

    [Fact]
    public void BuiltIn_SmallPayload_SkipsCompression()
    {
        var policy = new CompressionPolicyOptions { MinCompressSizeBytes = 4096, BuiltInDefaultsEnabled = true, Rules = [] };
        var selector = NewSelector(policy);
        var result = selector.ResolveForCompress(new() { ByteLength = 100, ContentType = "text/plain" });
        Assert.False(result.ShouldCompress);
    }

    [Fact]
    public void NoRule_UsesDefaultAlgorithm()
    {
        var policy = new CompressionPolicyOptions { BuiltInDefaultsEnabled = false, DefaultAlgorithm = "GZip", Rules = [] };
        var selector = NewSelector(policy, new() { DefaultAlgorithm = CompressionAlgorithm.Brotli });
        var result = selector.ResolveForCompress(new() { ByteLength = 50_000, ContentType = "text/plain" });
        Assert.True(result.ShouldCompress);
        Assert.Equal(CompressionAlgorithm.GZip, result.Algorithm);
    }

    private static CompressionPolicyAlgorithmSelector NewSelector(CompressionPolicyOptions policy, CompressionServiceOptions? service = null)
        => new(policy, service ?? new CompressionServiceOptions(), NullLogger<CompressionPolicyAlgorithmSelector>.Instance);
}