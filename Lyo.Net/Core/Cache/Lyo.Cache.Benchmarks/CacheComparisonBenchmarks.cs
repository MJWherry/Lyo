using BenchmarkDotNet.Attributes;
using Lyo.Benchmark;

namespace Lyo.Cache.Benchmarks;

/// <summary>Compares object-cache operations between the local IMemoryCache implementation and FusionCache (no Redis backplane).</summary>
[BenchmarkDescription(
    "Object-cache get/set/try-get on a warm key, comparing local IMemoryCache against in-process FusionCache (no Redis backplane). Values are a nested NestedCachePayload (object graph + collection + dictionary) so the comparison reflects realistic cached entities rather than a trivial scalar.")]
[BenchmarkDataShape(
    typeof(NestedCachePayload), Notes = "Nested graph (Address -> Geo object, Contacts collection, Attributes dictionary) cached as the object value across the comparison.")]
[BenchmarkSla(MaxMeanUs = 5, Standard = "In-process object cache hit/set should stay low-microsecond - no network and no large-graph serialization on the hot path.")]
public class CacheComparisonBenchmarks
{
    private const string HitKey = "comparison-hit";
    private ICacheService _fusion = null!;
    private ICacheService _local = null!;
    private NestedCachePayload _value = null!;

    [GlobalSetup]
    public void Setup()
    {
        _local = CacheBenchmarkSupport.CreateLocal();
        _fusion = CacheBenchmarkSupport.CreateFusion();
        _value = CacheBenchmarkSupport.GenerateNested(512);
        _local.Set(HitKey, _value);
        _fusion.Set(HitKey, _value);
    }

    [Benchmark(Baseline = true)]
    public NestedCachePayload? Local_GetOrSet_Hit() => _local.GetOrSet<NestedCachePayload>(HitKey, _ => _value);

    [Benchmark]
    public NestedCachePayload? Fusion_GetOrSet_Hit() => _fusion.GetOrSet<NestedCachePayload>(HitKey, _ => _value);

    [Benchmark]
    public async ValueTask<NestedCachePayload?> Local_GetOrSetAsync_Hit()
        => await _local.GetOrSetAsync<NestedCachePayload>(HitKey, _ => Task.FromResult<NestedCachePayload?>(_value));

    [Benchmark]
    public async ValueTask<NestedCachePayload?> Fusion_GetOrSetAsync_Hit()
        => await _fusion.GetOrSetAsync<NestedCachePayload>(HitKey, _ => Task.FromResult<NestedCachePayload?>(_value));

    [Benchmark]
    public void Local_Set() => _local.Set("comparison-set-local", _value);

    [Benchmark]
    public void Fusion_Set() => _fusion.Set("comparison-set-fusion", _value);

    [Benchmark]
    public bool Local_TryGetValue() => _local.TryGetValue<NestedCachePayload>(HitKey, out var _);

    [Benchmark]
    public bool Fusion_TryGetValue() => _fusion.TryGetValue<NestedCachePayload>(HitKey, out var _);
}