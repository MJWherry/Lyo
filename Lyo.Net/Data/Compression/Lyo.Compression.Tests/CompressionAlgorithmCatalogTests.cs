using Lyo.Common.Core.Net;
using Lyo.Compression.Models;
using Lyo.Compression.Zstd;

namespace Lyo.Compression.Tests;

/// <summary>Covers the catalog metadata algorithms carry so call sites stop maintaining their own magic-number and content-encoding tables.</summary>
public class CompressionAlgorithmCatalogTests
{
    [Fact]
    public void ContentEncoding_MatchesTheSharedHttpTokens()
    {
        Assert.Equal(LyoContentEncodings.GZip, CompressionAlgorithm.GZip.ContentEncoding);
        Assert.Equal(LyoContentEncodings.Deflate, CompressionAlgorithm.Deflate.ContentEncoding);
        Assert.Equal(LyoContentEncodings.Brotli, CompressionAlgorithm.Brotli.ContentEncoding);
        Assert.Equal(LyoContentEncodings.Zstd, ZstdCompressionAlgorithm.Instance.ContentEncoding);
    }

    [Fact]
    public void ContentEncoding_IsNullForFormatsWithNoHttpToken() => Assert.Null(CompressionAlgorithm.ZLib.ContentEncoding);

    [Fact]
    public void TryFromContentEncoding_IsCaseInsensitiveAndTrims() => Assert.Same(CompressionAlgorithm.GZip, CompressionAlgorithm.TryFromContentEncoding(" GZIP "));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("identity")]
    public void TryFromContentEncoding_UnknownOrBlank_ReturnsNull(string? token) => Assert.Null(CompressionAlgorithm.TryFromContentEncoding(token));

    [Fact]
    public void MatchesMagic_RecognizesOwnHeaderOnly()
    {
        Assert.True(CompressionAlgorithm.GZip.MatchesMagic([0x1F, 0x8B, 0x08]));
        Assert.False(CompressionAlgorithm.GZip.MatchesMagic([0x78, 0x9C]));
        Assert.True(CompressionAlgorithm.ZLib.MatchesMagic([0x78, 0xDA, 0x00]));
    }

    [Fact]
    public void MatchesMagic_HeaderlessFormats_NeverMatch()
    {
        Assert.Empty(CompressionAlgorithm.Deflate.MagicPrefixes);
        Assert.Empty(CompressionAlgorithm.Brotli.MagicPrefixes);
        Assert.False(CompressionAlgorithm.Brotli.MatchesMagic([0x81, 0x00, 0x00, 0x00]));
    }

    [Fact]
    public void MatchesMagic_ShorterThanPrefix_DoesNotMatch() => Assert.False(CompressionAlgorithm.GZip.MatchesMagic([0x1F]));

    [Fact]
    public void Detectable_OrdersLongestPrefixFirstAndExcludesHeaderlessFormats()
    {
        var detectable = CompressionAlgorithm.Detectable;
        Assert.DoesNotContain(CompressionAlgorithm.Brotli, detectable);
        Assert.Contains(CompressionAlgorithm.GZip, detectable);

        var lengths = detectable.Select(a => a.MagicPrefixes.Max(p => p.Length)).ToArray();
        Assert.Equal(lengths.OrderByDescending(l => l), lengths);
    }

    [Fact]
    public void FileType_ResolvesFromTheSharedFileTypeCatalog()
    {
        Assert.Equal("application/gzip", CompressionAlgorithm.GZip.FileType.MimeType);
        Assert.Equal(".gz", CompressionAlgorithm.GZip.FileType.DefaultExtension);
    }
}
