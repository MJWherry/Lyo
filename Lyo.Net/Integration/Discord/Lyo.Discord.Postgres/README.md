# Lyo.Discord.Postgres

PostgreSQL persistence and `Lyo.Api` endpoint mappings for Discord entities. Schema name is fixed to `discord` (`PostgresDiscordOptions.Schema`).

## What ships

- **`DiscordDbContext`** ([`Database/DiscordDbContext.cs`](Database/DiscordDbContext.cs)) with `DbSet`s for **`DiscordUser`**, **`DiscordGuild`**, **`DiscordChannel`**,
  **`DiscordEmoji`**, **`DiscordRole`**, **`DiscordInteraction`**, **`DiscordMessage`**, **`DiscordAttachment`**, **`DiscordReaction`** (composite key
  `(MessageId, ReactorId, EmojiId)`), and **`DiscordMember`** (composite key `(UserId, GuildId)`). The model snapshot lives under [`Migrations/`](Migrations).
- **`DiscordDbContextFactory`** for design-time tooling.
- **`PostgresDiscordOptions`** ([`PostgresDiscordOptions.cs`](PostgresDiscordOptions.cs)) — `IPostgresMigrationConfig`. Section `"PostgresDiscord"`. Properties:
  `ConnectionString`, `EnableAutoMigrations` (default `false`); `Schema = "discord"` is constant.
- **Endpoint mapping helpers** in [`Extensions.cs`](Extensions.cs) and [`DiscordGuildSettingsEndpoints.cs`](DiscordGuildSettingsEndpoints.cs).
- **`DiscordGuildSettingsHelper`** for resolving / seeding default `DiscordGuildSettings` bindings.
- **`DiscordGuildSettingsDefinitionSeeder`** — `IHostedService` that registers the settings document definition with `Lyo.Config`.

## DI surface ([`Extensions.cs`](Extensions.cs))

All registrations are extension methods on `IServiceCollection` (declared inside `extension(IServiceCollection services)` blocks):

| Method                                                              | Description                                                                                                                                                       |
|---------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `AddDiscordDbContext(string connectionString)`                      | Convenience: registers the DbContext factory plus a scoped `DiscordDbContext` resolved from the factory.                                                          |
| `AddDiscordDbContext(Action<DbContextOptionsBuilder>)`              | Standard EF `AddDbContext` overload.                                                                                                                              |
| `AddDiscordDbContextFactory(Action<PostgresDiscordOptions>)`        | Builds options inline.                                                                                                                                            |
| `AddDiscordDbContextFactoryFromConfiguration(config, sectionName?)` | Binds options from the supplied configuration (default section `"PostgresDiscord"`).                                                                              |
| `AddDiscordDbContextFactory(PostgresDiscordOptions)`                | Registers the factory and `Npgsql` provider, points the migrations history table at the `discord` schema, and wires `AddPostgresMigrations<DiscordDbContext, …>`. |
| `AddPostgresDiscord(...)` (3 overloads)                             | The factory method above **plus** `AddLyoCrudServices<DiscordDbContext>()`. Use this when hosting the Discord REST API.                                           |
| `AddDiscordGuildSettingsInfrastructure()`                           | Registers the hosted seeder so `Lyo.Config` knows about `DiscordGuildSettings` (requires `AddPostgresConfigStore`).                                               |

`AddPostgresDiscord` requires that the host has already registered `AddLyoQueryServices`, a cache implementation, and an `ILyoMapper` (e.g. via `MapsterLyoMapper` configured by
`ConfigureDiscordMappings`).

## Mapster mappings — `ConfigureDiscordMappings`

Call once on your `TypeAdapterConfig` to wire the `*Req` ↔ EF ↔ `*Res` mappings used by `Lyo.Api` CRUD. Two name-sanitizers run for nullable input:
`DiscordUsernameOrPlaceholder` (≤ 35 chars, falls back to `"(unknown)"`) and `DiscordGuildNameOrPlaceholder` (≤ 50 chars). Audit timestamps (`CreatedTimestamp`,
`UpdatedTimestamp`) are explicitly ignored from request DTOs because the CRUD hooks set them.

## Endpoint mapping — `app.BuildDiscordGroup()`

Maps the full Discord REST surface under group `"Discord"` using the typed `CreateBuilder` pipeline. Each entity gets `ApiFeatureFlag.All | UpsertInheritCreate |
UpsertInheritUpdate | PatchInheritsUpdate` plus a `CrudConfiguration<DiscordDbContext, T, TReq>` that stamps `CreatedTimestamp` / `UpdatedTimestamp` in `BeforeCreate` /
`BeforeUpdate`:

| Route segment         | EF entity            | Notes                                                                                                                               |
|-----------------------|----------------------|-------------------------------------------------------------------------------------------------------------------------------------|
| `Discord/User`        | `DiscordUser`        | Audit hook only.                                                                                                                    |
| `Discord/Guild`       | `DiscordGuild`       | Audit hook **and** `AfterUpsert` that calls `DiscordGuildSettingsHelper.EnsureDefaultBindingAsync` if `IConfigStore` is registered. |
| `Discord/Channel`     | `DiscordChannel`     | Audit hook only.                                                                                                                    |
| `Discord/Emoji`       | `DiscordEmoji`       | Audit hook only.                                                                                                                    |
| `Discord/Role`        | `DiscordRole`        | Audit hook only.                                                                                                                    |
| `Discord/Interaction` | `DiscordInteraction` | Default config (no audit columns on this entity).                                                                                   |
| `Discord/Message`     | `DiscordMessage`     | Audit hook only.                                                                                                                    |
| `Discord/Attachment`  | `DiscordAttachment`  | Default config.                                                                                                                     |
| `Discord/Member`      | `DiscordMember`      | Composite PK `(UserId, GuildId)` → only `Query`, `Upsert`/`UpsertBulk`, `Patch`/`PatchBulk` (no GET-by-id).                         |

`BuildDiscordGroup` also calls `MapDiscordGuildSettingsEndpoints`, which (only when an `IConfigStore` is registered) maps:

| Method | Route                                                              |
|--------|--------------------------------------------------------------------|
| `GET`  | `Discord/Guild/{guildId:long}/GuildSettings`                       |
| `PUT`  | `Discord/Guild/{guildId:long}/GuildSettings`                       |
| `GET`  | `Discord/Guild/{guildId:long}/GuildSettings/Revisions`             |
| `POST` | `Discord/Guild/{guildId:long}/GuildSettings/Revert/{revision:int}` |

The PUT route validates the guild exists, clears `Revision` before save, calls `NormalizeForPersistence`, and round-trips the persisted value back to the caller. The host is
expected to layer authorization on top — no auth is wired here by default.

## Quick start

```csharp
services.AddPostgresDiscordFromConfiguration(builder.Configuration);
services.AddDiscordGuildSettingsInfrastructure();        // optional, requires AddPostgresConfigStore
services.AddSingleton(new TypeAdapterConfig().ConfigureDiscordMappings());

var app = builder.Build();
app.BuildDiscordGroup();
app.Run();
```

## Related projects

- [`Lyo.Api`](../../Api/Lyo.Api/README.md) — endpoint pipeline.
- [`Lyo.Config`](../../../Features/Config/Lyo.Config/README.md) — guild-settings store backing `DiscordGuildSettings`.
- [`Lyo.Discord.Models`](../Lyo.Discord.Models/README.md) — DTOs and route constants.
- [`Lyo.Postgres`](../../../Data/Postgres/Lyo.Postgres/README.md) — migrations infrastructure.
- [`Lyo.Exceptions`](../../../Core/Lyo.Exceptions/README.md)
