# CI: build, pack, and publish

Lyo is a library monorepo. Consumer apps live in other repos and restore `Lyo.*` from nuget.org (stable or prerelease). There is no in-repo app deploy.

The dispatcher is [CI - Pipeline](../.github/workflows/pipeline.yml). Run it from **Actions → CI - Pipeline → Run workflow**, or merge to `main` for an automatic release.

## Branch model

Daily work is on `dev`. Feature and agent branches PR into `dev`. Promote `dev` to `main` when you want a public stable release.

| Branch | Auto-run? | Manual Run workflow | Default channel / destination |
|--------|-----------|---------------------|-------------------------------|
| `main` | Yes, `on: push` | Yes | `release` → nuget.org |
| `dev` | No | Yes | `preview` → nuget.org |
| any other branch | No | Yes | `preview` → artifacts only (`none`) |

There is no `pull_request` workflow and no auto-build on `dev`. Agents (or you) trigger `workflow_dispatch` when a preview pack is needed. Nothing in this repo requires a review or a green check to merge a PR or push to `main`.

### Version on auto-`main`

A push to `main` has no version text box. nuget.org versions are immutable, so the job picks one:

1. If `HEAD` is tagged `vX.Y.Z`, use `X.Y.Z`.
2. Else patch-bump the latest `v*` tag (`v1.2.3` → `1.2.4`).
3. If there are no tags, the job **fails**. It will not publish `1.0.0`.

Auto-`main` also defaults to `scope=changed` since that last tag. Use Run workflow on `main` if you need `all` or `named`.

`channel=release` is rejected on any branch other than `main`. Preview on nuget.org from `dev` is the pre-release path (`1.2.0-preview.<run_number>`).

## Dispatch inputs

| Input | What it does |
|-------|----------------|
| `scope` | `changed`: packable projects whose directory changed since `since`. `named`: the `packages` input. `all`: every packable `Lyo.*` library. |
| `packages` | Names or globs when `scope=named` (space-separated). |
| `since` | Git ref for `scope=changed`. Empty = last `v*` tag, else `HEAD~1`. |
| `version` | SemVer. Required off `main`. On `main`, empty uses the tag / patch-bump rule. |
| `stages` | `build` compiles `Lyo.slnx` only. `pack` / `build-and-pack` / `pack-and-publish` / `all` compile the selected pack set, then pack without rebuilding. Publish stages do not pack a second time. |
| `destination` | `auto` (follows the branch table) / `none` / `github` / `nuget.org` / `both`. |
| `channel` | `auto` (follows the branch table) / `preview` / `release`. |
| `force` | Rebuild the selected pack set even if fingerprints match. |
| `dry_run` | Pack and upload artifacts. Skip all pushes. |

Pack selection does **not** walk `ProjectReference`s. If you change Encryption, only Encryption is packed. If a Common change breaks Encryption, edit Encryption too (or name both). Shared `Directory.Build.*` / package icon changes still select every packable project.

Each packed nupkg pins Lyo `ProjectReference`s to the dependency's last published version (`.build-state`, then nuget.org, then GitHub Packages), unless that dependency is also in the same pack set.

The slnx Build job runs only for `stages=build`. Pack/publish runs compile the selected libraries in the Pack job, then `dotnet pack --no-build`. Pipeline's nuget.org job pushes those artifacts. It does not pack again.

Job titles look like `Pack CI - dev - v1.2.0-preview.47`. The workflow `name` stays `CI - Pipeline` (the Actions sidebar). The run list uses the same `CI - {branch} - v{version}` shape: typed version plus `-preview.{run_number}` off main, or `preview.{run_number}` / `#{run_number}` when version is left empty. GitHub evaluates that title at dispatch, so it cannot wait for Resolve.

## Feeds

- **nuget.org release** (`1.2.0`). Auto-`main` or dispatch on `main` with `channel=release`. Publishes immediately (no approval environment). The OIDC login job has **no** GitHub Environment so the Trusted Publishing policy can keep Environment empty.
- **nuget.org preview** (`1.2.0-preview.N`). Dispatch on `dev` (default). Apps must use `--prerelease` or an exact version.
- **GitHub Packages.** Optional (`destination=github` or `both`).
- **Actions artifacts.** Always uploaded after a successful pack. The nuget.org / GitHub Packages push fails if every nupkg already exists (409 Conflict) or any package hits a non-duplicate error.

App repos typically need only nuget.org:

```xml
<packageSources>
  <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
</packageSources>
```

```bash
dotnet add package Lyo.Encryption --version 1.2.0
dotnet add package Lyo.Encryption --version 1.2.0-preview.47 --prerelease
```

GitHub Packages (`https://nuget.pkg.github.com/OWNER/index.json`) is optional if you also publish there. Authenticate with `GITHUB_TOKEN` (same user/org) or a PAT with `read:packages`.

## Tests (disabled)

[`_test.yml`](../.github/workflows/_test.yml) and [`scripts/nuget/ci_test.py`](../scripts/nuget/ci_test.py) discover `{Name}.Tests` next to each selected library (or every `*.Tests` project when `scope=all`). Missing tests are skipped. Zero test projects is success.

The test job is **commented out** in `pipeline.yml`. When you enable it:

- Run only on `workflow_dispatch` with `channel=preview`.
- Do not run on auto-`main` or `channel=release`.
- Do not add `on: pull_request` (that would auto-build branches).

Uncomment the `test` job in `pipeline.yml` and drop `if: false`.

## One-time GitHub / nuget.org setup

1. Create a `dev` branch from `main` if it does not exist. Do **not** turn on required reviews, required status checks, or “no direct push” unless you want those later — this pipeline does not depend on them.
2. Keep the nuget.org Trusted Publishing policy **Environment empty**, workflow file `publish-nuget.yml`, repository `mjwherry/Lyo`. Secret `NUGET_USER` is the nuget.org username. Details: [Publishing](publishing.md).
3. Tag the current nuget.org line (`v1.0.0` or whatever is live) so auto-`main` can patch-bump.

## Local pack

```bash
python3 scripts/nuget/build_nuget.py -v 1.2.0 Lyo.Encryption
python3 scripts/nuget/ci_pack.py --scope named --packages Lyo.Encryption --version 1.2.0 --channel preview
python3 scripts/nuget/ci_pack.py --scope named --packages Lyo.Encryption --version 1.2.0 --channel preview --compile-only
python3 scripts/nuget/ci_pack.py --scope named --packages Lyo.Encryption --version 1.2.0 --channel preview --pack-only
```

See [Publishing](publishing.md) for fingerprint skip, `--changed-since`, and `--release`.
