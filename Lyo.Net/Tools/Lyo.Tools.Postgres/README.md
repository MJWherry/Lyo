# Lyo.Tools.Postgres

Spectre.Console TUI for running and rolling back EF Core migrations against Lyo Postgres `DbContext`s, plus a couple of Bogus-powered seeders. Use it to bring a fresh local database up to the latest schema or step one schema back to a specific migration.

## Entry point

`Program.cs` builds a default `Microsoft.Extensions.Hosting` host, registers a singleton `ConnectionStringProvider` (initial value from the `ConnectionString` config key, set in `appsettings.Development.json` or environment variables), then hands off to `Menu.RunAsync(scope.ServiceProvider, cts.Token)`. Ctrl+C cancels the host. Three scoped services are wired. `MigrationRunner` drives EF migrations on demand for any registered context. `ComicDbSeeder` fakes comic series, volumes, chapters (with `VolumeId`), pages, character and volume appearances, and tags, and prompts to replace existing rows. `PeopleDbSeeder` fakes person rows with phones, emails, addresses, and related data.

## Main menu

- **Seeds.** Comic Database or People Database. Each seeder prompts for record count (default 20 / 50) and an optional integer random seed. Both skip if the target table already has rows.
- **Migrations.** See below.
- **Change connection string.** Accepts a new value and updates `ConnectionStringProvider` in place. The header shows a masked preview (`first 40 chars + ****`).

## Migrations menu

`MigrationRunner` (see `MigrationRunner.cs`) knows about 21 `DbContext`s, each paired with a Postgres schema name used for `__EFMigrationsHistory`:

| Label | DbContext | Schema |
| ----------------- | ---------------------------- | ---------------- |
| Audit | `AuditDbContext` | `audit` |
| ChangeTracker | `ChangeTrackerDbContext` | `change_tracker` |
| Comic | `ComicDbContext` | `comic` |
| Comment | `CommentDbContext` | `comment` |
| Config | `ConfigDbContext` | `config` |
| ContactUs | `ContactUsDbContext` | `contact` |
| Discord | `DiscordDbContext` | `discord` |
| Email | `EmailDbContext` | `email` |
| Endato | `EndatoDbContext` | `endato` |
| Favorite | `FavoriteDbContext` | `favorite` |
| FileMetadataStore | `FileMetadataStoreDbContext` | `filestore` |
| HomeInventory | `HomeInventoryDbContext` | `home_inventory` |
| Job | `JobContext` | `job` |
| Note | `NoteDbContext` | `note` |
| People | `PeopleDbContext` | `people` |
| Rating | `RatingDbContext` | `rating` |
| Reporting | `ReportingContext` | `reporting` |
| ShortUrl | `ShortUrlDbContext` | `url` |
| Sms | `SmsDbContext` | `sms` |
| SmsTwilio | `TwilioSmsDbContext` | `sms` |
| Tag | `TagDbContext` | `tag` |

The menu offers **Run All (Latest)**, which calls every `RunXxxAsync` in registration order, or a context-specific submenu with four actions:

- **Migrate to Latest.** `MigrateLatestAsync<TContext>` (`context.Database.MigrateAsync`).
- **Migrate to Target….** `MigrateToAsync<TContext>(target)` via `IMigrator.MigrateAsync(target)`. The picker lists every migration with ` APPLIED` / `· PENDING` status, plus a
  top "Roll back all" sentinel (passes `"0"` after a confirmation) and a "Cancel" sentinel.
- **View Status.** Table of all defined migrations with applied/pending state.
- **View Current Version.** `GetCurrentVersionAsync<TContext>` returns the last applied migration name, or "No migrations applied yet."

Before each `MigrateAsync`, `EnsureSchemaAsync` runs `CREATE SCHEMA IF NOT EXISTS "schema"` so the migrations history table can be created in its own schema even on a brand-new
database. Contexts are constructed at call time via `Activator.CreateInstance(typeof(TContext), options)` with the connection string pulled from `ConnectionStringProvider` on every
call, so the "Change Connection String" menu option takes effect immediately.

## `ConnectionStringProvider`

- `ConnectionString` (mutable), current value.
- `IsConfigured`. Non-empty check.
- `GetOrThrow()` throws if no string is set (e.g. you launched without `appsettings.Development.json`).
- `GetMasked()` returns `(not set)` or the first 40 characters followed by `` for safe display in the TUI header.

## Configuration

