# Lyo.Authorization

A grant is a subject, an action, a resource type, an optional resource id, and an optional tenant. It does not sign anyone in and it does not parse tokens. A null resource id
covers every id of that type. A set id matches only that id. The same split applies to tenant. There are no deny rows.

## Features

- **Expired at utc now is denied.** An expired row still occupies the active key until it is revoked. Save supersedes an expired row and inserts a new one.
- **Checks are not audited.** Saves, revokes, and supersedes are. A throwing audit recorder does not fail the save.

## Examples

### Register and check

```csharp
services.AddInMemoryAuthorization();
var auth = app.Services.GetRequiredService<IResourceAuthorization>();
var allowed = await auth.IsAllowedAsync(new GrantCheck {
    Subject = userId,
    Action = "read",
    ResourceType = "note",
    ResourceId = noteId.ToString("D")
}, ct);
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Audit` (direct, lyo)
- `Lyo.Common.Core` (direct, lyo)
- `Lyo.EntityReference.Models` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Health` (direct, lyo)
- `Lyo.Metrics` (direct, lyo)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)