# Phase 0 inventory (2026-09-07)

Recorded after fixing analysis-script `sys.path` so commands run from the repo root.

- slnx vs disk: 365 csproj = 365 slnx Path entries (zero orphans).
- clone_report: type-1 6.76% redundant (17,068 lines); type-2 23.72% (59,852 lines). Snapshot: `clone_report.json`.
- dep_closure: whole-solution snapshot refreshed in `dep_closure_pre.json`.
- `ArgumentNullException.ThrowIfNull` outside tests/`ArgumentHelpers.cs`: 40 files / 112 calls.
- Non-Models libraries without a sibling `*.Tests` project named after the package: 139 (includes hosts, Web.Components, codecs — not a kill list).
