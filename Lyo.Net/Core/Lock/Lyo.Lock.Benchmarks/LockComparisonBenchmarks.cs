using BenchmarkDotNet.Attributes;
using Lyo.Benchmark;
using Lyo.Lock.Abstractions;
using Lyo.Lock.Redis;
using Microsoft.Extensions.Logging.Abstractions;
using StackExchange.Redis;
using Testcontainers.Redis;

namespace Lyo.Lock.Benchmarks;

/// <summary>
/// Compares uncontended lock operations between the in-process <see cref="LocalLockService" /> and the distributed <see cref="RedisLockService" />. Requires Docker; a
/// throwaway Redis container is started in <see cref="GlobalSetup" />.
/// </summary>
[BenchmarkDescription(
    "Uncontended acquire/release and execute-with-lock on a unique key, comparing in-process LocalLockService against distributed RedisLockService (throwaway Docker Redis). Isolates per-operation lock cost.")]
public class LockComparisonBenchmarks
{
    private ILockService _local = null!;
    private IConnectionMultiplexer _redis = null!;
    private RedisContainer _redisContainer = null!;
    private ILockService _redisLock = null!;

    [GlobalSetup]
    public void Setup()
    {
        _redisContainer = new RedisBuilder("redis:7-alpine").Build();
        _redisContainer.StartAsync().GetAwaiter().GetResult();
        _redis = ConnectionMultiplexer.Connect(_redisContainer.GetConnectionString());
        _local = new LocalLockService(NullLogger<LocalLockService>.Instance, new());
        _redisLock = new RedisLockService(_redis, NullLogger<RedisLockService>.Instance, new());
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _redis.Dispose();
        _redisContainer.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    [Benchmark(Baseline = true)]
    [BenchmarkSla(MaxMeanUs = 10, Standard = "An uncontended in-process lock acquire/release should be a few microseconds (no network).")]
    public async Task Local_AcquireRelease()
    {
        var handle = await _local.AcquireAsync($"local-{Guid.NewGuid():N}", TimeSpan.FromSeconds(5));
        await handle!.ReleaseAsync();
    }

    [Benchmark]
    [BenchmarkSla(
        MaxMeanMs = 5, Standard = "A distributed Redis lock acquire/release is bounded by a network round-trip and should stay within a few milliseconds on a local network.")]
    public async Task Redis_AcquireRelease()
    {
        var handle = await _redisLock.AcquireAsync($"redis-{Guid.NewGuid():N}", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30));
        await handle!.ReleaseAsync();
    }

    [Benchmark]
    [BenchmarkSla(MaxMeanUs = 15, Standard = "In-process execute-with-lock adds only local coordination and should stay in the low tens of microseconds.")]
    public async Task Local_ExecuteWithLock() => await _local.ExecuteWithLockAsync($"local-exec-{Guid.NewGuid():N}", _ => Task.CompletedTask, TimeSpan.FromSeconds(5));

    [Benchmark]
    [BenchmarkSla(MaxMeanMs = 5, Standard = "Distributed execute-with-lock is bounded by Redis round-trips and should stay within a few milliseconds on a local network.")]
    public async Task Redis_ExecuteWithLock()
        => await _redisLock.ExecuteWithLockAsync($"redis-exec-{Guid.NewGuid():N}", _ => Task.CompletedTask, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30));
}