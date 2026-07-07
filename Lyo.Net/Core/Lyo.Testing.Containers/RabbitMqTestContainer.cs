using Testcontainers.RabbitMq;

namespace Lyo.Testing.Containers;

/// <summary>
/// Owns a Testcontainers RabbitMQ instance (management-enabled image): start once, expose AMQP + management endpoints, dispose async. The management HTTP API
/// (<see cref="AdminUrl" />) backs peek and queue-statistics operations.
/// </summary>
public sealed class RabbitMqTestContainer : IAsyncDisposable
{
    private const int AmqpPort = 5672;
    private const int ManagementPort = 15672;

    /// <summary>Username configured on the container.</summary>
    public const string DefaultUsername = "rabbitmq";

    /// <summary>Password configured on the container.</summary>
    public const string DefaultPassword = "rabbitmq";

    private readonly RabbitMqContainer _container;
    private bool _started;

    /// <summary>Host name for AMQP connections after the container has started.</summary>
    public string Host => StartedContainer.Hostname;

    /// <summary>Mapped AMQP port after the container has started.</summary>
    public int Port => StartedContainer.GetMappedPublicPort(AmqpPort);

    /// <summary>Management API base URL (http://host:port) after the container has started.</summary>
    public string AdminUrl => $"http://{StartedContainer.Hostname}:{_container.GetMappedPublicPort(ManagementPort)}";

    /// <summary>AMQP connection string (amqp://user:pass@host:port) after the container has started.</summary>
    public string ConnectionString => StartedContainer.GetConnectionString();

    public string Username => DefaultUsername;

    public string Password => DefaultPassword;

    private RabbitMqContainer StartedContainer => _started ? _container : throw new InvalidOperationException("Call StartAsync before reading container endpoints.");

    public RabbitMqTestContainer(RabbitMqContainerOptions? options = null)
    {
        options ??= new();
        var builder = new RabbitMqBuilder(options.Image)
            .WithUsername(DefaultUsername)
            .WithPassword(DefaultPassword)
            .WithPortBinding(ManagementPort, true);

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
