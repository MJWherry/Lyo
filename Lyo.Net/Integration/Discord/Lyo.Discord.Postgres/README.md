# Lyo.Discord.Postgres

PostgreSQL persistence and `Lyo.Api` endpoint mappings for Discord entities. Schema name is fixed to `discord` (`PostgresDiscordOptions.Schema`).

## Examples

### Quick start

```csharp
services.AddPostgresDiscordFromConfiguration(builder.Configuration);
services.AddDiscordGuildSettingsInfrastructure(); // optional, requires AddPostgresConfigStore
services.AddSingleton(new TypeAdapterConfig().ConfigureDiscordMappings());

var app = builder.Build();
app.BuildDiscordGroup();
app.Run();
```

## What ships

- `DiscordDbContext` ([`Database/DiscordDbContext.cs`](Database/DiscordDbContext.cs)) with `DbSet`s for `DiscordUser`, `DiscordGuild`, `DiscordChannel`, `DiscordEmoji`, `DiscordRole`, `DiscordInteraction`, `DiscordMessage`, `DiscordAttachment`, `DiscordReaction` (composite key `(MessageId, ReactorId, EmojiId)`), and `DiscordMember` (composite key `(UserId, GuildId)`). The model snapshot lives under [`Migrations/`](Migrations).
- `DiscordDbContextFactory` for design-time tooling.
- `PostgresDiscordOptions` ([`PostgresDiscordOptions.cs`](PostgresDiscordOptions.cs)). `IPostgresMigrationConfig`. Section `"PostgresDiscord"`. Properties: `ConnectionString`, `EnableAutoMigrations` (default `false`); `Schema = "discord"` is constant.
- **Endpoint mapping helpers** in [`Extensions.cs`](Extensions.cs) and [`DiscordGuildSettingsEndpoints.cs`](DiscordGuildSettingsEndpoints.cs).
- `DiscordGuildSettingsHelper` for resolving / seeding default `DiscordGuildSettings` bindings.
- `DiscordGuildSettingsDefinitionSeeder`. `IHostedService` that registers the settings document definition with `Lyo.Config`.

## DI registration ([`Extensions.cs`](Extensions.cs))

All registrations are extension methods on `IServiceCollection` (declared inside `extension(IServiceCollection services)` blocks):

| Method | Description |
| ------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `AddDiscordDbContext(string connectionString)` | Convenience: registers the DbContext factory plus a scoped `DiscordDbContext` resolved from the factory. |
| `AddDiscordDbContext(Action<DbContextOptionsBuilder>)` | Standard EF `AddDbContext` overload. |
| `AddDiscordDbContextFactory(Action<PostgresDiscordOptions>)` | Builds options inline. |
| `AddDiscordDbContextFactoryFromConfiguration(config, sectionName?)` | Binds options from the supplied configuration (default section `"PostgresDiscord"`). |
| `AddDiscordDbContextFactory(PostgresDiscordOptions)` | Registers the factory and `Npgsql` provider, points the migrations history table at the `discord` schema, and wires `AddPostgresMigrations<DiscordDbContext, …>`. |
| `AddPostgresDiscord(...)` (3 overloads) | The factory method above **plus** `AddLyoCrudServices<DiscordDbContext>()`. Use this when hosting the Discord REST API. |
| `AddDiscordGuildSettingsInfrastructure()` | Registers the hosted seeder so `Lyo.Config` knows about `DiscordGuildSettings` (requires `AddPostgresConfigStore`). |

`AddPostgresDiscord` requires that the host has already registered `AddLyoQueryServices`, a cache implementation, and an `ILyoMapper` (e.g. via `MapsterLyoMapper` configured by
`ConfigureDiscordMappings`).

## Mapster mappings (`ConfigureDiscordMappings`)

Call once on your `TypeAdapterConfig` to wire the `*Req` ↔ EF ↔ `*Res` mappings used by `Lyo.Api` CRUD. Two name-sanitizers run for nullable input: `DiscordUsernameOrPlaceholder` (≤ 35 chars, falls back to `"(unknown)"`) and `DiscordGuildNameOrPlaceholder` (≤ 50 chars). Audit timestamps (`CreatedTimestamp`, `UpdatedTimestamp`) are explicitly ignored from request DTOs because the CRUD hooks set them.

## Endpoint mapping (`app.BuildDiscordGroup()`)

