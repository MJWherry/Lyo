# Lyo.FileSystemWatcher.Postgres

EF Core persistence for Lyo.FileSystemWatcher models. Schema `filesystem_watcher` holds watch, snapshot (tree jsonb + content_hash + content_hash_algorithm), and change rows. Hashes use Lyo.Hashing (SHA-256 by default). `FileSystemWatcherPersister` copies DTOs on ScanCompleted and writes them off the debounce thread. Register with AddPostgresFileSystemWatcherStore / FromConfiguration.

## Features

- **Store only.** No Lyo.Api reference. Drift HTTP lives in Lyo.Drift.Postgres.
- **Named hash.** Dedupe compares content_hash_algorithm and content_hash together.
- **Persister.** Maps ScanCompleted on the timer thread, writes through IFileSystemWatcherStore on a background channel.

## Examples

### Register the store

```csharp
services.AddPostgresFileSystemWatcherStoreFromConfiguration(configuration);
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Json` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.FileSystemWatcher` (direct, lyo)
- `Lyo.FileSystemWatcher.Models` (direct, lyo)
- `Lyo.Hashing` (direct, lyo)
- `Lyo.Postgres` (direct, lyo)
- `Microsoft.EntityFrameworkCore` `10.0.5` (direct, microsoft)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (direct, microsoft)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.Health` (transitive, lyo)
- `Lyo.IO.FileSystem` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (transitive, microsoft)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` (transitive, third-party)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)