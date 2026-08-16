# Publishing packages

Lyo libraries are packed as NuGet packages by
[`scripts/nuget/build_nuget.py`](../scripts/nuget/build_nuget.py). The script discovers projects, builds them in dependency order, and emits `.nupkg` (plus `.snupkg`
symbols) into a local output directory. Packaging uses SDK-style `dotnet pack`; dependencies are derived automatically from `ProjectReference` items, so there are no
hand-maintained `.nuspec` files.

## Shared package metadata

Common package properties are set once in
[`Lyo.Net/Directory.Build.props`](../Lyo.Net/Directory.Build.props), including:

- `PackageLicenseExpression` = `Apache-2.0`
- `RepositoryUrl` / `PackageProjectUrl`
- `IncludeSymbols` = `true`, `SymbolPackageFormat` = `snupkg`
- `GenerateDocumentationFile` = `true`
- `Deterministic` = `true`, plus author/copyright fields
- `PackageReadmeFile` = `README.md` when that file exists next to the csproj (`Directory.Build.targets` packs it)

## Usage

```bash
# Build all packages as 1.0.0-preview (skips unchanged — see change detection)
python3 scripts/nuget/build_nuget.py

# Build all packages at a specific version (still tagged preview: 2.0.0-preview)
python3 scripts/nuget/build_nuget.py -v 2.0.0

# Release / deploy: same version with no prerelease label (1.0.0)
python3 scripts/nuget/build_nuget.py --release
python3 scripts/nuget/build_nuget.py -v 2.0.0 --release

# Build a specific package and its Lyo dependencies
python3 scripts/nuget/build_nuget.py Lyo.Encryption

# Pin a version for a specific package + deps
python3 scripts/nuget/build_nuget.py -v 1.5.0 Lyo.Encryption

# Force a rebuild even if nothing changed
python3 scripts/nuget/build_nuget.py -f Lyo.Encryption

# Only packages whose source changed since the latest tag (or HEAD~1 if untagged)
python3 scripts/nuget/build_nuget.py --release --changed-since

# Same, but since a specific ref
python3 scripts/nuget/build_nuget.py --release --changed-since v1.0.0
python3 scripts/nuget/build_nuget.py --release --changed-since origin/main

# Patterns and multiple targets
python3 scripts/nuget/build_nuget.py 'Lyo.Encryption.*'
python3 scripts/nuget/build_nuget.py Lyo.Encryption Lyo.Compression
```

Test, benchmark, and tool projects (`*.Tests`, `*.Benchmarks`, `*TestConsole`, anything under `Tools/`) are excluded from packing.

## Versioning

- The default version prefix is `1.0.0`; override with `-v/--version`.
- Local packs append the SemVer prerelease label `preview` (e.g. `1.0.0-preview`) so NuGet treats them as prerelease and they cannot be confused with a published release of the
  same number. Pass `--release` when deploying so the version is used as-is (`1.0.0`). If `-v` already includes a prerelease label, it is left unchanged.
- The same package version is passed to both `dotnet build` and `dotnet pack`. `FileVersion` uses the numeric prefix only (`1.0.0`), because Win32 file versions cannot carry a
  prerelease label; `AssemblyInformationalVersion` keeps the full NuGet version.
- When you build a specific package with `-v`, only that package and its Lyo dependencies are (re)built.

## Change detection

To avoid re-emitting identical packages, each project's source directory is fingerprinted with git: the last commit touching the directory, the staged + unstaged diff against
`HEAD`, and the names/contents of untracked files. The fingerprint and the last packed version are stored in
`$NUGET_OUTPUT_DIR/.build-state` (one line per project, `Name=<hash>:<version>`).

Per project, three outcomes are possible:

| Situation                         | Action                                                                     |
|-----------------------------------|----------------------------------------------------------------------------|
| Source changed (or `--force`)     | Full rebuild (`--no-incremental`) + pack.                                  |
| Source unchanged, version changed | Incremental rebuild (so assembly metadata matches the new version) + pack. |
| Source and version unchanged      | Skipped entirely; no new `.nupkg`.                                         |

Use `-f`/`--force` to bypass fingerprint skip and always rebuild. Use `--changed-since` to *select* which projects to consider (git diff since a ref); that is independent of the fingerprint skip.

`--changed-since` with no ref uses the latest git tag, or `HEAD~1` if the repo has no tags. A change to `Lyo.Net/Directory.Build.props`, `Directory.Build.targets`, or `Directory.Packages.props` selects every packable package.

