# Lyo.Config

Typed, definition-driven configuration for **per-entity** values (e.g. a Discord guild, a tenant). The abstract API lives here; **PostgreSQL** persistence is in
`Lyo.Config.Postgres`.

## Concepts

| Piece                        | Role                                                                                                                                                             |
|------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **`ConfigDefinitionRecord`** | Declares an allowed key for an `ForEntityType` (CLR type name string), the CLR value type, optional default, `IsRequired`, and metadata.                         |
| **`ConfigBindingRecord`**    | Stores the actual value for one **entity instance** (`EntityRef`: type + id) under a definition.                                                                 |
| **`ConfigValue`**            | Wrapper: CLR type name + JSON payload. Serialize/deserialize with **`ConfigJsonSerializerOptions.Default`** when you do not pass custom `JsonSerializerOptions`. |
| **`ResolvedConfigRecord`**   | Produced by **`LoadConfigAsync`**: every definition for that entity type, each with optional binding; **`Value`** is `binding ?? default`.                       |

Definitions are unique per `(ForEntityType, Key)`. Bindings are unique per `(DefinitionId, ForEntityType, ForEntityId)`. In PostgreSQL, `config_binding` has a `value_type` column (
same CLR type name as `config_definition.for_value_type`, denormalized for querying and exports).

## JSON

**`ConfigJsonSerializerOptions.Default`** is used whenever `ConfigValue` callers pass `null` for options: camelCase property names, case-insensitive deserialization, omit nulls
when writing. Keeps API JSON and stored `value_json` aligned.

## `IsRequired`

If **`IsRequired`** is true and the definition has **no** default (`DefaultValue` null), each entity must have a **binding** for that key.

- **`LoadConfigAsync`** calls **`ResolvedConfigRecord.ValidateRequired()`** and throws if any required key has no resolved value.
- **`DeleteBindingAsync` / `DeleteBindingsAsync`** refuse to remove a binding that would violate that rule.

If **`IsRequired`** is true and a **default** exists, the default supplies the resolved value when no binding exists (deleting the binding is allowed).

## Deleting definitions

**`DeleteDefinitionAsync`** removes the definition row. In PostgreSQL, **`config_binding`** rows referencing that definition are removed by **foreign-key `ON DELETE CASCADE`**.

## Versioning

There are two different “version” stories:

### 1. CLR / definition type changes

Each definition’s **`ForValueType`** is the CLR type name for the stored JSON, same convention as **`ForEntityType`**: `Type.FullName` (same form as `ConfigValue.TypeName`; use
`ConfigValue.GetTypeName(typeof(T))` when seeding). If you rename types, split types, or change the JSON shape incompatibly:

- Update the **definition** (and seeders) so `ForValueType` matches the new type.
- **Migrate** existing `value_json` (or delete bindings and recreate), or introduce a **new key** and deprecate the old one.

The `Lyo.Config` layer does not auto-migrate arbitrary payloads.

### 2. Document schema version inside the JSON (optional pattern)

For a **single JSON document** stored as one binding (e.g. **`DiscordGuildSettings`**), use an integer **`Version`** field and a **`CurrentSchemaVersion`** constant on the model:

- **`NormalizeForRead()`** — After `GetValue<T>()`, fix legacy documents (e.g. `Version <= 0` or older version numbers): set defaults for new properties, rewrite fields, then set *
  *`Version`** to the version you’ve upgraded to.
- **`NormalizeForPersistence()`** — Before **`ConfigValue.From`**, call this so every save writes **`Version == CurrentSchemaVersion`**.

When you add a breaking or additive shape change: bump **`CurrentSchemaVersion`**, extend **`NormalizeForRead()`** with `if (Version == n) { …; Version = n + 1; }` (or jump
straight to current), and deploy readers before or with writers.

This is **application-level** migration inside one binding value; it does not replace backups or one-off SQL migrations when you need them.

### 3. Binding value history (revert)

PostgreSQL stores **append-only revisions** in **`config.config_binding_revision`**: primary key is **`(binding_id, revision)`** (no separate row id). Each successful *
*`SaveBindingAsync`** writes a new row with a monotonic **`revision`** number (1-based per binding). The current value still lives on **`config_binding`** for fast reads.

- **`GetBindingRevisionsAsync`** / **`GetBindingRevisionAsync`** — inspect history (newest first in the list overload).
- **`RevertBindingToRevisionAsync`** — copies the snapshot at **`revision`** onto the binding and **appends** a new revision (so the timeline stays linear and “revert” is
  auditable).

Deleting a binding (or its definition) cascades and removes revision rows. The initial migration seeds revision **1** from each existing binding’s current value so history starts
at deploy time.

## See also

- **`IConfigStore`** — full API surface.
- **`Lyo.Config.Postgres`** — EF Core schema (`config` schema), **`PostgresConfigStore`**, migrations.

<!-- LYO_README_SYNC:BEGIN -->

## Dependencies

*(Synchronized from `Lyo.Config.csproj`.)*

**Target framework:** `netstandard2.0;net10.0`

### NuGet packages

| Package | Version |
| --- | --- |
| `System.Text.Json` | `[10,)` |

### Project references

- `Lyo.Common`

## Public API (generated)

Top-level `public` types in `*.cs` (*8*). Nested types and file-scoped namespaces may omit some entries.

- `ConfigBindingRecord`
- `ConfigBindingRevisionRecord`
- `ConfigDefinitionRecord`
- `ConfigJsonSerializerOptions`
- `ConfigValue`
- `IConfigStore`
- `ResolvedConfigItemRecord`
- `ResolvedConfigRecord`

<!-- LYO_README_SYNC:END -->

