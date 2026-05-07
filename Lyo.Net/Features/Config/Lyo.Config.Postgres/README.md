# Lyo.Config.Postgres

PostgreSQL implementation of `Lyo.Config` for storing typed config definitions by entity type and config bindings by `EntityRef`.

## Dependencies

*(Synchronized from `Lyo.Config.Postgres.csproj`.)*

**Target framework:** `net10.0`

### NuGet packages

| Package                                           | Version |
|---------------------------------------------------|---------|
| `Microsoft.EntityFrameworkCore.Design`            | `[10,)` |
| `Microsoft.Extensions.Configuration.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Configuration.Binder`       | `[10,)` |

### Project references

- [`Lyo.Config`](../Lyo.Config/README.md)
- [`Lyo.Health`](../../../Core/Health/Lyo.Health/README.md)
- [`Lyo.Postgres`](../../../Data/Postgres/Lyo.Postgres/README.md)