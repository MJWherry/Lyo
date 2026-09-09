namespace Lyo.Testing.Containers;

/// <summary>
/// xUnit fixture base: starts a shared Redis container, then runs <see cref="OnContainerStartedAsync" />; teardown runs <see cref="OnContainerDisposingAsync" /> before
/// the container stops. Use with <c>IClassFixture</c> or <c>[assembly: AssemblyFixture(typeof(T))]</c>.
/// </summary>
public abstract class RedisContainerFixtureBase : IAsyncLifetime
{
    /// <summary>The container resource, started after <see cref="InitializeAsync" />.</summary>
    protected RedisTestContainer Container { get; }

    /// <summary>Connection string once the container is running.</summary>
    public string ConnectionString => Container.ConnectionString;

    protected RedisContainerFixtureBase()
        : this(null) { }

    protected RedisContainerFixtureBase(RedisContainerOptions? options) => Container = new(options);

    public async ValueTask InitializeAsync()
    {
        await Container.StartAsync(TestContext.Current.CancellationToken);
        await OnContainerStartedAsync(ConnectionString, TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await OnContainerDisposingAsync(TestContext.Current.CancellationToken);
        await Container.DisposeAsync();
    }

    /// <summary>Called after the container is up and <see cref="ConnectionString" /> is valid (for example build a service provider against it).</summary>
    protected virtual ValueTask OnContainerStartedAsync(string connectionString, CancellationToken cancellationToken) => ValueTask.CompletedTask;

    /// <summary>Called before the container is disposed (for example tear down clients that still need the server).</summary>
    protected virtual ValueTask OnContainerDisposingAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;
}