`appsettings.json` only declares `ConnectionString` (empty) and logging defaults. Override locally via `appsettings.Development.json`, environment variables (`ConnectionString=…`), or the interactive **Change Connection String** menu.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Audit.Postgres` (direct, lyo)
- `Lyo.ChangeTracker.Postgres` (direct, lyo)
- `Lyo.Comic.Postgres` (direct, lyo)
- `Lyo.Comment.Postgres` (direct, lyo)
- `Lyo.Config.Postgres` (direct, lyo)
- `Lyo.ContactUs.Postgres` (direct, lyo)
- `Lyo.Discord.Postgres` (direct, lyo)
- `Lyo.Email.Postgres` (direct, lyo)
- `Lyo.Endato.Postgres` (direct, lyo)
- `Lyo.Favorite.Postgres` (direct, lyo)
- `Lyo.FileMetadataStore.Postgres` (direct, lyo)
- `Lyo.Geolocation.Postgres` (direct, lyo)
- `Lyo.HomeInventory.Postgres` (direct, lyo)
- `Lyo.Job.Postgres` (direct, lyo)
- `Lyo.Note.Postgres` (direct, lyo)
- `Lyo.People.Postgres` (direct, lyo)
- `Lyo.Rating.Postgres` (direct, lyo)
- `Lyo.Reporting.Postgres` (direct, lyo)
- `Lyo.ShortUrl.Postgres` (direct, lyo)
- `Lyo.Sms.Postgres` (direct, lyo)
- `Lyo.Sms.Twilio.Postgres` (direct, lyo)
- `Lyo.Tag.Postgres` (direct, lyo)
- `Bogus` `35.6.5` (direct, third-party)
- `Spectre.Console` `0.57.2` (direct, third-party)
- `Lyo.Api` (transitive, lyo)
- `Lyo.Api.Export` (transitive, lyo)
- `Lyo.Api.Models` (transitive, lyo)
- `Lyo.Audit` (transitive, lyo)
- `Lyo.Cache` (transitive, lyo)
- `Lyo.ChangeTracker` (transitive, lyo)
- `Lyo.Comic` (transitive, lyo)
- `Lyo.Comment` (transitive, lyo)
- `Lyo.Common` (transitive, lyo)
- `Lyo.Compression` (transitive, lyo)
- `Lyo.Config` (transitive, lyo)
- `Lyo.ContactUs` (transitive, lyo)
- `Lyo.ContentThreatScan` (transitive, lyo)
- `Lyo.Csv` (transitive, lyo)
- `Lyo.Csv.Models` (transitive, lyo)
- `Lyo.DataTable.Models` (transitive, lyo)
- `Lyo.DateAndTime` (transitive, lyo)
- `Lyo.Diagnostic` (transitive, lyo)
- `Lyo.Diagnostic.AspNetCore` (transitive, lyo)
- `Lyo.Diff` (transitive, lyo)
- `Lyo.Discord.Models` (transitive, lyo)
- `Lyo.Encryption` (transitive, lyo)
- `Lyo.EntityReference.Models` (transitive, lyo)
- `Lyo.EntityReference.Postgres` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Favorite` (transitive, lyo)
- `Lyo.FileMetadataStore` (transitive, lyo)
- `Lyo.FileStorage` (transitive, lyo)
- `Lyo.Formatter` (transitive, lyo)
- `Lyo.Geolocation` (transitive, lyo)
- `Lyo.Geolocation.Models` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.Health` (transitive, lyo)
- `Lyo.HomeInventory` (transitive, lyo)
- `Lyo.IO.Temp` (transitive, lyo)
- `Lyo.Job.Models` (transitive, lyo)
- `Lyo.KeyStore` (transitive, lyo)
- `Lyo.Lock` (transitive, lyo)
- `Lyo.MessageQueue` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.Note` (transitive, lyo)
- `Lyo.PackageMetadata` (transitive, lyo)
- `Lyo.People.Models` (transitive, lyo)
- `Lyo.Postgres` (transitive, lyo)
- `Lyo.Query` (transitive, lyo)
- `Lyo.Query.Models` (transitive, lyo)
- `Lyo.Rating` (transitive, lyo)
- `Lyo.Reporting.Models` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Schedule.Models` (transitive, lyo)
- `Lyo.Scheduler` (transitive, lyo)
- `Lyo.ShortUrl` (transitive, lyo)
- `Lyo.Sms` (transitive, lyo)
- `Lyo.Sms.Models` (transitive, lyo)
- `Lyo.Sms.Twilio` (transitive, lyo)
- `Lyo.Streams` (transitive, lyo)
- `Lyo.Tag` (transitive, lyo)
- `Lyo.Validation` (transitive, lyo)
- `Lyo.Xlsx` (transitive, lyo)
- `Lyo.Xlsx.Models` (transitive, lyo)
- `BouncyCastle.Cryptography` `2.6.2` (transitive, third-party, netstandard2.0)
- `ClosedXML` `0.105.0` (transitive, third-party)
- `DocumentFormat.OpenXml` `3.1.1` (transitive, third-party)
- `EasyCompressor` `2.1.0` (transitive, third-party)
- `ExcelDataReader` `3.9.0` (transitive, third-party)
- `ExcelDataReader.DataSet` `3.9.0` (transitive, third-party)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` (transitive, third-party)
- `Mapster` `10.0.10` (transitive, third-party)
- `Microsoft.AspNetCore.Authorization` `10.0.5` (transitive, microsoft)
- `Microsoft.AspNetCore.Http.Abstractions` `2.*` (transitive, microsoft)
- `Microsoft.AspNetCore.OpenApi` `10.0.5` (transitive, microsoft)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.EntityFrameworkCore` `10.0.5` (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Analyzers` `10.0.5` (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Caching.Memory` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Configuration` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Http` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.DataAnnotations` `10.0.5` (transitive, microsoft)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` (transitive, third-party)
- `SmartFormat.NET` `3.6.1` (transitive, third-party)
- `System.Buffers` `4.6.1` (transitive, microsoft, netstandard2.0)
- `System.ComponentModel.Annotations` `5.0.0` (transitive, microsoft)
- `System.Diagnostics.DiagnosticSource` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Encoding.CodePages` `10.0.5` (transitive, microsoft)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft, netstandard2.0)
- `Twilio` `7.14.9` (transitive, third-party)