Maps Discord REST endpoints under group `"Discord"` using the typed `CreateBuilder` pipeline. Each entity gets `ApiFeatureFlag.All | UpsertInheritCreate |
UpsertInheritUpdate | PatchInheritsUpdate` plus a `CrudConfiguration<DiscordDbContext, T, TReq>` that stamps `CreatedTimestamp` / `UpdatedTimestamp` in `BeforeCreate` /
`BeforeUpdate`:

| Route segment | EF entity | Notes |
| --------------------- | -------------------- | ----------------------------------------------------------------------------------------------------------------------------------- |
| `Discord/User` | `DiscordUser` | Audit hook only. |
| `Discord/Guild` | `DiscordGuild` | Audit hook **and** `AfterUpsert` that calls `DiscordGuildSettingsHelper.EnsureDefaultBindingAsync` if `IConfigStore` is registered. |
| `Discord/Channel` | `DiscordChannel` | Audit hook only. |
| `Discord/Emoji` | `DiscordEmoji` | Audit hook only. |
| `Discord/Role` | `DiscordRole` | Audit hook only. |
| `Discord/Interaction` | `DiscordInteraction` | Default config (no audit columns on this entity). |
| `Discord/Message` | `DiscordMessage` | Audit hook only. |
| `Discord/Attachment` | `DiscordAttachment` | Default config. |
| `Discord/Member` | `DiscordMember` | Composite PK `(UserId, GuildId)` → only `Query`, `Upsert`/`UpsertBulk`, `Patch`/`PatchBulk` (no GET-by-id). |

`BuildDiscordGroup` also calls `MapDiscordGuildSettingsEndpoints`, which (only when an `IConfigStore` is registered) maps:

| Method | Route |
|--------|--------------------------------------------------------------------|
| `GET` | `Discord/Guild/{guildId:long}/GuildSettings` |
| `PUT` | `Discord/Guild/{guildId:long}/GuildSettings` |
| `GET` | `Discord/Guild/{guildId:long}/GuildSettings/Revisions` |
| `POST` | `Discord/Guild/{guildId:long}/GuildSettings/Revert/{revision:int}` |

The PUT route validates the guild exists, clears `Revision` before save, calls `NormalizeForPersistence`, and round-trips the persisted value back to the caller. The host is
expected to layer authorization on top. No auth is wired here by default.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Api` (direct, lyo)
- `Lyo.Config` (direct, lyo)
- `Lyo.Discord.Models` (direct, lyo)
- `Lyo.EntityReference.Models` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Postgres` (direct, lyo)
- `Mapster` `10.0.10` (direct, third-party)
- `Microsoft.EntityFrameworkCore` `10.0.5` (direct, microsoft)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (direct, microsoft)
- `Lyo.Api.Models` (transitive, lyo)
- `Lyo.Cache` (transitive, lyo)
- `Lyo.Common` (transitive, lyo)
- `Lyo.Compression` (transitive, lyo)
- `Lyo.DateAndTime` (transitive, lyo)
- `Lyo.Diagnostic` (transitive, lyo)
- `Lyo.Diagnostic.AspNetCore` (transitive, lyo)
- `Lyo.Diff` (transitive, lyo)
- `Lyo.Encryption` (transitive, lyo)
- `Lyo.Formatter` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.Health` (transitive, lyo)
- `Lyo.KeyStore` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.PackageMetadata` (transitive, lyo)
- `Lyo.Query` (transitive, lyo)
- `Lyo.Query.Models` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Streams` (transitive, lyo)
- `Lyo.Validation` (transitive, lyo)
- `BouncyCastle.Cryptography` `2.6.2` (transitive, third-party, netstandard2.0)
- `EasyCompressor` `2.1.0` (transitive, third-party)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` (transitive, third-party)
- `Microsoft.AspNetCore.Authorization` `10.0.5` (transitive, microsoft)
- `Microsoft.AspNetCore.Http.Abstractions` `2.*` (transitive, microsoft)
- `Microsoft.AspNetCore.OpenApi` `10.0.5` (transitive, microsoft)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.EntityFrameworkCore.Analyzers` `10.0.5` (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Caching.Memory` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` (transitive, third-party)
- `SmartFormat.NET` `3.6.1` (transitive, third-party)
- `System.Buffers` `4.6.1` (transitive, microsoft, netstandard2.0)
- `System.ComponentModel.Annotations` `5.0.0` (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft, netstandard2.0)