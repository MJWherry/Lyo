# Lyo.FileMetadataStore.Sqlite

**Status:** *Placeholder package.*

The `.csproj` description explicitly marks this NuGet-compatible split as reserving the ID for a future **`SqliteFileMetadataStore`** implementation—but **today there are no SQLite
sources** referenced (project compiles essentially empty besides assembly metadata targets).

Why keep the package?

- Prevents squatting/conflicting identities on NuGet feeds for internal restores.
- Provides a deterministic namespace slot if you prototype offline-first clients or WASM-side caches later.

Until code lands:

| Need                                                             | Instead use                                                                                                  |
|------------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------|
| Local dev ergonomics faster than PostgreSQL ephemeral containers | Run lightweight Postgres via compose **or** an in-memory test double mocking **`IFileMetadataStore`**.       |
| Embedded single-user scenario                                    | Embed **LiteDB**/SQLite manually behind a handcrafted `IFileMetadataStore` in your app—not this package yet. |

When implementing:

1. Map **`FileStoreResult`** fields to pragmatic SQLite column types (**BLOB for hashes**, **TEXT for JSON** fragments).
2. Provide migrations via **`Microsoft.EntityFrameworkCore.Sqlite`** (likely share entity classes with Postgres via table splitting—DRY thoughtfully).
3. Document concurrency limitations (SQLite single-writer)—not suitable for high-concurrency ingestion without WAL tuning.

Track removal/modification of the placeholder description in `.csproj` when real code merges so consumers stop seeing “placeholder” in IDE package tooltips.
