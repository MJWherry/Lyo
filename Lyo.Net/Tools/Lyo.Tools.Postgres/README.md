# Lyo.Tools.Postgres

Interactive Spectre.Console TUI for running and rolling back EF Core migrations against the Lyo Postgres `DbContext`s, plus a couple of Bogus-powered seeders. Useful for spinning a
fresh local database up to the latest schema or stepping a single schema back to a specific migration.

## Entry point

`Program.cs` builds a default `Microsoft.Extensions.Hosting` host, registers a singleton `ConnectionStringProvider` (initial value from the `ConnectionString` config key — set in
`appsettings.Development.json` or environment variables), then hands off to `Menu.RunAsync(scope.ServiceProvider, cts.Token)`. Ctrl+C cancels the host.

Three scoped services are wired:

- `MigrationRunner` — drives EF migrations on demand for any registered context.
- `ComicDbSeeder` — Bogus-faked comic series, characters, tags, and authors.
- `PeopleDbSeeder` — Bogus-faked person rows with phones, emails, addresses, etc.

## Main menu

`Menu.cs` renders a Spectre prompt with three options plus exit:

1. **Seeds** — Comic Database or People Database. Each seeder prompts for record count (default 20 / 50) and an optional integer random seed. Both skip if the target table already
   has rows.
2. **Migrations** — see below.
3. **Change Connection String** — accepts a new value and updates `ConnectionStringProvider` in place. The header shows a masked preview (`first 40 chars + ****`).

Errors are caught per menu choice and printed via `WriteError`, then the menu loops.

## Migrations menu

`MigrationRunner` (see `MigrationRunner.cs`) knows about 21 `DbContext`s, each paired with a Postgres schema name used for `__EFMigrationsHistory`:

| Label             | DbContext                    | Schema           |
|-------------------|------------------------------|------------------|
| Audit             | `AuditDbContext`             | `audit`          |
| ChangeTracker     | `ChangeTrackerDbContext`     | `change_tracker` |
| Comic             | `ComicDbContext`             | `comic`          |
| Comment           | `CommentDbContext`           | `comment`        |
| Config            | `ConfigDbContext`            | `config`         |
| ContactUs         | `ContactUsDbContext`         | `contact`        |
| Discord           | `DiscordDbContext`           | `discord`        |
| Email             | `EmailDbContext`             | `email`          |
| Endato            | `EndatoDbContext`            | `endato`         |
| Favorite          | `FavoriteDbContext`          | `favorite`       |
| FileMetadataStore | `FileMetadataStoreDbContext` | `filestore`      |
| HomeInventory     | `HomeInventoryDbContext`     | `home_inventory` |
| Job               | `JobContext`                 | `job`            |
| Note              | `NoteDbContext`              | `note`           |
| People            | `PeopleDbContext`            | `people`         |
| Rating            | `RatingDbContext`            | `rating`         |
| Reporting         | `ReportingContext`           | `reporting`      |
| ShortUrl          | `ShortUrlDbContext`          | `url`            |
| Sms               | `SmsDbContext`               | `sms`            |
| SmsTwilio         | `TwilioSmsDbContext`         | `sms`            |
| Tag               | `TagDbContext`               | `tag`            |

The menu offers **Run All (Latest)**, which calls every `RunXxxAsync` in registration order, or a context-specific submenu with four actions:

- **Migrate to Latest** — `MigrateLatestAsync<TContext>` (`context.Database.MigrateAsync`).
- **Migrate to Target…** — `MigrateToAsync<TContext>(target)` via `IMigrator.MigrateAsync(target)`. The picker lists every migration with `✓ APPLIED` / `· PENDING` status, plus a
  top "Roll back all" sentinel (passes `"0"` after a confirmation) and a "Cancel" sentinel.
- **View Status** — Table of all defined migrations with applied/pending state.
- **View Current Version** — `GetCurrentVersionAsync<TContext>` returns the last applied migration name, or "No migrations applied yet."

Before each `MigrateAsync`, `EnsureSchemaAsync` runs `CREATE SCHEMA IF NOT EXISTS "schema"` so the migrations history table can be created in its own schema even on a brand-new
database. Contexts are constructed at call time via `Activator.CreateInstance(typeof(TContext), options)` with the connection string pulled from `ConnectionStringProvider` on every
call, so the "Change Connection String" menu option takes effect immediately.

## `ConnectionStringProvider`

Singleton `string?` holder with three members:

- `ConnectionString` (mutable) — current value.
- `IsConfigured` — non-empty check.
- `GetOrThrow()` — throws if no string is set (e.g. you launched without `appsettings.Development.json`).
- `GetMasked()` — returns `(not set)` or the first 40 characters followed by `****` for safe display in the TUI header.

## Configuration

`appsettings.json` only declares `ConnectionString` (empty) and logging defaults. Override locally via `appsettings.Development.json`, environment variables (`ConnectionString=…`),
or the interactive **Change Connection String** menu.

## Related projects

- [`Lyo.Audit.Postgres`](../../Core/Audit/Lyo.Audit.Postgres/README.md)
- [`Lyo.ChangeTracker.Postgres`](../../Core/ChangeTracker/Lyo.ChangeTracker.Postgres/README.md)
- [`Lyo.Comic.Postgres`](../../Features/Comic/Lyo.Comic.Postgres/README.md)
- [`Lyo.Comment.Postgres`](../../Features/Comment/Lyo.Comment.Postgres/README.md)
- [`Lyo.Config.Postgres`](../../Features/Config/Lyo.Config.Postgres/README.md)
- [`Lyo.ContactUs.Postgres`](../../Features/ContactUs/Lyo.ContactUs.Postgres/README.md)
- [`Lyo.Discord.Postgres`](../../Integration/Discord/Lyo.Discord.Postgres/README.md)
- [`Lyo.Email.Postgres`](../../Communication/Email/Lyo.Email.Postgres/README.md)
- [`Lyo.Endato.Postgres`](../../Integration/Endato/Lyo.Endato.Postgres/README.md)
- [`Lyo.Favorite.Postgres`](../../Features/Favorite/Lyo.Favorite.Postgres/README.md)
- [`Lyo.FileMetadataStore.Postgres`](../../Data/FileMetadataStore/Lyo.FileMetadataStore.Postgres/README.md)
- [`Lyo.HomeInventory.Postgres`](../../Features/HomeInventory/Lyo.HomeInventory.Postgres/README.md)
- [`Lyo.Job.Postgres`](../../Integration/Job/Lyo.Job.Postgres/README.md)
- [`Lyo.Note.Postgres`](../../Features/Note/Lyo.Note.Postgres/README.md)
- [`Lyo.People.Postgres`](../../Core/People/Lyo.People.Postgres/README.md)
- [`Lyo.Postgres`](../../Data/Postgres/Lyo.Postgres/README.md)
- [`Lyo.Rating.Postgres`](../../Features/Rating/Lyo.Rating.Postgres/README.md)
- [`Lyo.ShortUrl.Postgres`](../../Features/ShortUrl/Lyo.ShortUrl.Postgres/README.md)
- [`Lyo.Sms.Postgres`](../../Communication/Sms/Lyo.Sms.Postgres/README.md)
- [`Lyo.Sms.Twilio.Postgres`](../../Communication/Sms/Lyo.Sms.Twilio.Postgres/README.md)
- [`Lyo.Tag.Postgres`](../../Features/Tag/Lyo.Tag.Postgres/README.md)
- [`Lyo.Reporting.Postgres`](../../Integration/Reporting/Lyo.Reporting.Postgres/README.md)
