# Publishing packages

Lyo libraries are packed as NuGet packages by
[`Lyo.Net/build-nuget.sh`](../Lyo.Net/build-nuget.sh). The script discovers
projects, builds them in dependency order, and emits `.nupkg` (plus `.snupkg`
symbols) into a local output directory. Packaging uses SDK-style `dotnet pack`;
dependencies are derived automatically from `ProjectReference` items, so there
are no hand-maintained `.nuspec` files.

## Shared package metadata

Common package properties are set once in
[`Lyo.Net/Directory.Build.props`](../Lyo.Net/Directory.Build.props), including:

- `PackageLicenseExpression` = `Apache-2.0`
- `RepositoryUrl` / `PackageProjectUrl`
- `IncludeSymbols` = `true`, `SymbolPackageFormat` = `snupkg`
- `GenerateDocumentationFile` = `true`
- `Deterministic` = `true`, plus author/copyright fields

## Usage

```bash
# Build all packages (skips unchanged — see change detection)
Lyo.Net/build-nuget.sh

# Build all packages at a specific version
Lyo.Net/build-nuget.sh -v 2.0.0

# Build a specific package and its Lyo dependencies
Lyo.Net/build-nuget.sh Lyo.Encryption

# Pin a version for a specific package + deps
Lyo.Net/build-nuget.sh -v 1.5.0 Lyo.Encryption

# Force a rebuild even if nothing changed
Lyo.Net/build-nuget.sh -f Lyo.Encryption

# Patterns and multiple targets
Lyo.Net/build-nuget.sh 'Lyo.Encryption.*'
Lyo.Net/build-nuget.sh Lyo.Encryption Lyo.Compression
```

Test, benchmark, and tool projects (`*.Tests`, `*.Benchmarks`, `*TestConsole`,
anything under `Tools/`) are excluded from packing.

## Versioning

- The default version is `1.0.0`; override with `-v/--version`.
- The same `VERSION` is passed to both `dotnet build` and `dotnet pack`, so the
  NuGet package version and the embedded assembly metadata
  (`AssemblyInformationalVersion`, `FileVersion`, etc.) stay aligned.
- When you build a specific package with `-v`, only that package and its Lyo
  dependencies are (re)built.

## Change detection

To avoid re-emitting identical packages, each project's source directory is
fingerprinted with git: the last commit touching the directory, the staged +
unstaged diff against `HEAD`, and the names/contents of untracked files. The
fingerprint and the last packed version are stored in
`$NUGET_OUTPUT_DIR/.build-state` (one line per project, `Name=<hash>:<version>`).

Per project, three outcomes are possible:

| Situation                         | Action                                                                     |
|-----------------------------------|----------------------------------------------------------------------------|
| Source changed (or `--force`)     | Full rebuild (`--no-incremental`) + pack.                                  |
| Source unchanged, version changed | Incremental rebuild (so assembly metadata matches the new version) + pack. |
| Source and version unchanged      | Skipped entirely; no new `.nupkg`.                                         |

Use `-f`/`--force` to bypass change detection and always rebuild.

## Environment variables

| Variable           | Purpose                                                 | Default         |
|--------------------|---------------------------------------------------------|-----------------|
| `NUGET_OUTPUT_DIR` | Output directory for packages and the build-state file. | `~/nuget-local` |
| `BUILD_CONFIG`     | Build configuration.                                    | `Release`       |

## Consuming the output

Add the output directory as a NuGet source, then reference packages as usual:

```bash
dotnet nuget add source "$HOME/nuget-local" --name lyo-local
dotnet add <your-project> package Lyo.Encryption --version 1.0.0
```

> There is no public package feed configured in this repo. Publishing to a remote
> feed (nuget.org, GitHub Packages, an internal feed) is a separate step: push
> the generated `.nupkg`/`.snupkg` with `dotnet nuget push` against your feed and
> credentials.
