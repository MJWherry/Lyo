# Lyo.Api.Tests.Host

Reference ASP.NET Core minimal-API host used by `Lyo.Api.Tests` and other integration tests as a `WebApplicationFactory<Program>` target. It wires a realistic combination of `Lyo.Api` services so tests exercise the same registration and middleware paths as production hosts.

## Examples

### Using it from tests

```csharp
public sealed class JobApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public JobApiTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Query_returns_empty_list()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsync("/api/Job/Definition/QueryConcrete", JsonContent.Create(new { Start = 0, Amount = 10 }));
        response.EnsureSuccessStatusCode();
    }
}
```

## What it ships

- `Program` ([`Program.cs`](Program.cs)). Top-level statements that build the host. The empty `Lyo.Api.Tests.Host.Program` class at the bottom is the entry-point type required by `WebApplicationFactory<Program>`.
- `MapsterLyoMapper` ([`MapsterLyoMapper.cs`](MapsterLyoMapper.cs)). The `ILyoMapper` implementation registered for tests; thin Mapster adapter so request/response DTOs map end-to-end.

## Registered services

- **CSV / XLSX / Formatter** (`AddCsvService`, `AddXlsxService`, `AddFormatterService`). Required by export and computed fields.
- **Response compression** (Brotli + Gzip, `Fastest` level) and **request decompression**.
- **JSON** via `LyoJsonSerializerOptions.ApplyTo` so `ICachePayloadSerializer` (registered by `AddLyoQueryServices`) matches the wire format.
- **Local cache** (`AddLocalCache`) and `AddLyoQueryServices`.
- **Job persistence** through `AddPostgresJobManagementFromConfiguration` (PostgreSQL `Lyo.Job` schema; configured via `appsettings.json`).
- **Export service** for `JobContext`.
- **Mapster** (`TypeAdapterConfig` + `IMapper` + `ILyoMapper` ↔ `MapsterLyoMapper`).
- **CORS** with an allow-everything default policy (test convenience).

## Endpoints

- **Typed builder** at `/api/Job/Definition`. Uses `CreateBuilder<JobContext, JobDefinition, JobDefinitionReq, JobDefinitionRes, Guid>` with `AllowAnonymous`, `WithMetadata(IncludeEntityMetadata = true)`, and lifecycle hooks (`WithGet`, `WithCreate`, `WithCreateBulk`, `WithUpdate`, `WithUpdateBulk`, `WithPatch`, `WithPatchBulk`, `WithUpsert`, `WithUpsertBulk`, `WithDelete`, `WithDeleteBulk`). Each hook appends a marker (e.g. `[afterCreate]`) to `Description` so tests can verify hook execution order.
- **Dynamic builder** at `api/Job`. `MapDynamicCrudEndpoints<JobContext>` with `IncludeOnly<JobDefinition>` and `BeforeCreate` setting a `Guid.NewGuid()` PK when callers omit `Id`. Features: `ApiFeatureSet.DefaultCrud + ExportApiFeature.Instance` on dynamic defaults.

## Using it from tests

Pair with `Lyo.Api.Client` for typed assertions and reuse `LyoJsonSerializerOptions.Create()` so cached payloads parse with the same contract.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Api` (direct, lyo)
- `Lyo.Api.Export` (direct, lyo)
- `Lyo.Api.Export.Csv` (direct, lyo)
- `Lyo.Api.Export.Xlsx` (direct, lyo)
- `Lyo.Cache` (direct, lyo)
- `Lyo.Csv` (direct, lyo)
- `Lyo.Formatter` (direct, lyo)
- `Lyo.Job.Models` (direct, lyo)
- `Lyo.Job.Postgres` (direct, lyo)
- `Lyo.Xlsx` (direct, lyo)
- `Mapster` `10.0.10` (direct, third-party)
- `Mapster.DependencyInjection` `10.0.10` (direct, third-party)
- `Lyo.Api.Models` (transitive, lyo)
- `Lyo.Audit` (transitive, lyo)
- `Lyo.Common` (transitive, lyo)
- `Lyo.Compression` (transitive, lyo)
- `Lyo.Csv.Models` (transitive, lyo)
- `Lyo.DataTable.Models` (transitive, lyo)
- `Lyo.DateAndTime` (transitive, lyo)
- `Lyo.Diagnostic` (transitive, lyo)
- `Lyo.Diagnostic.AspNetCore` (transitive, lyo)
- `Lyo.Diff` (transitive, lyo)
- `Lyo.Encryption` (transitive, lyo)
- `Lyo.EntityReference.Models` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.Health` (transitive, lyo)
- `Lyo.KeyStore` (transitive, lyo)
- `Lyo.MessageQueue` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.PackageMetadata` (transitive, lyo)
- `Lyo.Postgres` (transitive, lyo)
- `Lyo.Query` (transitive, lyo)
- `Lyo.Query.Models` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Schedule.Models` (transitive, lyo)
- `Lyo.Scheduler` (transitive, lyo)
- `Lyo.Streams` (transitive, lyo)
- `Lyo.Validation` (transitive, lyo)
- `Lyo.Xlsx.Models` (transitive, lyo)
- `BouncyCastle.Cryptography` `2.6.2` (transitive, third-party, netstandard2.0)
- `ClosedXML` `0.105.0` (transitive, third-party)
- `DocumentFormat.OpenXml` `3.1.1` (transitive, third-party)
- `EasyCompressor` `2.1.0` (transitive, third-party)
- `ExcelDataReader` `3.9.0` (transitive, third-party)
- `ExcelDataReader.DataSet` `3.9.0` (transitive, third-party)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` (transitive, third-party)
- `Microsoft.AspNetCore.OpenApi` `10.0.5` (transitive, microsoft)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.EntityFrameworkCore` `10.0.5` (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Analyzers` `10.0.5` (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Caching.Memory` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Configuration` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` (transitive, third-party)
- `SmartFormat.NET` `3.6.1` (transitive, third-party)
- `System.Buffers` `4.6.1` (transitive, microsoft, netstandard2.0)
- `System.ComponentModel.Annotations` `5.0.0` (transitive, microsoft)
- `System.Diagnostics.DiagnosticSource` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Encoding.CodePages` `10.0.5` (transitive, microsoft)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft, netstandard2.0)