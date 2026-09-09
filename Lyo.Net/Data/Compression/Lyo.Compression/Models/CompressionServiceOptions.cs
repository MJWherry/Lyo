using System.Diagnostics;
using System.IO.Compression;

namespace Lyo.Compression.Models;

/// <summary>Mutable options used to build <see cref="Lyo.Compression.CompressionService" />; also bindable from configuration.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class CompressionServiceOptions
{
    /// <summary>Recommended appsettings section name when binding this type.</summary>
    public const string SectionName = "CompressionOptions";

    /// <summary>Fallback options for <see cref="ICompressionResolver" /> / <see cref="CompressionService" /> when none are supplied.</summary>
    public static CompressionServiceOptions Default { get; } = new();

    /// <summary>Max concurrent file operations in batch compress/decompress APIs.</summary>
    public int MaxParallelFileOperations { get; set; } = Environment.ProcessorCount;

    /// <summary>Encoding name for string helpers when <c>encoding</c> is omitted (e.g. <c>utf-8</c>); invalid names fall back to UTF-8 at runtime.</summary>
    public string DefaultEncoding { get; set; } = "utf-8";

    /// <summary>Starting <see cref="CompressionLevel" /> passed to codecs that support it (GZip, Brotli, Deflate, ZLib, BZip2 mapping).</summary>
    public CompressionLevel DefaultCompressionLevel { get; set; } = CompressionLevel.Optimal;

    /// <summary>Codec for this service instance. On <c>netstandard2.0</c>, <see cref="CompressionAlgorithm.Brotli" /> and <see cref="CompressionAlgorithm.ZLib" /> are unavailable.</summary>
    public CompressionAlgorithm DefaultAlgorithm { get; set; } =
#if NETSTANDARD2_0
        CompressionAlgorithm.GZip;
#else
        CompressionAlgorithm.Brotli;
#endif

    /// <summary>Buffer size for sync buffered file streams (minimum 1024 enforced in <see cref="Lyo.Compression.CompressionService" /> ctor).</summary>
    public int DefaultFileBufferSize { get; set; } = 65536;

    /// <summary>Buffer size for async file streams (minimum 1024 enforced in <see cref="Lyo.Compression.CompressionService" /> ctor).</summary>
    public int AsyncFileBufferSize { get; set; } = 65536;

    /// <summary>Max compressed input length and max allowed decompressed output size (bytes).</summary>
    public long MaxInputSize { get; set; } = 10L * 1024 * 1024 * 1024; // 10 GB cap.

    /// <summary>
    /// If <see langword="true" />, record compression metrics when a non-null <see cref="Lyo.Metrics.IMetrics" /> is supplied to
    /// <see cref="Lyo.Compression.CompressionService" />.
    /// </summary>
    public bool EnableMetrics { get; set; } = false;

    public override string ToString()
        => $"CompressionServiceOptions: MaxParallelFileOperations={MaxParallelFileOperations}, DefaultEncoding={DefaultEncoding}, DefaultCompressionLevel={DefaultCompressionLevel}, Algorithm={DefaultAlgorithm}, DefaultFileBufferSize={DefaultFileBufferSize}, AsyncFileBufferSize={AsyncFileBufferSize}, MaxInputSize={MaxInputSize}, EnableMetrics={EnableMetrics}";
}