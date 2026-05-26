# Lyo.Testing.Containers

xUnit v3 fixture helpers around **Testcontainers** so integration tests can spin up real backing services without hand-rolling lifecycle plumbing. The shipped helpers cover **PostgreSQL** — other backends can be added by following the same shape.

> **Internal-only:** `IsPackable` is `false` and `xunit.v3.extensibility.core` is a project-level dependency. Reference this project from test projects; do not pack it.

## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `Testcontainers.PostgreSql` | `4.10.0` | Spins up the Docker container. |
| `xunit.v3.extensibility.core` | `3.2.2` | Provides `IAsyncLifetime` + `TestContext.Current` used by the fixture base. |

Docker (or a compatible runtime) must be available on the host running the tests.

## Public surface

| Type | Role |
|------|------|
| **`PostgresTestContainer`** | `IAsyncDisposable` wrapper around `PostgreSqlContainer`. Call `StartAsync(CancellationToken)` once, then read `ConnectionString`. Throws `InvalidOperationException` when `ConnectionString` is read before `StartAsync`. |
| **`PostgresContainerOptions`** | `Image` (defaults to `postgres:16-alpine`) and optional `ConfigureBuilder(Action<PostgreSqlBuilder>)` hook for custom env vars, networks, volumes, etc. |
| **`PostgresContainerFixtureBase`** | Abstract xUnit `IAsyncLifetime` fixture: starts a shared container, invokes `OnContainerStartedAsync(connectionString, ct)`, exposes `ConnectionString`, and calls `OnContainerDisposingAsync(ct)` before tearing the container down. Cancellation is sourced from `TestContext.Current.CancellationToken`. |

## Quick start — xUnit v3 class fixture

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

## Sharing one container across an assembly

```csharp
[assembly: AssemblyFixture(typeof(MyFeatureFixture))]
```

`PostgresContainerFixtureBase` is safe to reuse as an assembly fixture because `OnContainerStartedAsync` runs exactly once and `OnContainerDisposingAsync` runs once at teardown.

## Customizing the container

```csharp
public sealed class TrgmFixture() : PostgresContainerFixtureBase(new PostgresContainerOptions {
    Image = "postgres:17-alpine",
    ConfigureBuilder = builder => builder
        .WithEnvironment("POSTGRES_DB", "lyo_tests")
        .WithCommand("postgres", "-c", "shared_preload_libraries=pg_trgm"),
});
```

## Standalone use (no fixture)

For ad-hoc scripts or non-xUnit harnesses, `PostgresTestContainer` is usable directly:

```csharp
await using var container = new PostgresTestContainer();
await container.StartAsync(ct);
var connectionString = container.ConnectionString;
```

## See also

- **`Lyo.Testing`** — shared assertion / fake / time-control helpers used by tests across the solution.
- **Testcontainers for .NET** documentation — for advanced builder configuration (volumes, networks, wait strategies).
