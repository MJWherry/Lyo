using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json.Serialization;

namespace Lyo.Compression.Models;

[DebuggerDisplay("{ToString(),nq}")]
public class CompressionServiceOptions
{
    public const string SectionName = "CompressionOptions";

    public int MaxParallelFileOperations { get; set; } = Environment.ProcessorCount;

    public string DefaultEncoding { get; set; } = "utf-8";

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public CompressionLevel DefaultCompressionLevel { get; set; } = CompressionLevel.Optimal;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public CompressionAlgorithm DefaultAlgorithm { get; set; }
#if NETSTANDARD2_0
        = CompressionAlgorithm.GZip;
#else
        = CompressionAlgorithm.Brotli;
#endif

    // Optimal buffer sizes for file I/O (64KB - good balance between memory and performance)
    public int DefaultFileBufferSize { get; set; } = 65536;

    public int AsyncFileBufferSize { get; set; } = 65536;

    /// <summary>Maximum allowed input size in bytes for compression/decompression operations. Defaults to 10 GB to prevent denial-of-service attacks from extremely large inputs.</summary>
    public long MaxInputSize { get; set; } = 10L * 1024 * 1024 * 1024; // 10 GB

    /// <summary>Enable metrics collection for compression operations. Default: false</summary>
    public bool EnableMetrics { get; set; } = false;

    public override string ToString()
        => $"CompressionServiceOptions: MaxParallelFileOperations={MaxParallelFileOperations}, DefaultEncoding={DefaultEncoding}, DefaultCompressionLevel={DefaultCompressionLevel}, Algorithm={DefaultAlgorithm}, DefaultFileBufferSize={DefaultFileBufferSize}, AsyncFileBufferSize={AsyncFileBufferSize}, MaxInputSize={MaxInputSize}, EnableMetrics={EnableMetrics}";
}