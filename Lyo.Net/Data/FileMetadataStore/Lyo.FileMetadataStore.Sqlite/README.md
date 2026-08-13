# Lyo.FileMetadataStore.Sqlite

SQLite implementation of **`IFileMetadataStore`** using Entity Framework Core. Functional parity with [ `Lyo.FileMetadataStore.Postgres`](../Lyo.FileMetadataStore.Postgres/README.md) for embedded, offline-first, and local-dev scenarios.

## Examples

### Register services

```csharp
using Lyo.FileMetadataStore.Sqlite;

// Simple
services.AddSqliteFileMetadataStore("Data Source=./filestore.db");

// With auto-migrations at host startup
services.AddSqliteFileMetadataStoreDbContextFactoryFromConfiguration(configuration);
services.AddSqliteFileMetadataStore();
services.AddSqliteFileAuditSink();
services.AddSqliteFileDownloadAccessService();
services.AddSqliteStagedFileUploadStore(); // optional — builder adds when missing

// Keyed (multi-store hosts)
services.AddSqliteFileMetadataStoreKeyed("sqlite-metadata")
    .ConfigureSqliteFileStore(options => {
        options.ConnectionString = "Data Source=./filestore.db";
        options.EnableAutoMigrations = true;
    })
    .Build();
```

### Register services (2)

```json
{
  "SqliteFileMetadataStore": {
    "ConnectionString": "Data Source=./filestore.db",
    "EnableAutoMigrations": true
  }
}
```

### Register services (3)

```csharp
services.AddS3FileStorageServiceKeyed("my-files")
    .UseFileMetadataStore("sqlite-metadata")
    .Build(configuration);
```

## What is included

| Component | Role |
| --------------------------------------- | -------------------------------------------------- |
| **`SqliteFileMetadataStore`** | `IFileMetadataStore` + `IHealth` |
| **`SqliteFileAuditSink`** | `IFileAuditEventHandler` — append-only audit rows |
| **`SqliteMultipartUploadSessionStore`** | `IMultipartUploadSessionStore` |
| **`SqliteStagedFileUploadStore`** | `IStagedFileUploadStore` |
| **`SqliteFileDownloadAccessService`** | Time-boxed download access tokens |
| **`SqliteFileMetadataStoreDbContext`** | EF Core context (6 tables, same shape as Postgres) |

Schema tables: `file_metadata`, `file_data`, `file_audit_events`, `multipart_upload_session`, `staged_file_upload`, `file_download_access_links`.

## Registration

`appsettings.json`: Wire blob storage to the keyed metadata store the same way as Postgres:

## Migrations

Migrations ship in this package. Enable **`EnableAutoMigrations`** (via [`Lyo.Sqlite`](../../Sqlite/Lyo.Sqlite/README.md)) or run `dotnet ef database update` using `SqliteFileMetadataStoreDbContextFactory`. Design-time connection string: `FILEMETADATASTORE_CONNECTION_STRING` or `FILESTORE_CONNECTION_STRING` (defaults to `Data Source=./filestore-design.db`).

## Concurrency

SQLite is single-writer. Suitable for embedded clients, local tools, and low-concurrency dev hosts. For high-throughput multi-instance ingestion use Postgres. Enable WAL at the connection level if your host opens many concurrent readers (`PRAGMA journal_mode=WAL`).

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` — (direct, lyo)
- `Lyo.FileMetadataStore` — (direct, lyo)
- `Lyo.FileStorage` — (direct, lyo)
- `Lyo.Health` — (direct, lyo)
- `Lyo.Lock` — (direct, lyo)
- `Lyo.Sqlite` — (direct, lyo)
- `Microsoft.EntityFrameworkCore` `10.0.5` — (direct, microsoft)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` — (direct, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` — (direct, microsoft)
- `Lyo.Common` — (transitive, lyo)
- `Lyo.Compression` — (transitive, lyo)
- `Lyo.ContentThreatScan` — (transitive, lyo)
- `Lyo.Encryption` — (transitive, lyo)
- `Lyo.Hashing` — (transitive, lyo)
- `Lyo.IO.Temp` — (transitive, lyo)
- `Lyo.KeyStore` — (transitive, lyo)
- `Lyo.Metrics` — (transitive, lyo)
- `Lyo.Result` — (transitive, lyo)
- `Lyo.Streams` — (transitive, lyo)
- `BouncyCastle.Cryptography` `2.6.2` — (transitive, third-party, netstandard2.0)
- `EasyCompressor` `2.1.0` — (transitive, third-party)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` — (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` — (transitive, microsoft, netstandard2.0)
- `Microsoft.EntityFrameworkCore.Sqlite` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options.DataAnnotations` `10.0.5` — (transitive, microsoft)
- `System.Buffers` `4.6.1` — (transitive, microsoft, netstandard2.0)
- `System.IO.Hashing` `10.0.5` — (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` — (transitive, microsoft, netstandard2.0)