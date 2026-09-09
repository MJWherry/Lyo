# Lyo.Config

Typed, definition-driven configuration for per-entity values (a Discord guild, a tenant). The abstract API lives here. PostgreSQL persistence is in `Lyo.Config.Postgres`.

## Concepts

| Piece | Role |
| -------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `ConfigDefinitionRecord` | Declares an allowed key for an `ForEntityType` (CLR type name string), the CLR value type, optional default, `IsRequired`, and metadata. |
| `ConfigBindingRecord` | Stores the actual value for one **entity instance** (`EntityRef`: type + id) under a definition. |
| `ConfigValue` | Wrapper: CLR type name + JSON payload. Serialize/deserialize with `ConfigJsonSerializerOptions.Default` when you do not pass custom `JsonSerializerOptions`. |
| `ResolvedConfigRecord` | Produced by `LoadConfigAsync`: every definition for that entity type, each with optional binding. `Value` is `binding ?? default`. |
| `IsEncrypted` | On the definition. Clients send plaintext JSON plus this flag. The API host encrypts at rest. Web.Components, TestGateway, and CLI never call `IEncryptionService`. |
| `ConfigDefinitionRevisionRecord` | Append-only snapshot of key metadata (type, required, default, encrypted flag). Separate from binding **value** history. |

Definitions are unique per `(ForEntityType, Key)`. Bindings are unique per `(DefinitionId, ForEntityType, ForEntityId)`. In PostgreSQL, `config_binding` has a `value_type` column (same CLR type name as `config_definition.for_value_type`, denormalized for querying and exports).

## JSON

`ConfigJsonSerializerOptions.Default` is used whenever `ConfigValue` callers pass `null` for options: camelCase property names, case-insensitive deserialization, omit nulls when writing. That keeps API JSON and stored `value_json` aligned.

## `IsRequired`

If `IsRequired` is true and the definition has **no** default (`DefaultValue` null), each entity must have a **binding** for that key. - `LoadConfigAsync` calls `ResolvedConfigRecord.ValidateRequired()` and throws if any required key has no resolved value. - `DeleteBindingAsync` / `DeleteBindingsAsync` refuse to remove a binding that would violate that rule. If `IsRequired` is true and a **default** exists, the default supplies the resolved value when no binding exists (deleting the binding is allowed).

## Deleting definitions

`DeleteDefinitionAsync` removes the definition row. In PostgreSQL, `config_binding` rows referencing that definition are removed by foreign-key `ON DELETE CASCADE`.

## Versioning

Two version stories:

## Versioning. 1. CLR / definition type changes

Each definition's `ForValueType` is the CLR type name for the stored JSON, same convention as `ForEntityType`: `Type.FullName` (same form as `ConfigValue.TypeName`; use `ConfigValue.GetTypeName(typeof(T))` when seeding). If you rename types, split types, or change the JSON shape incompatibly: - Update the **definition** (and seeders) so `ForValueType` matches the new type. - **Migrate** existing `value_json` (or delete bindings and recreate), or introduce a **new key** and deprecate the old one. The `Lyo.Config` layer does not auto-migrate arbitrary payloads.

## Versioning. 2. Document schema version inside the JSON (optional pattern)

For a **single JSON document** stored as one binding (e.g. `DiscordGuildSettings`), use an integer `Version` field and a `CurrentSchemaVersion` constant on the model:

- `NormalizeForRead()` After `GetValue<T>()`, fix legacy documents (e.g. `Version <= 0` or older version numbers): set defaults for new properties, rewrite fields, then set `Version` to the version you've upgraded to.
- `NormalizeForPersistence()` Before `ConfigValue.From`, call this so every save writes `Version == CurrentSchemaVersion`.

When you add a breaking or additive shape change: bump `CurrentSchemaVersion`, extend `NormalizeForRead()` with `if (Version == n) { …; Version = n + 1; }` (or jump straight to current), and deploy readers before or with writers.

This is **application-level** migration inside one binding value. It does not replace backups or one-off SQL migrations when you need them.

## Versioning. 3. Binding value history (revert)

PostgreSQL stores **append-only value revisions** in `config.config_binding_revision`: primary key is `(binding_id, revision)` (no separate row id). Each successful `SaveBindingAsync` writes a new row with a monotonic `revision` number (1-based per binding).

- `GetBindingRevisionsAsync` / `GetBindingRevisionAsync`. Inspect history (newest first in the list overload).
- `RevertBindingToRevisionAsync`. Copies the snapshot at `revision` onto the binding and **appends** a new revision (so the timeline stays linear and "revert" is auditable).

Deleting a binding (or its definition) cascades and removes revision rows. Adding a definition does **not** create a value revision. Value history starts at the first binding save.

## Versioning. 4. Definition metadata history (revert)

PostgreSQL stores **append-only definition revisions** in `config.config_definition_revision`: primary key is `(definition_id, revision)`. Every `SaveDefinitionAsync` appends a snapshot (type, required, description, default, encrypted flag) with no unchanged-JSON skip.

- `GetDefinitionRevisionsAsync` / `GetDefinitionRevisionAsync`.
- `RevertDefinitionToRevisionAsync`. Copies the snapshot onto the definition and **appends** a new revision so revert stays auditable.

## `IConfigStore` at a glance

- `SaveDefinitionAsync(ConfigDefinitionRecord)` upserts and always appends a definition revision.
- `GetDefinitionByIdAsync(Guid)` / `GetDefinitionAsync(string forEntityType, string key)` look up one definition.
- `GetDefinitionsAsync(string forEntityType)` enumerates definitions for a type.
- `DeleteDefinitionAsync(Guid)` deletes. Postgres cascades to `config_binding` rows.
- `GetDefinitionRevisionsAsync` / `GetDefinitionRevisionAsync` / `RevertDefinitionToRevisionAsync` for metadata history.
- `GetBindingRevisionsAsync` / `RevertBindingToRevisionAsync` for per-entity value history.

## `AppConfigEntity`

- `AppEntityType = "App"` (the stored `EntityRef.EntityType` for app-scoped definitions and bindings).
- `ToEntityRef(string appKind, string appId)` / `TryCreate(...)` URI-decode each segment, lowercase, validate the slug character set (`a-z`, `0-9`, `-`, `_`, `.`, length ≤ 128), and produce `new EntityRef("App", $"{kindNorm}:{idNorm}")`. This is the compound-id shape that `ConfigBindingRecord.ForEntityId` is sized for (string, not `Guid`).

## See also

- [`Lyo.Config.Postgres`](../Lyo.Config.Postgres/README.md). EF Core schema (`config` schema), `PostgresConfigStore`, migrations.
- [`Lyo.EntityReference.Models`](../../../Core/EntityReference/Lyo.EntityReference.Models/README.md). `EntityRef` used throughout the binding APIs.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Json` (direct, lyo)
- `Lyo.EntityReference.Models` (direct, lyo)
- `Lyo.Validation` (direct, lyo)
- `System.Text.Json` `10.0.5` (direct, microsoft, netstandard2.0)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.Parameters` (transitive, lyo)
- `Lyo.Query.Evaluation` (transitive, lyo)
- `Lyo.Query.Models` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Validation.Models` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `System.ComponentModel.Annotations` `5.0.0` (transitive, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)