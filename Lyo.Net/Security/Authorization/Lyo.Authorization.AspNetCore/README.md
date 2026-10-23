# Lyo.Authorization.AspNetCore

Call AddLyoApiTokenAuthentication and then AddLyoGrantAuthorization. The grant policy provider forwards every name that does not start with grant|, including scope policies, role
policies, the default policy, and the fallback policy. A second call is a no-op.

## Features

- **[RequireGrant] and .RequireGrant ()** share one handler. Several attributes on one action are combined with AND.
- **Subject is lyo:user when that claim is set, otherwise sub.** A grant stored only for sub does not match when lyo:user is a different value.
- **Anonymous is 401. Authenticated without a grant is 403.** A store exception is 500. A missing or non-guid tenant route does not succeed and does not throw.
- **MapGrantEndpoints** lists, saves, revokes, and checks grants for an already authenticated caller. Subject is `lyo:user` or else `sub`. Save, revoke, and another subject's list
  or check require `authorization.grants.manage` when the host passes that policy.

## Examples

### Controller and minimal API

```csharp
[RequireScope("note.read")]
[RequireRole("editor")]
[RequireGrant("read", "note", resourceIdRoute: "id")]
[HttpGet("/notes/{id:guid}")]
public IActionResult GetNote(Guid id) => Ok();

group.MapGet("/notes/{id:guid}", GetNote)
    .RequireScope("note.read")
    .RequireRole("editor")
    .RequireGrant("read", "note", resourceIdRoute: "id");

app.MapGrantEndpoints(GrantEndpoints.ManagePolicyName);
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Authorization` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Audit` (transitive, lyo)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.EntityReference.Models` (transitive, lyo)
- `Lyo.Health` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)