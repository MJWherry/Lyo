# Migration policy (Story stores)

Story persistence ships as NuGet packages (`Lyo.Story.Sqlite`, `Lyo.Story.Postgres`). Game packages live in the Lyo-Game repo. Player machines and servers apply the EF Core migrations that ship inside those packages, so a migration that has been released is part of a user's database forever.

## Rules

- Never edit, rename, reorder, or delete a migration that has shipped in a published package. Fix mistakes with a new migration.
- Every schema change gets a migration in both the SQLite and the Postgres package of that area, in the same change, with the same name.
- Migrations are additive where possible. A column that becomes required gets a default or a backfill in the migration; do not rely on the app to repair rows.
- Do not hand-edit `*ModelSnapshot.cs`. Regenerate with `dotnet ef migrations add`.
- A migration that drops or rewrites data needs a note in the release changelog.

## Previous-version fixtures

Game upgrade fixtures live in the Lyo-Game repo at `Lyo.Game.Net/Plugins/Game/Runtime/Lyo.Game.Runtime.Tests/Fixtures/v{N}/{area}.sql`. Story keeps the same rule: SQL scripts, not `.db` files, one script per SQLite area, where `v{N}` is the released package version.

At each release:

1. For every SQLite area package, script the schema as shipped: `dotnet ef migrations script 0 <LastMigrationInRelease> --project <Area>.Sqlite --startup-project <Area>.Sqlite -o <fixture path>`.
2. Commit the scripts under a new `Fixtures/v{N}/` folder. Never regenerate an older folder.
3. Point the upgrade test at every fixture folder, so each earlier release keeps upgrading to the current schema.

## Postgres

Postgres stores run the same migrations through `EnableAutoMigrations` or a deploy step. Servers should back up before upgrading and run migrations from one instance only.
