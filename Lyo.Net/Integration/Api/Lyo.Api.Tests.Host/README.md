# Lyo.Api.Tests.Host

Reference ASP.NET Core minimal-API host used by `Lyo.Api.Tests` and other integration tests as a `WebApplicationFactory<Program>` target. It wires a realistic combination of
`Lyo.Api` services so tests exercise the same registration and middleware paths as production hosts.

## What it ships

- **`Program`** ([`Program.cs`](Program.cs)) — top-level statements that build the host. The empty `Lyo.Api.Tests.Host.Program` class at the bottom is the entry-point type
  required by `WebApplicationFactory<Program>`.
- **`MapsterLyoMapper`** ([`MapsterLyoMapper.cs`](MapsterLyoMapper.cs)) — the `ILyoMapper` implementation registered for tests; thin Mapster adapter so request/response DTOs map
  end-to-end.

## Registered services

The host configures (see [`Program.cs`](Program.cs)):

- **CSV / XLSX / Formatter** (`AddCsvService`, `AddXlsxService`, `AddFormatterService`) — required by export and computed fields.
- **Response compression** (Brotli + Gzip, `Fastest` level) and **request decompression**.
- **JSON** via `LyoJsonSerializerOptions.ApplyTo` so `ICachePayloadSerializer` (registered by `AddLyoQueryServices`) matches the wire format.
- **Local cache** (`AddLocalCache`) and **`AddLyoQueryServices`**.
- **Job persistence** through `AddPostgresJobManagementFromConfiguration` (PostgreSQL `Lyo.Job` schema; configured via `appsettings.json`).
- **Export service** for `JobContext`.
- **Mapster** (`TypeAdapterConfig` + `IMapper` + `ILyoMapper` ↔ `MapsterLyoMapper`).
- **CORS** with an allow-everything default policy (test convenience).

## Endpoint surfaces

Two parallel surfaces are mapped over the same `JobContext`:

- **Typed builder** at `/api/Job/Definition` — uses `CreateBuilder<JobContext, JobDefinition, JobDefinitionReq, JobDefinitionRes, Guid>` with `AllowAnonymous`,
  `WithMetadata(IncludeEntityMetadata = true)`, and lifecycle hooks (`WithGet`, `WithCreate`, `WithCreateBulk`, `WithUpdate`, `WithUpdateBulk`, `WithPatch`, `WithPatchBulk`,
  `WithUpsert`, `WithUpsertBulk`, `WithDelete`, `WithDeleteBulk`). Each hook appends a marker (e.g. `[afterCreate]`) to `Description` so tests can verify hook execution order.
- **Dynamic builder** at `api/Job` — `MapDynamicCrudEndpoints<JobContext>` with `IncludeOnly<JobDefinition>` and `BeforeCreate` ensuring a `Guid.NewGuid()` PK when callers omit
  `Id`. Features: `ApiFeatureFlag.All | UpsertInheritCreate | UpsertInheritUpdate | PatchInheritsUpdate`.

## Using it from tests

```csharp
public sealed class JobApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public JobApiTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Query_returns_empty_list()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsync("/api/Job/Definition/Query", JsonContent.Create(new { Start = 0, Amount = 10 }));
        response.EnsureSuccessStatusCode();
    }
}
```

Pair with `Lyo.Api.Client` for typed assertions and reuse `LyoJsonSerializerOptions.Create()` so cached payloads parse with the same contract.

## Related projects

- [`Lyo.Api`](../Lyo.Api/README.md)
- [`Lyo.Api.Models`](../Lyo.Api.Models/README.md)
- [`Lyo.Cache`](../../../Core/Cache/Lyo.Cache/README.md)
- [`Lyo.Csv`](../../../Data/Csv/Lyo.Csv/README.md)
- [`Lyo.Formatter`](../../../Data/Formatter/Lyo.Formatter/README.md)
- [`Lyo.Job.Models`](../../Job/Lyo.Job.Models/README.md)
- [`Lyo.Job.Postgres`](../../Job/Lyo.Job.Postgres/README.md)
- [`Lyo.Xlsx`](../../../Data/Xlsx/Lyo.Xlsx/README.md)
