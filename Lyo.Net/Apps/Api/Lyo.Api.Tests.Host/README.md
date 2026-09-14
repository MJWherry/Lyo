# Lyo.Api.Tests.Host

Reference ASP.NET Core minimal-API host that `Lyo.Api.Tests` and other integration tests target via `WebApplicationFactory<Program>`. It wires a realistic mix of `Lyo.Api` services so tests walk the same registration and middleware paths production hosts use.

## Examples

### Calling it from tests

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

## What ships

- `Program` ([`Program.cs`](Program.cs)). Top-level statements that build the host. At the bottom, the empty `Lyo.Api.Tests.Host.Program` class is the entry-point type `WebApplicationFactory<Program>` requires.

## Services registered

- **CSV / XLSX / Formatter** (`AddXlsxService`, `AddCsvService`, `AddFormatterService`). Export and computed fields need these.
- **Response compression** (Gzip + Brotli, `Fastest` level) and **request decompression**.
- **JSON** via `LyoJsonSerializerOptions.ApplyTo` so `ICachePayloadSerializer` (from `AddLyoQueryServices`) matches the wire format.
- **Local cache** (`AddLocalCache`) plus `AddLyoQueryServices`.
- **Job persistence** through `AddPostgresJobManagementFromConfiguration` (PostgreSQL `Lyo.Job` schema; configured via `appsettings.json`).
- **Export service** for `JobContext`.
- **Mapping** is registered by `AddPostgresJobManagementFromConfiguration` (`ILyoMapper` the Job package needs), so this host has no mapper of its own. Hosts that map their own DTOs supply an `ILyoMapper` themselves — Lyo ships no AutoMapper or Mapster adapter.
- **CORS** with a default policy that allows everything (test convenience).

## Endpoints

- **Typed builder** at `/api/Job/Definition`. Uses `CreateBuilder<JobContext, JobDefinition, JobDefinitionReq, JobDefinitionRes, Guid>` with `AllowAnonymous`, `WithMetadata(IncludeEntityMetadata = true)`, and lifecycle hooks (`WithGet`, `WithCreate`, `WithCreateBulk`, `WithUpdate`, `WithUpdateBulk`, `WithPatch`, `WithPatchBulk`, `WithUpsert`, `WithUpsertBulk`, `WithDelete`, `WithDeleteBulk`). Each hook appends a marker (e.g. `[afterCreate]`) to `Description` so tests can verify hook execution order.
- **Dynamic builder** at `api/Job`. `MapDynamicCrudEndpoints<JobContext>` with `IncludeOnly<JobDefinition>` and `BeforeCreate` setting a `Guid.NewGuid()` PK when callers omit `Id`. Features: `ApiFeatureSet.DefaultCrud + ExportApiFeature.Instance` on dynamic defaults.

## Calling it from tests

Use `Lyo.Api.Client` for typed assertions, and parse cached payloads with `LyoJsonSerializerOptions.Create()` so the contract matches the server.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Api` (direct, lyo)
- `Lyo.Api.Export` (direct, lyo)
- `Lyo.Api.Export.Csv` (direct, lyo)
- `Lyo.Api.Export.Xlsx` (direct, lyo)
- `Lyo.Cache` (direct, lyo)
- `Lyo.Common.Json` (direct, lyo)
- `Lyo.Csv` (direct, lyo)
- `Lyo.Formatter` (direct, lyo)
- `Lyo.Job.Models` (direct, lyo)
- `Lyo.Job.Postgres` (direct, lyo)
- `Lyo.Xlsx` (direct, lyo)
- `Lyo.Api.Models` (transitive, lyo)
- `Lyo.Audit` (transitive, lyo)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
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
- `Lyo.Parameters` (transitive, lyo)
- `Lyo.Postgres` (transitive, lyo)
- `Lyo.Query` (transitive, lyo)
- `Lyo.Query.Evaluation` (transitive, lyo)
- `Lyo.Query.Models` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Schedule.Models` (transitive, lyo)
- `Lyo.Scheduler` (transitive, lyo)
- `Lyo.Streams` (transitive, lyo)
- `Lyo.Validation` (transitive, lyo)
- `Lyo.Validation.Models` (transitive, lyo)
- `Lyo.Xlsx.Models` (transitive, lyo)
- `BouncyCastle.Cryptography` `2.6.2` (transitive, third-party, netstandard2.0)
- `ClosedXML` `0.105.0` (transitive, third-party)
- `DocumentFormat.OpenXml` `3.1.1` (transitive, third-party)
- `DynamicExpresso.Core` `2.19.3` (transitive, third-party)
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