## Publish from GitHub Actions

The [Publish - NuGet](../.github/workflows/publish-nuget.yml) workflow packs with `--release` (no `preview` label) and pushes to nuget.org. Run it from **Actions → Publish - NuGet → Run workflow**:

| Input      | What it does                                                                                          |
|------------|-------------------------------------------------------------------------------------------------------|
| `scope`    | `changed` — packages whose directory changed since `since`. `named` — the `packages` input. `all` — every packable `Lyo.*` library. |
| `packages` | Names or globs when `scope=named` (space-separated), e.g. `Lyo.Encryption` or `Lyo.Encryption.*`.     |
| `version`  | Release version (`1.0.0`). nuget.org versions are immutable — bump this to actually ship code changes. |
| `since`    | Git ref for `scope=changed`. Empty = latest tag, else `HEAD~1`.                                       |
| `dry_run`  | Pack and upload artifacts only; skip nuget.org.                                                       |

Changed packages are packed together with their Lyo `ProjectReference` dependencies at the same version. `--skip-duplicate` ignores nupkgs that already exist on nuget.org at that version.

### One-time repo / nuget.org setup

Do **not** put a long-lived NuGet API key in GitHub secrets. nuget.org is moving to 30-day keys; GitHub Actions should use [Trusted Publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing) (OIDC → a 1-hour key).

1. **nuget.org account** — sign in at [nuget.org](https://www.nuget.org/) (Microsoft account). Open **Account settings** and confirm your email so you can publish.
2. **Trusted Publishing policy** — username menu → **Trusted Publishing** → add a policy:
   - **Repository owner:** `mjwherry` (the GitHub user/org that owns this repo)
   - **Repository:** `Lyo`
   - **Workflow file:** `publish-nuget.yml` (file name only, not `.github/workflows/…`)
   - **Environment:** leave empty (this workflow does not use a GitHub Environment)
   - **Policy owner:** your nuget.org user (or an org you belong to). That owner will own every new `Lyo.*` id this job publishes.
3. **GitHub secret `NUGET_USER`** — repo **Settings → Secrets and variables → Actions → New repository secret**. Value is your **nuget.org profile username**, not your email. The `NuGet/login` action sends it with the OIDC token.
4. **First run** — Actions → **Publish - NuGet** → `dry_run=true`, `scope=named`, one package (e.g. `Lyo.Common`) to confirm pack. Then the same with `dry_run=false`. A new policy on a private repo stays provisionally active for 7 days until the first successful push; after that it locks to this repo.
5. **Version** — local `python3 scripts/nuget/build_nuget.py` produces `1.0.0-preview`. This workflow produces `1.0.0` (or whatever you type). If `1.0.0` is already on nuget.org, either bump `version` or the push is skipped (`--skip-duplicate`).
6. **Optional:** [reserve the `Lyo.` prefix](https://learn.microsoft.com/en-us/nuget/nuget-org/id-prefix-reservation) on nuget.org so only your account can publish `Lyo.*`. Needs a verified domain.

Emergency local push (short-lived API key from nuget.org **API Keys**, glob `Lyo.*`, Push only):

```bash
NUGET_OUTPUT_DIR=./artifacts/nuget python3 scripts/nuget/build_nuget.py --release -v 1.0.0 Lyo.Encryption
dotnet nuget push artifacts/nuget/*.nupkg --api-key "$NUGET_API_KEY" --source https://api.nuget.org/v3/index.json --skip-duplicate
```

## Environment variables

| Variable           | Purpose                                                 | Default         |
|--------------------|---------------------------------------------------------|-----------------|
| `NUGET_OUTPUT_DIR` | Output directory for packages and the build-state file. | `~/nuget-local` |
| `BUILD_CONFIG`     | Build configuration.                                    | `Release`       |

## Consuming the output

Add the output directory as a NuGet source, then reference packages as usual:

```bash
dotnet nuget add source "$HOME/nuget-local" --name lyo-local
dotnet add <your-project> package Lyo.Encryption --version 1.0.0-preview
```

> Local packs go to `~/nuget-local` (or `NUGET_OUTPUT_DIR`) and are versioned `*-preview`. Release packages are published to nuget.org by the
> [Publish - NuGet](../.github/workflows/publish-nuget.yml) workflow — see [Publish from GitHub Actions](#publish-from-github-actions).
