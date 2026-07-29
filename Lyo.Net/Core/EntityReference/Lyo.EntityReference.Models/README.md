# Lyo.EntityReference.Models

Portable primitives for **entity references** in Lyo: a typed pair of logical entity kind (`EntityType`) and identifier string (`EntityId`), plus helpers for composite keys, JSON,
opaque tokens, validation, and domain row shapes. **No database or EF dependency.**

## Examples

### Quick start

```csharp
using System.Text.Json;
using Lyo.EntityReference.Models;

// From CLR type + Guid key (logical type from attribute or FullName)
var r = EntityRef.For<MyAggregate>(aggregate.Id);

// From entity instance + collection expression keys
var rEntity = EntityRef.For(person, p => [p.Id]);

// Logical type name only
var typeName = EntityRef.LogicalTypeName<MyAggregate>();

// Explicit type + id
var r2 = EntityRef.ForKey("Comic.Issue", issueId.ToString());

// JSON: register converter once
var options = new JsonSerializerOptions();
options.Converters.Add(new EntityRefJsonConverter());
JsonSerializer.Serialize(r, options); // {"entityType":"...","entityId":"..."}

// Relation endpoints (For = subject, From = actor)
var endpoints = EntityRelationBuilder.For<Volume>(volumeId).From<User>(userId);
EntityRelationValidation.RequireSubjectActor(endpoints.Subject, endpoints.Actor);

// Import provenance (owner id assigned on persist)
var source = EntitySourceRecord.From<EndatoPsPerson>(externalId, DateTime.UtcNow);
EntitySourceValidation.RequireSource(source);

// Map EntityRef to persisted string columns (Guid ids become "d290f1ee-6c54-4b01-90e6-d701748f0851")
var subjectId = EntityRefPersistedGuid.PersistedEntityId(subjectRef);
```

## Concepts

- **`EntityRef`** — Immutable value (`readonly record struct`) used at API boundaries. Constructors validate non-whitespace type and id.
- **Stable type names** — Decorate CLR types with **`[EntityRefLogicalType("MyModule.Widget")]`** so persisted `EntityType` does not depend on `Type.FullName`.
- **Composite ids** — Multiple key segments are sorted lexically and joined via **`EntityRefCompositeEncoding`** so literal `:` inside a segment stays unambiguous.
- **Relation vs source** — Two distinct persistence shapes:
- **Relations** (favorite, note, comment, …) — Subject + actor endpoints. Domain types use **`SubjectEntityType`** / **`ActorEntityId`**; PostgreSQL columns remain * *`for_entity_*`** / **`from_entity_*`**.
- **Source links** (`*_source`) — External import provenance only: **`source_entity_type`** / **`source_entity_id`** + **`imported_at`**. Owner identity lives on the parent aggregate (e.g. `person_id`), not in EntityReference.
- **String persistence** — Endpoint and source ids are **nullable varchar** at the DB; stores and validation helpers enforce required values per use case. Callers still pass * *`EntityRef`** at API boundaries; use **`EntityRefPersistedGuid.PersistedEntityId()`** (or **`RequirePersistedGuid`** when comparing to row `Guid` primary keys) when `EntityId` is a single GUID string.
- **`EntityRelationRow`** — Abstract domain mirror of a tenant-scoped relation row (subject/actor, visibility, soft-delete, metadata). Endpoint properties are **`string?`**, aligned with persisted columns.
- **`EntitySourceRecord`** — Provenance shape: **`(EntityRef Source, DateTime ImportedAt)`**. Use **`EntitySourceRecord.From(source, importedAt)`** at import; the owning aggregate id is set when persisting child `*_source` rows.
- **`IEntitySourceDerived`** — Aggregates imported from external sources carry optional **`EntitySourceRecord? Source`** and **`LocallyModifiedAt`** when local edits diverge from the external source.

## Debugging

`EntityRef`, `EntityRelationRow`, `EntityRefOptions`, and `EntityRefActionContext` use **`[DebuggerDisplay(...)]`** for compact watches. Several types override **`ToString()`** for readable logs (distinct from **`ToOpaqueToken()`** on `EntityRef`).

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common` — (direct, lyo)
- `Lyo.Exceptions` — (direct, lyo)
- `System.Text.Json` `10.0.5` — (direct, microsoft, netstandard2.0)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (transitive, microsoft)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)