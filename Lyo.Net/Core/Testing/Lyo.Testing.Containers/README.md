# Lyo.Testing.Containers

xUnit v3 fixtures around Testcontainers. The shipped helpers start and dispose PostgreSQL and RabbitMQ containers. Other backends can follow the same shape.

Internal-only: `IsPackable` is `false` and `xunit.v3.extensibility.core` is a project-level dependency. Reference this project from test projects. Do not pack it.

## Examples

### xUnit v3 class fixture

```csharp
using Lyo.Testing.Containers;

public sealed class MyFeatureFixture : PostgresContainerFixtureBase
{
    protected override async ValueTask OnContainerStartedAsync(string connectionString, CancellationToken ct)
    {
        // e.g. apply EF Core migrations against the new database
        using var ctx = new MyDbContext(BuildContextOptions(connectionString));
        await ctx.Database.MigrateAsync(ct);
    }

    private static DbContextOptions<MyDbContext> BuildContextOptions(string connectionString) =>
        new DbContextOptionsBuilder<MyDbContext>().UseNpgsql(connectionString).Options;
}

public sealed class MyFeatureTests(MyFeatureFixture fixture) : IClassFixture<MyFeatureFixture>
{
    [Fact]
    public async Task ConnectsAndQueries()
    {
        using var ctx = new MyDbContext(new DbContextOptionsBuilder<MyDbContext>()
            .UseNpgsql(fixture.ConnectionString).Options);
        Assert.NotNull(await ctx.Database.GetDbConnection().ServerVersionAsync());
    }
}
```

### Customizing the container

```csharp
public sealed class TrgmFixture() : PostgresContainerFixtureBase(new PostgresContainerOptions {
    Image = "postgres:17-alpine",
    ConfigureBuilder = builder => builder
        .WithEnvironment("POSTGRES_DB", "lyo_tests")
        .WithCommand("postgres", "-c", "shared_preload_libraries=pg_trgm"),
});
```

### Standalone use (no fixture)

```csharp
await using var container = new PostgresTestContainer();
await container.StartAsync(ct);
var connectionString = container.ConnectionString;

await using var broker = new RabbitMqTestContainer();
await broker.StartAsync(ct);
var (host, port, adminUrl) = (broker.Host, broker.Port, broker.AdminUrl);
```

## Types

| Type | Role |
| ------------------------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `PostgresTestContainer` | `IAsyncDisposable` wrapper around `PostgreSqlContainer`. Call `StartAsync(CancellationToken)` once, then read `ConnectionString`. Throws `InvalidOperationException` when `ConnectionString` is read before `StartAsync`. |
| `PostgresContainerOptions` | `Image` (defaults to `postgres:16-alpine`) and optional `ConfigureBuilder(Action<PostgreSqlBuilder>)` hook for custom env vars, networks, volumes, and similar. |
| `PostgresContainerFixtureBase` | Abstract xUnit `IAsyncLifetime` fixture: starts a shared container, invokes `OnContainerStartedAsync(connectionString, ct)`, exposes `ConnectionString`, and calls `OnContainerDisposingAsync(ct)` before tearing the container down. Cancellation is sourced from `TestContext.Current.CancellationToken`. |
| `RabbitMqTestContainer` | `IAsyncDisposable` wrapper around `RabbitMqContainer`. After `StartAsync`, exposes `Host`, `Port` (mapped AMQP), `AdminUrl` (mapped management HTTP API), `Username`/`Password`, and the AMQP `ConnectionString`. |
| `RabbitMqContainerOptions` | `Image` (defaults to `rabbitmq:4-management-alpine`, which must be a management-enabled image for `AdminUrl` to work) and optional `ConfigureBuilder(Action<RabbitMqBuilder>)` hook. |
| `RabbitMqContainerFixtureBase` | Abstract xUnit `IAsyncLifetime` fixture mirroring the Postgres one: starts a shared broker, invokes `OnContainerStartedAsync(container, ct)`, exposes the endpoint properties, and calls `OnContainerDisposingAsync(ct)` before teardown. |

## Sharing one container across an assembly

```csharp
[assembly: AssemblyFixture(typeof(MyFeatureFixture))]
```

`PostgresContainerFixtureBase` is safe to reuse as an assembly fixture because `OnContainerStartedAsync` runs exactly once and `OnContainerDisposingAsync` runs once at teardown.

## RabbitMQ fixture

`RabbitMqContainerFixtureBase` follows the same pattern. `Lyo.MessageQueue.RabbitMq.Tests` uses it to run
`RabbitMqService` against a real broker:

```csharp
public sealed class RabbitMqBrokerFixture : RabbitMqContainerFixtureBase
{
    public RabbitMqService CreateService(Action<RabbitMqOptions>? configure = null)
    {
        var options = new RabbitMqOptions { Host = Host, Port = Port, AdminUrl = AdminUrl, Username = Username, Password = Password };
        configure?.Invoke(options);
        var factory = new ConnectionFactory { HostName = options.Host, Port = options.Port, UserName = options.Username, Password = options.Password };
        return new(options, factory);
    }
}

[assembly: AssemblyFixture(typeof(RabbitMqBrokerFixture))]
```

The management HTTP API (`AdminUrl`) is exposed so peek and queue-statistics operations work in tests.

## See also

- `Lyo.Testing`. Shared assertion, fake, and time-control helpers used by tests across the solution.
- Testcontainers for .NET documentation. Builder configuration (volumes, networks, wait strategies).

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common` (direct, lyo)
- `Testcontainers.PostgreSql` `4.13.0` (direct, third-party)
- `Testcontainers.RabbitMq` `4.13.0` (direct, third-party)
- `xunit.v3.extensibility.core` `3.2.2` (direct, third-party)
- `Lyo.Exceptions` (transitive, lyo)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)