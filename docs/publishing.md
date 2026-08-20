# Publishing packages

Lyo libraries are packed as NuGet packages by [`scripts/nuget/build_nuget.py`](../scripts/nuget/build_nuget.py). The script discovers projects and emits `.nupkg` (plus `.snupkg` symbols) into a local output directory. It packs **only the projects you name or that changed**. It does not walk `ProjectReference`s up or down. Packaging uses SDK-style `dotnet pack`. Lyo `ProjectReference`s become package dependencies pinned to the dependency's last published version, unless that project is also in the same pack set. There are no hand-maintained `.nuspec` files.

GitHub Actions: see [CI](ci.md).

## Shared package metadata

Common package properties are set once in [`Lyo.Net/Directory.Build.props`](../Lyo.Net/Directory.Build.props), including:

- `PackageLicenseExpression` = `Apache-2.0`
- `RepositoryUrl` / `PackageProjectUrl`
- `IncludeSymbols` = `true`, `SymbolPackageFormat` = `snupkg`
- `GenerateDocumentationFile` = `true`
- `Deterministic` = `true`, plus author/copyright fields
- `PackageReadmeFile` = `README.md` when that file exists next to the csproj (`Directory.Build.targets` packs it)
- `PackageIcon` = `icon.png` from [`Lyo.Net/assets/icon.png`](../Lyo.Net/assets/icon.png) (nuget.org does not accept SVG; source is `assets/icon.svg`)

## Usage

```bash
# Build all packages as 1.0.0-preview (skips unchanged; see change detection)
python3 scripts/nuget/build_nuget.py

# Build all packages at a specific version (still tagged preview: 2.0.0-preview)
python3 scripts/nuget/build_nuget.py -v 2.0.0

# Release / deploy: same version with no prerelease label (1.0.0)
python3 scripts/nuget/build_nuget.py --release
python3 scripts/nuget/build_nuget.py -v 2.0.0 --release

# Build a specific package only (does not pack Common because Encryption references it)
python3 scripts/nuget/build_nuget.py Lyo.Encryption

# Pin a version for that package only
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

- The default version prefix is `1.0.0`. Override with `-v/--version`.
- Local packs append the SemVer prerelease label `preview` (for example `1.0.0-preview`) so NuGet treats them as prerelease and they cannot be confused with a published release of the same number. Pass `--release` when deploying so the version is used as-is (`1.0.0`). If `-v` already includes a prerelease label, it is left unchanged.
- The same package version is passed to both `dotnet build` and `dotnet pack`. `FileVersion` uses the numeric prefix only (`1.0.0`), because Win32 file versions cannot carry a prerelease label. `AssemblyInformationalVersion` keeps the full NuGet version.
- When you build a specific package with `-v`, only that package is packed. Upstream Lyo references in the nupkg are pinned to each dependency's last published version (`.build-state`, then nuget.org, then GitHub Packages). If a Common change breaks Encryption, edit Encryption too so it is in the pack set.

## Change detection

To avoid re-emitting identical packages, each project's source directory is fingerprinted with git: the last commit touching the directory, the staged + unstaged diff against `HEAD`, and the names/contents of untracked files. The fingerprint and the last packed version are stored in `$NUGET_OUTPUT_DIR/.build-state` (one line per project, `Name=<hash>:<version>`).

Per project, three outcomes are possible:

| Situation                         | Action                                                                     |
|-----------------------------------|----------------------------------------------------------------------------|
| Source changed (or `--force`)     | Full rebuild (`--no-incremental`) + pack.                                  |
| Source unchanged, version changed | Incremental rebuild (so assembly metadata matches the new version) + pack. |
| Source and version unchanged      | Skipped entirely. No new `.nupkg`.                                         |

Use `-f`/`--force` to bypass fingerprint skip and always rebuild. Use `--changed-since` to *select* which projects to consider (git diff since a ref). That is independent of the fingerprint skip.

`--changed-since` with no ref uses the latest git tag, or `HEAD~1` if the repo has no tags. A change to `Lyo.Net/Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`, or `Lyo.Net/assets/icon.{png,svg}` selects every packable package.

## Publish from GitHub Actions

Use **[CI - Pipeline](ci.md)** (`pipeline.yml`) for everyday pack and publish:

- Push to `main`. Automatic stable release to nuget.org (`scope=changed`, version from the `v*` tag or a patch bump).
- Run workflow on `dev`. nuget.org **preview** (`1.2.0-preview.<run>`).
- Run workflow on a feature branch. Artifacts only unless you set `destination`.

The [Publish - NuGet](../.github/workflows/publish-nuget.yml) workflow is the OIDC push job (filename locked for Trusted Publishing) plus an emergency pack+push dispatch. `--skip-duplicate` ignores nupkgs that already exist on nuget.org at that version.

### One-time repo / nuget.org setup

Do **not** put a long-lived NuGet API key in GitHub secrets. nuget.org is moving to 30-day keys. GitHub Actions should use [Trusted Publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing) (OIDC → a 1-hour key).

1. **nuget.org account.** Sign in at [nuget.org](https://www.nuget.org/) (Microsoft account). Open **Account settings** and confirm your email so you can publish.
2. **Trusted Publishing policy.** Username menu → **Trusted Publishing** → add a policy:
   - **Repository owner:** `mjwherry` (the GitHub user/org that owns this repo)
   - **Repository:** `Lyo`
   - **Workflow file:** `publish-nuget.yml` (file name only, not `.github/workflows/…`)
   - **Environment:** leave empty (the OIDC login job has no GitHub Environment; publishes are not gated on reviewers)
   - **Policy owner:** your nuget.org user (or an org you belong to). That owner will own every new `Lyo.*` id this job publishes.
3. **GitHub secret `NUGET_USER`.** Repo **Settings → Secrets and variables → Actions → New repository secret**. Value is your **nuget.org profile username**, not your email. The `NuGet/login` action sends it with the OIDC token.
4. **First run.** Actions → **CI - Pipeline** on `dev`, `dry_run=true`, `scope=named`, one package (e.g. `Lyo.Common`) to confirm pack. Then the same with `dry_run=false` to push a nuget.org prerelease. A new policy on a private repo stays provisionally active for 7 days until the first successful push. After that it locks to this repo.
5. **Version.** Local `python3 scripts/nuget/build_nuget.py` produces `1.0.0-preview`. Pipeline preview produces `1.0.0-preview.<run>`. Auto-`main` produces a stable `X.Y.Z`. If that version is already on nuget.org, the push is skipped (`--skip-duplicate`).
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

> Local packs go to `~/nuget-local` (or `NUGET_OUTPUT_DIR`) and are versioned `*-preview`. nuget.org publishes go through [CI - Pipeline](ci.md).
