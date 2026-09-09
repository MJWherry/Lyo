using Testcontainers.Redis;

namespace Lyo.Benchmark.Containers;

/// <summary>
/// Thin wrapper around a disposable Redis Testcontainers instance for Docker-backed benchmarks (cache backplane, distributed lock). Start it from <c>[GlobalSetup]</c>
/// and dispose it from <c>[GlobalCleanup]</c>.
/// </summary>
public sealed class RedisBenchmarkContainer : IDisposable
{
    private readonly RedisContainer _container;

    /// <summary>StackExchange.Redis connection string for the live container.</summary>
    public string ConnectionString => _container.GetConnectionString();

    /// <summary>Builds (but does not start) a Redis container from the given image.</summary>
    public RedisBenchmarkContainer(string image = "redis:7-alpine") => _container = new RedisBuilder(image).Build();

    /// <summary>Stops and disposes the inner container.</summary>
    public void Dispose() => _container.DisposeAsync().AsTask().GetAwaiter().GetResult();

    /// <summary>Starts the container on the calling thread (fits BenchmarkDotNet GlobalSetup).</summary>
    public RedisBenchmarkContainer Start()
    {
        _container.StartAsync().GetAwaiter().GetResult();
        return this;
    }
}