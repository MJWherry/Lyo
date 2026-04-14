# Lyo.Postgres

Shared PostgreSQL migration infrastructure for Lyo libraries. Provides `IHostedService` for running EF Core migrations at application startup (when `EnableAutoMigrations` is true).

## Features

- **IPostgresMigrationConfig** – Interface for options (ConnectionString, EnableAutoMigrations, Schema)
- **PostgresMigrationHostedService&lt;TContext, TOptions&gt;** – Runs migrations when the host starts
- **AddPostgresMigrations&lt;TContext, TOptions&gt;** – Registers the hosted service

## Usage

This package is typically used by other Lyo Postgres packages (e.g. Lyo.Audit.Postgres, Lyo.Email.Postgres). When you call `AddPostgresAuditRecorder` or `AddEmailDbContextFactory`
with `EnableAutoMigrations = true`, migrations run at **host startup** via `IHostedService`, not during service registration.

Ensure your application uses a host (e.g. `Host.CreateDefaultBuilder()` or `WebApplication.CreateBuilder()`).

<!-- LYO_README_SYNC:BEGIN -->

## Dependencies

*(Synchronized from `Lyo.Postgres.csproj`.)*

**Target framework:** `net10.0`

### NuGet packages

| Package | Version |
| --- | --- |
| `Microsoft.EntityFrameworkCore` | `[10,)` |
| `Microsoft.EntityFrameworkCore.Design` | `[10,)` |
| `Microsoft.EntityFrameworkCore.Relational` | `[10,)` |
| `Microsoft.Extensions.Hosting.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Logging.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Options` | `[10,)` |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | `[10,)` |

### Project references

- `Lyo.Exceptions`

## Public API (generated)

Top-level `public` types in `*.cs` (*3*). Nested types and file-scoped namespaces may omit some entries.

- `Extensions`
- `IPostgresMigrationConfig`
- `PostgresMigrationHostedService`

<!-- LYO_README_SYNC:END -->

