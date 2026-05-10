# Lyo.EntityReference.Models

Portable primitives for **entity references** in Lyo: a typed pair of logical entity kind (`EntityType`) and identifier string (`EntityId`), plus helpers for composite keys, JSON, opaque tokens, validation, and domain row shapes. **No database or EF dependency.**

## Concepts

- **`EntityRef`** — Immutable value (`readonly record struct`) used at API boundaries. Constructors validate non-whitespace type and id.
- **Stable type names** — Decorate CLR types with **`[EntityRefLogicalType("MyModule.Widget")]`** so persisted `EntityType` does not depend on `Type.FullName`.
- **Composite ids** — Multiple key segments are sorted lexically and joined via **`EntityRefCompositeEncoding`** so literal `:` inside a segment stays unambiguous.
- **Option A persistence** — Many PostgreSQL modules store `ForEntityId` / `FromEntityId` as **`uuid`**. Use **`EntityRefPersistedGuid`** when `EntityId` must be exactly one GUID string.
- **`EntityRefRow`** — Abstract domain mirror of the canonical association row (tenant, visibility, soft-delete, metadata). Feature modules typically derive their own row/DTO types from this shape.

## Features

| Area | Types |
|------|--------|
| Core reference | `EntityRef`, `EntityRefLogicalTypeAttribute` |
| Composite keys | `EntityRefCompositeEncoding` |
| JSON (`System.Text.Json`) | `EntityRefJsonConverter` (also on **netstandard2.0** via package reference) |
| Opaque framing | `EntityRef.ToOpaqueToken`, `TryParseOpaque`, `ParseOpaque` (separator `EntityRef.OpaqueSeparator`) |
| GUID persistence | `EntityRefPersistedGuid`, `EntityRefPersistenceException` |
| Validation | `EntityRefTypeGuard.EnsureKnown` |
| Configuration | `EntityRefOptions` (`DefaultTenantId`), `EntityRefWellKnown`, `EntityRefVisibility` |
| Hooks | `IEntityRefActionInterceptor`, `EntityRefActionContext`, `EntityRefActionKind` |

## Quick start

```csharp
using System.Text.Json;
using Lyo.EntityReference.Models;

// From CLR type + Guid key (logical type from attribute or FullName)
var r = EntityRef.For<MyAggregate>(aggregate.Id);

// Explicit type + id
var r2 = EntityRef.ForKey("Comic.Issue", issueId.ToString());

// JSON: register converter once
var options = new JsonSerializerOptions();
options.Converters.Add(new EntityRefJsonConverter());
JsonSerializer.Serialize(r, options); // {"entityType":"...","entityId":"..."}

// Opaque token for logs or headers
var token = r.ToOpaqueToken();
EntityRef.ParseOpaque(token);

// Resolve Guid for uuid columns
if (EntityRefPersistedGuid.TryGetPersistedGuid(r, out var guid))
{
    // map to ForEntityId / FromEntityId
}
```

## Debugging

`EntityRef`, `EntityRefRow`, `EntityRefOptions`, and `EntityRefActionContext` use **`[DebuggerDisplay(...)]`** for compact watches. Several types override **`ToString()`** for readable logs (distinct from **`ToOpaqueToken()`** on `EntityRef`).

## Related packages

- **`Lyo.EntityReference.Postgres`** — EF Core entity bases, fluent configuration, `DbContext` stamping, and store helpers for PostgreSQL.
