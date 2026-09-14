# Lyo.Seed

A named `SeedContributor` is run against an `ISeedTransport` by `Lyo.Seed`. The graph only wants items (`Func<int, T>` / `ForEach` factories). Bogus and other fakers remain in the host. This package ships two transports: `EfSeedTransport<TContext>` (`AddRange` + `SaveChangesAsync`) and `ApiSeedTransport` (chunked `POST {route}/Bulk`, skip via `POST {route}/QueryConcrete`).

## Features

- **No generator dependency.** Factories that return `T` go to `SeedGraph.Entity` and `ForEach`. Contributors can use Bogus, hand-built objects, or anything else.
- **EF transport.** Each batch is persisted by `EfSeedTransport<TContext>` with `SaveChangesAsync`. `After` callbacks may call `SeedSession.PersistAsync` or use `Context` for schema-specific deletes.
- **API transport.** `ApiSeedTransport` maps request types onto Lyo.Api routes, chunks at `MaxBulkAmount` (default 2000), and fails the run when `CreateBulkResult.FailedCount > 0`.
- **Conflict modes.** `Append` (default), optional `SkipIfNotEmpty`, and `Replace` (`OnClear` is required).

## Examples

### Contributor on EF

```csharp
public sealed class ParentContributor : SeedContributor
{
    public override string Name => "parents";
    public override SeedTransportKind SupportedTransports => SeedTransportKind.Ef;

    protected override void Configure(SeedGraph graph, SeedOptions options)
        => graph.Entity(options.Count, i => new ParentRow { Id = Guid.NewGuid(), Name = $"p-{i}" })
            .ForEach(_ => 2, (parent, i) => new ChildRow { Id = Guid.NewGuid(), ParentId = parent.Id, Label = $"{parent.Name}-{i}" });
}

await using var db = new MyDbContext(options);
var result = await new SeedRunner().SeedAsync(new ParentContributor(), new EfSeedTransport<MyDbContext>(db));
```

### Transport over the API

```csharp
var catalog = new SeedApiCatalog();
catalog.Map<PersonReq, PersonRes>("Person");
var transport = new ApiSeedTransport(apiClient, catalog);
await runner.SeedAsync(peopleApiContributor, transport, new() { Count = 50 });
```

### Container registration

```csharp
services.AddLyoSeed();
services.AddSeedContributor<ParentContributor>();
services.AddEfSeedTransport<MyDbContext>();
services.AddApiSeedTransport(api => api.Map<PersonReq, PersonRes>("Person"));
```

## How transports are built

Build `EfSeedTransport<TContext>` on a live `DbContext`, or `ApiSeedTransport` on `IApiClient` plus a `SeedApiCatalog`. `ISeedRunner` rejects a transport whose `Kind` is missing from `SeedContributor.SupportedTransports`.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Api.Client` (direct, lyo)
- `Lyo.Api.Models` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Http.Client` (direct, lyo)
- `Lyo.Query.Models` (direct, lyo)
- `Microsoft.EntityFrameworkCore` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Json` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.DateAndTime` (transitive, lyo)
- `Lyo.Diagnostic` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.PackageMetadata` (transitive, lyo)
- `Lyo.Parameters` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `AngleSharp` `1.5.0` (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Http` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.RateLimiting` `10.0.5` (transitive, microsoft)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft)