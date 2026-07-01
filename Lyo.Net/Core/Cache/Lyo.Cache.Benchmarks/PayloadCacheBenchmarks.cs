using System.Text;
using BenchmarkDotNet.Attributes;
using Lyo.Benchmarking;

namespace Lyo.Cache.Benchmarks;

/// <summary>Benchmarks the serialized byte-payload cache path (JSON + optional compression framing) across payload sizes, using a nested payload graph.</summary>
[BenchmarkDescription(
    "Serialized byte-payload cache path (JSON +/- compression framing) on a warm key, comparing no-compress vs auto-compress at increasing payload sizes up to 10 MB. The cached value is a nested NestedCachePayload whose Data body carries DataSize compressible bytes, so this exercises caching of large, structured outputs.")]
[BenchmarkParameter(
    "DataSize", Unit = "bytes",
    Description =
        "Length of the compressible string body inside the payload (1 KB, 64 KB, 1 MB, 10 MB) - caching is frequently used for large outputs, so the matrix scales accordingly.")]
[BenchmarkDataShape(typeof(NestedCachePayload), Notes = "Nested graph (Address -> Geo object, Contacts collection, Attributes dictionary) plus a DataSize-byte compressible body.")]
[BenchmarkSla(
    MaxMeanMs = 50, Standard = "In-process payload cache operations should complete well within a typical web request budget (<= 50 ms), even for multi-MB structured bodies.")]
public class PayloadCacheBenchmarks
{
    private const string PlainKey = "payload-plain";
    private const string CompressedKey = "payload-compressed";
    private ICacheService _compressed = null!;
    private ICacheService _plain = null!;
    private NestedCachePayload _value = null!;

    [Params(1024, 64 * 1024, 1024 * 1024, 10 * 1024 * 1024)]
    public int DataSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _plain = CacheBenchmarkSupport.CreateLocal(o => o.Payload.AutoCompress = false);
        _compressed = CacheBenchmarkSupport.CreateLocal(o => {
            o.Payload.AutoCompress = true;
            o.Payload.AutoCompressMinSizeBytes = 256;
        });

        _value = CacheBenchmarkSupport.GenerateNested(DataSize);
        // Pre-populate so the benchmarked calls exercise the read/decode/deserialize path.
        _ = _plain.GetOrSetPayloadAsync<NestedCachePayload>(PlainKey, _ => Task.FromResult<NestedCachePayload?>(_value)).AsTask().GetAwaiter().GetResult();
        _ = _compressed.GetOrSetPayloadAsync<NestedCachePayload>(CompressedKey, _ => Task.FromResult<NestedCachePayload?>(_value)).AsTask().GetAwaiter().GetResult();
    }

    [Benchmark(Baseline = true)]
    [BenchmarkDescription("Read + decode + deserialize the nested payload from the uncompressed cache entry (baseline).")]
    public async ValueTask<NestedCachePayload?> Payload_NoCompress_Hit()
        => await _plain.GetOrSetPayloadAsync<NestedCachePayload>(PlainKey, _ => Task.FromResult<NestedCachePayload?>(_value));

    [Benchmark]
    [BenchmarkDescription("Read + decompress + deserialize the nested payload from the auto-compressed cache entry.")]
    public async ValueTask<NestedCachePayload?> Payload_Compress_Hit()
        => await _compressed.GetOrSetPayloadAsync<NestedCachePayload>(CompressedKey, _ => Task.FromResult<NestedCachePayload?>(_value));

    [Benchmark]
    [BenchmarkDescription("Write the DataSize body bytes to the uncompressed payload cache.")]
    public void Payload_NoCompress_Set() => _plain.SetPayload("payload-set-plain", Encoding.UTF8.GetBytes(_value.Data));

    [Benchmark]
    [BenchmarkDescription("Write the DataSize body bytes to the auto-compressing payload cache (compression overhead on write).")]
    public void Payload_Compress_Set() => _compressed.SetPayload("payload-set-compressed", Encoding.UTF8.GetBytes(_value.Data));
}