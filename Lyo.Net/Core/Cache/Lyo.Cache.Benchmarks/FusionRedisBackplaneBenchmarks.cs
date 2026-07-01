using BenchmarkDotNet.Attributes;
using Lyo.Benchmarking;
using Lyo.Cache.Fusion;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.Redis;

namespace Lyo.Cache.Benchmarks;

/// <summary>
/// Measures FusionCache operations with a StackExchange Redis backplane attached, to surface backplane overhead vs the in-process FusionCache. Requires Docker; spins up a
/// throwaway Redis container in <see cref="GlobalSetup" />.
/// </summary>
[BenchmarkDescription(
    "FusionCache get/set/invalidate-by-tag with a StackExchange Redis backplane (throwaway Docker Redis), surfacing backplane overhead vs the in-process FusionCache.")]
[BenchmarkDataShape(typeof(CachePayload), Notes = "Small POCO cached as the object value.")]
public class FusionRedisBackplaneBenchmarks
{
    private const string HitKey = "redis-backplane-hit";
    private ICacheService _cache = null!;
    private ServiceProvider _provider = null!;
    private RedisContainer _redis = null!;
    private CachePayload _value = null!;

    [GlobalSetup]
    public void Setup()
    {
        _redis = new RedisBuilder().WithImage("redis:7-alpine").Build();
        _redis.StartAsync().GetAwaiter().GetResult();
        var services = new ServiceCollection();
        services.AddFusionCache(_redis.GetConnectionString(), o => o.Enabled = true);
        _provider = services.BuildServiceProvider();
        _cache = _provider.GetRequiredService<ICacheService>();
        _value = new() { Id = 1, Name = "redis", Data = "backplane-value" };
        _cache.Set(HitKey, _value);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _provider.Dispose();
        _redis.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    [Benchmark]
    public CachePayload? Fusion_Redis_GetOrSet_Hit() => _cache.GetOrSet<CachePayload>(HitKey, _ => _value);

    [Benchmark]
    public void Fusion_Redis_Set() => _cache.Set("redis-backplane-set", _value);

    [Benchmark]
    public async Task Fusion_Redis_InvalidateByTag() => await _cache.InvalidateCacheItemByTag("redis-tag");
}