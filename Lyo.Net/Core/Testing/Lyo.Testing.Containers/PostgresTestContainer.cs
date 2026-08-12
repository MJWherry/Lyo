using Testcontainers.PostgreSql;

namespace Lyo.Testing.Containers;

/// <summary>Owns a Testcontainers PostgreSQL instance: start once, expose connection string, dispose async.</summary>
public sealed class PostgresTestContainer : IAsyncDisposable
{
    private readonly PostgreSqlContainer _container;
    private bool _started;

    public string ConnectionString => _started ? _container.GetConnectionString() : throw new InvalidOperationException("Call StartAsync before reading ConnectionString.");

    public PostgresTestContainer(PostgresContainerOptions? options = null)
    {
        options ??= new();
        var builder = new PostgreSqlBuilder(options.Image);
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