# Lyo.Query.Postgres

PostgreSQL adapter for [`Lyo.Query`](../Lyo.Query/README.md). `AddLyoQueryPostgres()` replaces `IWhereClauseService` with `PostgresWhereClauseService`, which rewrites `FuzzyMatch.Similarity` calls to `EF.Functions.TrigramsSimilarity` so Npgsql emits SQL `similarity(col, query) >= threshold`.

The evaluator stays EF-agnostic: in-memory matchers and `EnumerableQuery` keep the CLR Jaccard implementation. SQL Fuzzy requires `CREATE EXTENSION IF NOT EXISTS pg_trgm` once per database. GIN/GiST `gin_trgm_ops` indexes are not created by this package.

Targets `net10.0`. Call after `AddLyoQueryServices()`.

## Features

- **SQL Fuzzy / NotFuzzy.** `similarity()` via Npgsql `EF.Functions.TrigramsSimilarity`, with a per-condition threshold (default 0.3).
- **CLR fallback.** `EnumerableQuery`, `MatchesWhereClause`, and `ExplainMatch` keep `FuzzyMatch.Similarity` so they do not pull the table into memory.
- **Opt-in DI.** `AddLyoQueryPostgres()` `Replace`s `IWhereClauseService`. Without it, Fuzzy on EF fails translation instead of silently scanning.

## Examples

### Register after Lyo.Query

```csharp
using Lyo.Query;
using Lyo.Query.Postgres;

services.AddLocalCache(/* … */);
services.AddLyoQueryServices();
services.AddLyoQueryPostgres();

// Database (once): CREATE EXTENSION IF NOT EXISTS pg_trgm;
```

### Fuzzy where clause

```csharp
var where = WhereClauseBuilder.And()
    .Fuzzy("Name", "jon", threshold: 0.4)
    .Build();

var filtered = whereClauseService.ApplyWhereClause(db.People, where);
```

## Indexes

The `%` operator can use a GIN/GiST `gin_trgm_ops` index more readily than `similarity() >= n`. v1 emits `similarity()` so mixed per-condition thresholds in one tree stay correct. Add `CREATE INDEX … USING gist (lower(name) gist_trgm_ops)` on hot columns if you own the schema; this package does not.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Query` (direct, lyo)
- `Lyo.Query.Evaluation` (direct, lyo)
- `Microsoft.EntityFrameworkCore` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` (direct, third-party)
- `Lyo.Cache` (transitive, lyo)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.Compression` (transitive, lyo)
- `Lyo.Encryption` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.Health` (transitive, lyo)
- `Lyo.Json` (transitive, lyo)
- `Lyo.KeyStore` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.Parameters` (transitive, lyo)
- `Lyo.Query.Models` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Streams` (transitive, lyo)
- `BouncyCastle.Cryptography` `2.6.2` (transitive, third-party, netstandard2.0)
- `EasyCompressor` `2.1.0` (transitive, third-party)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Caching.Memory` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `System.Buffers` `4.6.1` (transitive, microsoft, netstandard2.0)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Encoding.CodePages` `10.0.5` (transitive, microsoft)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft, netstandard2.0)