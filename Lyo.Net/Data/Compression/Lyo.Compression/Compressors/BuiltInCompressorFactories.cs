using System.IO.Compression;
using EasyCompressor;
using Lyo.Compression.Models;

namespace Lyo.Compression.Compressors;

/// <summary>Built-in <see cref="ICompressorFactory" /> implementations for algorithms shipped in the base <c>Lyo.Compression</c> package (BCL codecs only).</summary>
internal static class CompressorFactoryConstants
{
    /// <summary>Name passed to EasyCompressor compressors; shown in diagnostics.</summary>
    public const string CompressorName = "CompressionService";
}

/// <summary>Factory for <see cref="CompressionAlgorithm.GZip" /> using <see cref="GZipCompressor" />.</summary>
public sealed class GZipCompressorFactory : ICompressorFactory
{
    public CompressionAlgorithm Algorithm => CompressionAlgorithm.GZip;

    public ICompressor Create(CompressionLevel level) => new GZipCompressor(CompressorFactoryConstants.CompressorName, level);
}

/// <summary>Factory for <see cref="CompressionAlgorithm.Deflate" /> using <see cref="DeflateCompressor" />.</summary>
public sealed class DeflateCompressorFactory : ICompressorFactory
{
    public CompressionAlgorithm Algorithm => CompressionAlgorithm.Deflate;

    public ICompressor Create(CompressionLevel level) => new DeflateCompressor(CompressorFactoryConstants.CompressorName, level);
}

#if !NETSTANDARD2_0
/// <summary>Factory for <see cref="CompressionAlgorithm.Brotli" /> backed by <see cref="BrotliCompressor" />. Unavailable on <c>netstandard2.0</c>.</summary>
public sealed class BrotliCompressorFactory : ICompressorFactory
{
    public CompressionAlgorithm Algorithm => CompressionAlgorithm.Brotli;

    public ICompressor Create(CompressionLevel level) => new BrotliCompressor(CompressorFactoryConstants.CompressorName, level);
}

/// <summary>Factory for <see cref="CompressionAlgorithm.ZLib" /> backed by <see cref="ZLibCompressor" />. Unavailable on <c>netstandard2.0</c>.</summary>
public sealed class ZLibCompressorFactory : ICompressorFactory
{
    public CompressionAlgorithm Algorithm => CompressionAlgorithm.ZLib;

    public ICompressor Create(CompressionLevel level) => new ZLibCompressor(CompressorFactoryConstants.CompressorName, level);
}
#endif