namespace Lyo.Testing.Containers;

/// <summary>
/// xUnit fixture base: starts a shared RabbitMQ container, then runs <see cref="OnContainerStartedAsync" />; teardown runs <see cref="OnContainerDisposingAsync" /> before
/// the container stops. Use with <c>IClassFixture</c> or <c>[assembly: AssemblyFixture(typeof(T))]</c>.
/// </summary>
public abstract class RabbitMqContainerFixtureBase : IAsyncLifetime
{
    /// <summary>The container resource, started after <see cref="InitializeAsync" />.</summary>
    protected RabbitMqTestContainer Container { get; }

    /// <summary>Host name for AMQP connections once the container is running.</summary>
    public string Host => Container.Host;

    /// <summary>Mapped AMQP port once the container is running.</summary>
    public int Port => Container.Port;

    /// <summary>Management API base URL once the container is running.</summary>
    public string AdminUrl => Container.AdminUrl;

    public string Username => Container.Username;

    public string Password => Container.Password;

    protected RabbitMqContainerFixtureBase()
        : this(null) { }

    protected RabbitMqContainerFixtureBase(RabbitMqContainerOptions? options) => Container = new(options);

    public async ValueTask InitializeAsync()
    {
        await Container.StartAsync(TestContext.Current.CancellationToken);
        await OnContainerStartedAsync(Container, TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await OnContainerDisposingAsync(TestContext.Current.CancellationToken);
        await Container.DisposeAsync();
    }

    /// <summary>Called after the container is up and its endpoints are valid (for example declare shared topology).</summary>
    protected virtual ValueTask OnContainerStartedAsync(RabbitMqTestContainer container, CancellationToken cancellationToken) => ValueTask.CompletedTask;

    /// <summary>Called before the container is disposed (for example tear down clients that still need the broker).</summary>
    protected virtual ValueTask OnContainerDisposingAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;
}