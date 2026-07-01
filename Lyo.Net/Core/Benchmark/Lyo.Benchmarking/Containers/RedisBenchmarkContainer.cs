using Testcontainers.Redis;

namespace Lyo.Benchmarking.Containers;

/// <summary>
/// Thin wrapper around a throwaway Redis Testcontainers container for Docker-dependent benchmarks (cache backplane, distributed lock). Start it in <c>[GlobalSetup]</c> and
/// dispose in <c>[GlobalCleanup]</c>.
/// </summary>
public sealed class RedisBenchmarkContainer : IDisposable
{
    private readonly RedisContainer _container;

    /// <summary>StackExchange.Redis connection string for the running container.</summary>
    public string ConnectionString => _container.GetConnectionString();

    /// <summary>Creates (but does not start) a Redis container using the given image.</summary>
    public RedisBenchmarkContainer(string image = "redis:7-alpine") => _container = new RedisBuilder(image).Build();

    /// <summary>Stops and disposes the underlying container.</summary>
    public void Dispose() => _container.DisposeAsync().AsTask().GetAwaiter().GetResult();

    /// <summary>Starts the container synchronously (suitable for BenchmarkDotNet GlobalSetup).</summary>
    public RedisBenchmarkContainer Start()
    {
        _container.StartAsync().GetAwaiter().GetResult();
        return this;
    }
}