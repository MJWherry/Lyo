# Lyo.Job.Tests

Unified tests for the job domain: client, worker, and Postgres integration.

## Layout

| Area | Location | Trait |
|------|----------|-------|
| Client | project root | _(none — fast unit tests)_ |
| Worker | project root | _(none — fast unit tests)_ |
| Postgres mapping/extensions | `Postgres/` | _(none — fast unit tests)_ |
| Postgres integration | `Postgres/` | `Category=Integration` |

## Running

```bash
# Fast unit tests only (no Docker)
dotnet test Integration/Job/Lyo.Job.Tests/Lyo.Job.Tests.csproj --filter "Category!=Integration"

# Full suite (requires Docker for Testcontainers Postgres)
dotnet test Integration/Job/Lyo.Job.Tests/Lyo.Job.Tests.csproj
```
