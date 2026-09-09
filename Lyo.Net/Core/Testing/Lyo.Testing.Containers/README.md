# Lyo.Testing.Containers

Testcontainers fixtures for xUnit v3. The shipped helpers start and dispose Redis, RabbitMQ, and PostgreSQL containers, and `ContainerTestHost` covers backends with no Testcontainers module of their own.

Internal-only: `IsPackable` is `false` and `xunit.v3.extensibility.core` is a project-level dependency. Reference this project from test projects. Do not pack it.

## Features

- **Per-backend containers and fixtures.** `RabbitMqTestContainer` / `RabbitMqContainerFixtureBase`, `PostgresTestContainer` / `PostgresContainerFixtureBase`, and `RedisTestContainer` / `RedisContainerFixtureBase`, each with an `*ContainerOptions` class exposing `Image` and a `ConfigureBuilder` hook.
- **`PostgresServiceFixtureBase<TContext>`.** The shape almost every `Lyo.*.Postgres.Tests` fixture needs: starts a Postgres container, builds a `ServiceProvider` from the abstract `ConfigureServices(services, connectionString)`, migrates `TContext`, then exposes `ContextFactory`, `ServiceProvider`, and `CreateScope()`. Override `OnMigratedAsync` to seed and `MinimumLogLevel` to change log verbosity.
- **`ContainerTestHost.TryStartAsync`.** Starts a raw `ContainerBuilder` container and returns `null` (rather than throwing) when Docker is unavailable, so integration suites skip cleanly instead of failing. Takes an optional `readyDelay` for servers whose wait strategy fires before they accept connections.

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

### Customize the container

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
| ------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `PostgresTestContainer` | `IAsyncDisposable` wrapper around `PostgreSqlContainer`. Call `StartAsync(CancellationToken)` once, then read `ConnectionString`. Throws `InvalidOperationException` when `ConnectionString` is read before `StartAsync`. |
| `PostgresContainerOptions` | `Image` (defaults to `postgres:16-alpine`) and optional `ConfigureBuilder(Action<PostgreSqlBuilder>)` hook for custom env vars, networks, volumes, and similar. |
| `PostgresContainerFixtureBase` | Abstract xUnit `IAsyncLifetime` fixture: starts a shared container, invokes `OnContainerStartedAsync(connectionString, ct)`, exposes `ConnectionString`, and calls `OnContainerDisposingAsync(ct)` before tearing the container down. Cancellation is sourced from `TestContext.Current.CancellationToken`. |
| `RabbitMqTestContainer` | `IAsyncDisposable` wrapper around `RabbitMqContainer`. After `StartAsync`, exposes `Host`, `Port` (mapped AMQP), `AdminUrl` (mapped management HTTP API), `Username`/`Password`, and the AMQP `ConnectionString`. |
| `RabbitMqContainerOptions` | `Image` (defaults to `rabbitmq:4-management-alpine`, which must be a management-enabled image for `AdminUrl` to work) and optional `ConfigureBuilder(Action<RabbitMqBuilder>)` hook. |
| `RabbitMqContainerFixtureBase` | Abstract xUnit `IAsyncLifetime` fixture mirroring the Postgres one: starts a shared broker, invokes `OnContainerStartedAsync(container, ct)`, exposes the endpoint properties, and calls `OnContainerDisposingAsync(ct)` before teardown. |
| `RedisTestContainer` | `IAsyncDisposable` wrapper around `RedisContainer`. Call `StartAsync(CancellationToken)`, then read `ConnectionString`. |
| `RedisContainerOptions` | `Image` (defaults to `redis:7-alpine`) and optional `ConfigureBuilder(Action<RedisBuilder>)` hook. |
| `RedisContainerFixtureBase` | Abstract xUnit `IAsyncLifetime` fixture exposing `ConnectionString`, with the same `OnContainerStartedAsync` / `OnContainerDisposingAsync` hooks as the other fixtures. |
| `PostgresServiceFixtureBase<TContext>` | Postgres fixture that also owns a DI container. Implement `ConfigureServices(services, connectionString)`; the base builds the provider, runs `MigrateAsync`, calls `OnMigratedAsync` for seeding, and exposes `ServiceProvider`, `ContextFactory`, and `CreateScope()`. `OnContainerStartedAsync` is sealed. |
| `ContainerTestHost.TryStartAsync(image, configure, ct, readyDelay)` | Raw `ContainerBuilder` start for backends without a Testcontainers module. Returns `null` and disposes the partial container when the start fails, so callers can skip instead of fail. |

## Sharing one container across an assembly

```csharp
[assembly: AssemblyFixture(typeof(MyFeatureFixture))]
```

Because `OnContainerStartedAsync` runs exactly once and `OnContainerDisposingAsync` runs once at teardown, `PostgresContainerFixtureBase` is safe as an assembly fixture.

## RabbitMQ fixture

The same fixture pattern is `RabbitMqContainerFixtureBase`. `Lyo.MessageQueue.RabbitMq.Tests` drives
`RabbitMqService` against a live broker with it:

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

Peek and queue-statistics tests need the management HTTP API (`AdminUrl`), which the fixture exposes.

## See also

- `Lyo.Testing`. Shared assertion, fake, and time-control helpers used by tests across the solution.
- Testcontainers for .NET documentation. Builder configuration (volumes, networks, wait strategies).

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Metadata` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Microsoft.EntityFrameworkCore` `10.0.5` (direct, microsoft)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Console` `10.0.5` (direct, microsoft)
- `Testcontainers` `4.13.0` (direct, third-party)
- `Testcontainers.PostgreSql` `4.13.0` (direct, third-party)
- `Testcontainers.RabbitMq` `4.13.0` (direct, third-party)
- `Testcontainers.Redis` `4.13.0` (direct, third-party)
- `xunit.v3.extensibility.core` `3.2.2` (direct, third-party)
- `Lyo.Common.Core` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)