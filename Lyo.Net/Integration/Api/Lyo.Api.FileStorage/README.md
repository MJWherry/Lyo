# Lyo.Api.FileStorage

HTTP endpoints for Lyo file storage. Hosts map `BuildFileStorageApi` after registering a keyed `IFileStorageService` (plus multipart, staged upload, and archive). FileMetadata Query/QueryProject is included. `GET key-ids` lists encryption key identifiers; keystore CRUD is not part of this API.

## Examples

### Host setup

```csharp
app.BuildFileStorageApi(); // defaults: Workbench/FileStorage, keyed gateway-filestorage
// or:
app.BuildFileStorageApi(new FileStorageApiOptions {
    Route = "Workbench/FileStorage",
    ServiceKey = "gateway-filestorage"
});
```

## What this maps

Workbench group at `FileStorageApiOptions.Route`: health, save/save-stream, copy/move/rename, metadata, download (always streams through the host, honors `?inline=true`; decrypt/decompress on the API), archive, access-links, presigned-read (direct-to-bucket URL), DEK migrate/rotate, `GET key-ids` (encryption key identifiers only, no raw material), `diagnostics/storage-keys`, `stage/*`, `multipart/*`, `direct-upload/*`. Optional `POST DirectUploadPath` (default `upload/file`). Read-only FileMetadata QueryProject at `FileMetadataRoute`. There are no `keys/*` CRUD routes. Wire DTOs live in `Lyo.Api.FileStorage.Models`.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Api` (direct, lyo)
- `Lyo.Api.FileStorage.Models` (direct, lyo)
- `Lyo.Cache` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.FileMetadataStore.Postgres` (direct, lyo)
- `Lyo.FileStorage` (direct, lyo)
- `Lyo.KeyStore` (direct, lyo)
- `Lyo.Api.Models` (transitive, lyo)
- `Lyo.Common` (transitive, lyo)
- `Lyo.Compression` (transitive, lyo)
- `Lyo.ContentThreatScan` (transitive, lyo)
- `Lyo.DateAndTime` (transitive, lyo)
- `Lyo.Diagnostic` (transitive, lyo)
- `Lyo.Diagnostic.AspNetCore` (transitive, lyo)
- `Lyo.Diff` (transitive, lyo)
- `Lyo.Encryption` (transitive, lyo)
- `Lyo.FileMetadataStore` (transitive, lyo)
- `Lyo.Formatter` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.Health` (transitive, lyo)
- `Lyo.IO.Temp` (transitive, lyo)
- `Lyo.Lock` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.PackageMetadata` (transitive, lyo)
- `Lyo.Postgres` (transitive, lyo)
- `Lyo.Query` (transitive, lyo)
- `Lyo.Query.Models` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Streams` (transitive, lyo)
- `Lyo.Validation` (transitive, lyo)
- `BouncyCastle.Cryptography` `2.6.2` (transitive, third-party, netstandard2.0)
- `EasyCompressor` `2.1.0` (transitive, third-party)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` (transitive, third-party)
- `Microsoft.AspNetCore.Authorization` `10.0.5` (transitive, microsoft)
- `Microsoft.AspNetCore.Http.Abstractions` `2.*` (transitive, microsoft)
- `Microsoft.AspNetCore.OpenApi` `10.0.5` (transitive, microsoft)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.EntityFrameworkCore` `10.0.5` (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Analyzers` `10.0.5` (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Caching.Memory` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.DataAnnotations` `10.0.5` (transitive, microsoft)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` (transitive, third-party)
- `SmartFormat.NET` `3.6.1` (transitive, third-party)
- `System.Buffers` `4.6.1` (transitive, microsoft, netstandard2.0)
- `System.ComponentModel.Annotations` `5.0.0` (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft, netstandard2.0)