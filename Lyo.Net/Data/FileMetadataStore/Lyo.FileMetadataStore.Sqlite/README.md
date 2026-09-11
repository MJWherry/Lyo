# Lyo.FileMetadataStore.Sqlite

EF Core SQLite IFileMetadataStore. Same store and adjunct services as [Lyo.FileMetadataStore.Postgres](../Lyo.FileMetadataStore.Postgres/README.md), for embedded, offline-first, and local-dev hosts.

## Examples

### Add the SQLite store

```csharp
using Lyo.FileMetadataStore.Sqlite;

// Simple
services.AddSqliteFileMetadataStore("Data Source=./filestore.db");

// With auto-migrations at host startup
services.AddSqliteFileMetadataStoreDbContextFactoryFromConfiguration(configuration);
services.AddSqliteFileMetadataStore();
services.AddSqliteFileAuditSink();
services.AddSqliteFileDownloadAccessService();

// Keyed (multi-store hosts)
services.AddSqliteFileMetadataStoreKeyed("sqlite-metadata")
    .ConfigureSqliteFileStore(options => {
        options.ConnectionString = "Data Source=./filestore.db";
        options.EnableAutoMigrations = true;
    })
    .Build();
```

### appsettings.json

```json
{
  "SqliteFileMetadataStore": {
    "ConnectionString": "Data Source=./filestore.db",
    "EnableAutoMigrations": true
  }
}
```

### Attach to keyed file storage

```csharp
services.AddS3FileStorageServiceKeyed("my-files")
    .UseFileMetadataStore("sqlite-metadata")
    .Build(configuration);
```

## Types

| Component | Role |
| --------------------------------- | -------------------------------------------------- |
| SqliteFileMetadataStore | IFileMetadataStore + IHealth |
| SqliteFileAuditSink | IFileAuditEventHandler. Append-only audit rows. |
| SqliteMultipartUploadSessionStore | IMultipartUploadSessionStore |
| SqliteFileDownloadAccessService | Time-limited download access tokens |
| SqliteFileMetadataStoreDbContext | EF Core context (5 tables, same shape as Postgres) |

Tables: `file_metadata`, `file_data`, `file_audit_events`, `multipart_upload_session`, `file_download_access_links`.

## How to register

`appsettings.json`: Point blob storage at the keyed metadata store the same way as Postgres:

## How migrations run

Migrations ship in this package. Turn on EnableAutoMigrations (via [`Lyo.Sqlite`](../../Sqlite/Lyo.Sqlite/README.md)) or run `dotnet ef database update` using SqliteFileMetadataStoreDbContextFactory. Design-time connection string: FILEMETADATASTORE_CONNECTION_STRING or FILESTORE_CONNECTION_STRING (defaults to `Data Source=./filestore-design.db`). `DropStagedFileUpload` removes the former `staged_file_upload` table. `AddFileMetadataJson` adds opaque `metadata_json` on `file_metadata` and `multipart_upload_session`.

## Writer limits

SQLite is single-writer. Fine for embedded clients, local tools, and low-concurrency dev hosts. High-throughput multi-instance ingestion should use Postgres. Enable WAL at the connection level if the host opens many concurrent readers (`PRAGMA journal_mode=WAL`).

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Configuration` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.FileMetadataStore` (direct, lyo)
- `Lyo.FileStorage` (direct, lyo)
- `Lyo.Health` (direct, lyo)
- `Lyo.Lock` (direct, lyo)
- `Lyo.Sqlite` (direct, lyo)
- `Microsoft.EntityFrameworkCore` `10.0.5` (direct, microsoft)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` (direct, microsoft)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.Compression` (transitive, lyo)
- `Lyo.Encryption` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.IO.Temp` (transitive, lyo)
- `Lyo.KeyStore` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Streams` (transitive, lyo)
- `BouncyCastle.Cryptography` `2.6.2` (transitive, third-party, netstandard2.0)
- `EasyCompressor` `2.1.0` (transitive, third-party)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.EntityFrameworkCore.Sqlite` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.DataAnnotations` `10.0.5` (transitive, microsoft)
- `System.Buffers` `4.6.1` (transitive, microsoft, netstandard2.0)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft, netstandard2.0)