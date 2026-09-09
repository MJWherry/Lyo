using Testcontainers.Redis;

namespace Lyo.Testing.Containers;

/// <summary>Owns a Testcontainers Redis instance: start once, expose the connection string, dispose async.</summary>
public sealed class RedisTestContainer : IAsyncDisposable
{
    private readonly RedisContainer _container;
    private bool _started;

    /// <summary>StackExchange.Redis-compatible connection string once the container is running.</summary>
    public string ConnectionString => _started ? _container.GetConnectionString() : throw new InvalidOperationException("Call StartAsync before reading ConnectionString.");

    public RedisTestContainer(RedisContainerOptions? options = null)
    {
        options ??= new();
        var builder = new RedisBuilder(options.Image);
        options.ConfigureBuilder?.Invoke(builder);
        _container = builder.Build();
    }

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();

    public async ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        await _container.StartAsync(cancellationToken);
        _started = true;
    }
}
