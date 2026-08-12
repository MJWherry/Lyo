using Lyo.Compression;
using Lyo.Compression.BZip2;
using Lyo.Compression.Compressors;
using Lyo.Compression.Lz4;
using Lyo.Compression.Lzma;
using Lyo.Compression.Models;
using Lyo.Compression.Snappier;
using Lyo.Compression.Xz;
using Lyo.Compression.Zstd;
using Lyo.Exceptions;

namespace Lyo.Cli.Services;

/// <summary>Compression helpers with all built-in + addon factories registered (Gateway-style).</summary>
internal static class CliCompression
{
    private static readonly ICompressorFactory[] AllFactories = [
        new GZipCompressorFactory(), new DeflateCompressorFactory(), new BrotliCompressorFactory(), new ZLibCompressorFactory(), new Lz4CompressorFactory(),
        new LzmaCompressorFactory(), new SnappierCompressorFactory(), new ZstdCompressorFactory(), new BZip2CompressorFactory(), new XzCompressorFactory()
    ];

    static CliCompression()
    {
        // Touch addon algorithm singletons so TryFromName can resolve them.
        _ = Lz4CompressionAlgorithm.Instance;
        _ = LzmaCompressionAlgorithm.Instance;
        _ = SnappierCompressionAlgorithm.Instance;
        _ = ZstdCompressionAlgorithm.Instance;
        _ = BZip2CompressionAlgorithm.Instance;
        _ = XzCompressionAlgorithm.Instance;
    }

    public static CompressionService CreateService(CompressionAlgorithm? defaultAlgorithm = null)
        => new(AllFactories, options: new() { DefaultAlgorithm = defaultAlgorithm ?? CompressionAlgorithm.Brotli });

    public static CompressionAlgorithm ParseAlgorithm(string? name)
    {
        name = string.IsNullOrWhiteSpace(name) ? "brotli" : name.Trim();
        var algo = CompressionAlgorithm.TryFromName(name) ?? CompressionAlgorithm.TryFromName(char.ToUpperInvariant(name[0]) + name[1..].ToLowerInvariant());
        if (algo is not null)
            return algo;

        // Common aliases
        algo = name.ToLowerInvariant() switch {
            "gz" or "gzip" => CompressionAlgorithm.GZip,
            "br" or "brotli" => CompressionAlgorithm.Brotli,
            "zz" or "zlib" => CompressionAlgorithm.ZLib,
            "deflate" => CompressionAlgorithm.Deflate,
            "lz4" => Lz4CompressionAlgorithm.Instance,
            "zstd" or "zst" => ZstdCompressionAlgorithm.Instance,
            "snappy" or "snappier" => SnappierCompressionAlgorithm.Instance,
            "lzma" => LzmaCompressionAlgorithm.Instance,
            "bz2" or "bzip2" => BZip2CompressionAlgorithm.Instance,
            "xz" => XzCompressionAlgorithm.Instance,
            var _ => null
        };

        ArgumentHelpers.ThrowIf(algo is null, $"Unknown compression algorithm '{name}'.");
        return algo!;
    }

    public static async Task CompressAsync(Stream input, Stream output, CompressionAlgorithm algorithm, CancellationToken ct)
    {
        var service = CreateService(algorithm);
        await service.CompressAsync(input, output, algorithm, ct: ct).ConfigureAwait(false);
    }

    public static async Task DecompressAsync(Stream input, Stream output, CompressionAlgorithm? algorithm, CancellationToken ct)
    {
        var service = CreateService(algorithm ?? CompressionAlgorithm.Brotli);
        if (algorithm is not null)
            await service.DecompressAsync(input, output, algorithm, ct: ct).ConfigureAwait(false);
        else
            await service.DecompressAsync(input, output, ct: ct).ConfigureAwait(false);
    }

    public static string FileExtension(CompressionAlgorithm algorithm) => algorithm.Extension;
}