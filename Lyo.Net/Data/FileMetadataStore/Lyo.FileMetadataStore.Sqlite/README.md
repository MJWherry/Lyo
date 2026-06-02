# Lyo.FileMetadataStore.Sqlite

SQLite implementation of **`IFileMetadataStore`** using Entity Framework Core. Functional parity with [
`Lyo.FileMetadataStore.Postgres`](../Lyo.FileMetadataStore.Postgres/README.md) for embedded, offline-first, and local-dev scenarios.

## What is included

| Component                               | Role                                               |
|-----------------------------------------|----------------------------------------------------|
| **`SqliteFileMetadataStore`**           | `IFileMetadataStore` + `IHealth`                   |
| **`SqliteFileAuditSink`**               | `IFileAuditEventHandler` — append-only audit rows  |
| **`SqliteMultipartUploadSessionStore`** | `IMultipartUploadSessionStore`                     |
| **`SqliteFileDownloadAccessService`**   | Time-boxed download access tokens                  |
| **`SqliteFileMetadataStoreDbContext`**  | EF Core context (5 tables, same shape as Postgres) |

Schema tables: `file_metadata`, `file_data`, `file_audit_events`, `multipart_upload_session`, `file_download_access_links`.

## Registration

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

`appsettings.json`:

```json
{
  "SqliteFileMetadataStore": {
    "ConnectionString": "Data Source=./filestore.db",
    "EnableAutoMigrations": true
  }
}
```

Wire blob storage to the keyed metadata store the same way as Postgres:

```csharp
services.AddS3FileStorageServiceKeyed("my-files")
    .UseFileMetadataStore("sqlite-metadata")
    .Build(configuration);
```

## Migrations

Migrations ship in this package. Enable **`EnableAutoMigrations`** (via [`Lyo.Sqlite`](../../Sqlite/Lyo.Sqlite/README.md)) or run `dotnet ef database update` using
`SqliteFileMetadataStoreDbContextFactory`.

Design-time connection string: `FILEMETADATASTORE_CONNECTION_STRING` or `FILESTORE_CONNECTION_STRING` (defaults to `Data Source=./filestore-design.db`).

## Concurrency

SQLite is single-writer. Suitable for embedded clients, local tools, and low-concurrency dev hosts. For high-throughput multi-instance ingestion use Postgres.

Enable WAL at the connection level if your host opens many concurrent readers (`PRAGMA journal_mode=WAL`).

## Dependencies

- [`Lyo.FileMetadataStore`](../Lyo.FileMetadataStore/README.md)
- [`Lyo.Sqlite`](../../Sqlite/Lyo.Sqlite/README.md)
- [`Lyo.FileStorage`](../../FileStorage/Lyo.FileStorage/README.md)
- `Microsoft.EntityFrameworkCore.Sqlite`
