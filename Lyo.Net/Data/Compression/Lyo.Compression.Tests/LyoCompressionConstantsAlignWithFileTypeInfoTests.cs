using Lyo.Common.Records;
using Lyo.Compression.BZip2;
using Lyo.Compression.Lz4;
using Lyo.Compression.Lzma;
using Lyo.Compression.Models;
using Lyo.Compression.Snappier;
using Lyo.Compression.Xz;
using Lyo.Compression.Zstd;

namespace Lyo.Compression.Tests;

public class LyoCompressionConstantsAlignWithFileTypeInfoTests
{
    [Fact]
    public void CompressionAlgorithm_Extensions_Match_FileTypeInfo_StreamDefaults()
    {
        Assert.Equal(FileTypeInfo.Gz.DefaultExtension, CompressionAlgorithm.GZip.Extension);
        Assert.Equal(FileTypeInfo.Brotli.DefaultExtension, CompressionAlgorithm.Brotli.Extension);
        Assert.Equal(FileTypeInfo.ZLibStream.DefaultExtension, CompressionAlgorithm.ZLib.Extension);
        Assert.Equal(FileTypeInfo.DeflateStream.DefaultExtension, CompressionAlgorithm.Deflate.Extension);
        Assert.Equal(FileTypeInfo.LZ4Stream.DefaultExtension, Lz4CompressionAlgorithm.Instance.Extension);
        Assert.Equal(FileTypeInfo.LZMAStream.DefaultExtension, LzmaCompressionAlgorithm.Instance.Extension);
        Assert.Equal(FileTypeInfo.SnappyStream.DefaultExtension, SnappierCompressionAlgorithm.Instance.Extension);
        Assert.Equal(FileTypeInfo.ZstdStream.DefaultExtension, ZstdCompressionAlgorithm.Instance.Extension);
        Assert.Equal(FileTypeInfo.Bz2.DefaultExtension, BZip2CompressionAlgorithm.Instance.Extension);
        Assert.Equal(FileTypeInfo.Xz.DefaultExtension, XzCompressionAlgorithm.Instance.Extension);
    }
}