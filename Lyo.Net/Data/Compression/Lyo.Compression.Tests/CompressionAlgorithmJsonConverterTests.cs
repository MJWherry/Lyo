using System.Text.Json;
using Lyo.Compression.LZ4;
using Lyo.Compression.Models;

namespace Lyo.Compression.Tests;

public class CompressionAlgorithmJsonConverterTests
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = false };

    [Theory]
    [InlineData("GZip")]
    [InlineData("Deflate")]
    public void RoundTrip_BuiltInAlgorithm_SerializesAsNameString(string name)
    {
        var algorithm = CompressionAlgorithm.TryFromName(name);
        Assert.NotNull(algorithm);

        var json = JsonSerializer.Serialize(algorithm, Options);
        Assert.Equal($"\"{name}\"", json);

        var roundTripped = JsonSerializer.Deserialize<CompressionAlgorithm>(json, Options);
        Assert.Same(algorithm, roundTripped);
    }

#if !NETSTANDARD2_0
    [Theory]
    [InlineData("Brotli")]
    [InlineData("ZLib")]
    public void RoundTrip_Net10BuiltInAlgorithm_SerializesAsNameString(string name)
    {
        var algorithm = CompressionAlgorithm.TryFromName(name);
        Assert.NotNull(algorithm);

        var json = JsonSerializer.Serialize(algorithm, Options);
        Assert.Equal($"\"{name}\"", json);

        var roundTripped = JsonSerializer.Deserialize<CompressionAlgorithm>(json, Options);
        Assert.Same(algorithm, roundTripped);
    }
#endif

    [Fact]
    public void RoundTrip_AddonAlgorithm_SerializesAsNameString()
    {
        CompressionAlgorithm algorithm = Lz4CompressionAlgorithm.Instance;
        var json = JsonSerializer.Serialize(algorithm, Options);
        Assert.Equal("\"LZ4\"", json);

        var roundTripped = JsonSerializer.Deserialize<CompressionAlgorithm>(json, Options);
        Assert.Same(Lz4CompressionAlgorithm.Instance, roundTripped);
    }

    [Fact]
    public void Deserialize_Null_ReturnsNull()
    {
        Assert.Null(JsonSerializer.Deserialize<CompressionAlgorithm>("null", Options));
    }

    [Fact]
    public void Serialize_Null_WritesNull()
    {
        CompressionAlgorithm? algorithm = null;
        Assert.Equal("null", JsonSerializer.Serialize(algorithm, Options));
    }

    [Fact]
    public void Deserialize_UnknownName_ReturnsNull()
    {
        Assert.Null(JsonSerializer.Deserialize<CompressionAlgorithm>("\"NotARealAlgorithm\"", Options));
    }

    [Theory]
    [InlineData("{\"Name\":\"GZip\",\"Extension\":\".gz\"}")]
    [InlineData("{\"name\":\"GZip\",\"extension\":\".gz\"}")]
    public void Deserialize_LegacyObjectFormat_ResolvesRegisteredAlgorithm(string json)
    {
        var algorithm = JsonSerializer.Deserialize<CompressionAlgorithm>(json, Options);
        Assert.Same(CompressionAlgorithm.GZip, algorithm);
    }

    [Fact]
    public void NestedProperty_RoundTrip_PreservesCompressionAlgorithm()
    {
        var dto = new MetadataWithCompression(true, CompressionAlgorithm.GZip);
        var json = JsonSerializer.Serialize(dto, Options);
        Assert.Contains("\"CompressionAlgorithm\":\"GZip\"", json);

        var roundTripped = JsonSerializer.Deserialize<MetadataWithCompression>(json, Options);
        Assert.NotNull(roundTripped);
        Assert.Same(CompressionAlgorithm.GZip, roundTripped.CompressionAlgorithm);
        Assert.True(roundTripped.IsCompressed);
    }

    [Fact]
    public void NestedProperty_Deserialize_LegacyObjectCompressionAlgorithm()
    {
        const string json = """{"IsCompressed":true,"CompressionAlgorithm":{"Name":"GZip","Extension":".gz"}}""";
        var dto = JsonSerializer.Deserialize<MetadataWithCompression>(json, Options);
        Assert.NotNull(dto);
        Assert.Same(CompressionAlgorithm.GZip, dto.CompressionAlgorithm);
    }

    private sealed record MetadataWithCompression(bool IsCompressed, CompressionAlgorithm? CompressionAlgorithm);
}
