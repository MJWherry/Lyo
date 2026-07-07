#!/usr/bin/env bash
# Run the Lyo BenchmarkDotNet suites in Release and regenerate the dashboard manifests.
#
# Usage:
#   scripts/benchmarks/run-dotnet-benchmarks.sh [--no-docker] [--filter '*'] [category ...]
#
#   category       One or more of: encryption compression hashing cache query csv xlsx lock.
#                  When omitted, every suite is run.
#   --no-docker    Skip suites whose benchmarks require Docker (Testcontainers): the Redis/Postgres
#                  classes are excluded via a BenchmarkDotNet --anyCategories-style filter, and the
#                  Docker-only suites still run their in-process classes.
#   --filter GLOB  Extra BenchmarkDotNet filter passed through to every suite (default '*').
#
# Each suite's in-process LyoBenchmarkExporter writes <name>.lyobench.json into BenchmarkDotNet.Artifacts
# next to its project. After each suite finishes, build-manifests.py exports that category immediately
# (archives a history snapshot, computes deltas vs the prior run, and updates data/<name>.{json,js}).
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
NET_DIR="$ROOT_DIR/Lyo.Net"

NO_DOCKER=0
FILTER='*'
CATEGORIES=()

while [[ $# -gt 0 ]]; do
  case "$1" in
    --no-docker) NO_DOCKER=1; shift ;;
    --filter) FILTER="$2"; shift 2 ;;
    -h|--help) sed -n '2,20p' "$0"; exit 0 ;;
    *) CATEGORIES+=("$1"); shift ;;
  esac
done

if [[ ${#CATEGORIES[@]} -eq 0 ]]; then
  CATEGORIES=(encryption compression hashing cache query csv xlsx lock)
fi

# Map category -> benchmark project path (relative to Lyo.Net).
declare -A PROJECTS=(
  [encryption]="Security/Encryption/Lyo.Encryption.Benchmarks/Lyo.Encryption.Benchmarks.csproj"
  [compression]="Data/Compression/Lyo.Compression.Benchmarks/Lyo.Compression.Benchmarks.csproj"
  [hashing]="Security/Hashing/Lyo.Hashing.Benchmarks/Lyo.Hashing.Benchmarks.csproj"
  [cache]="Core/Cache/Lyo.Cache.Benchmarks/Lyo.Cache.Benchmarks.csproj"
  [query]="Data/Query/Lyo.Query.Benchmarks/Lyo.Query.Benchmarks.csproj"
  [csv]="Data/Csv/Lyo.Csv.Benchmarks/Lyo.Csv.Benchmarks.csproj"
  [xlsx]="Data/Xlsx/Lyo.Xlsx.Benchmarks/Lyo.Xlsx.Benchmarks.csproj"
  [lock]="Core/Lock/Lyo.Lock.Benchmarks/Lyo.Lock.Benchmarks.csproj"
)

# In-process (non-Docker) class globs per suite. When --no-docker is set we pass these as positive
# --filter globs instead of running everything, because BenchmarkDotNet has no name-based exclusion
# flag (only positive --filter / --anyCategories). Suites not listed here have no Docker classes.
declare -A NODOCKER_FILTERS=(
  [cache]="*PayloadCacheBenchmarks* *CacheComparisonBenchmarks*"
  [query]="*WhereClauseBenchmarks* *SortBenchmarks* *ProjectionBenchmarks* *MappingBenchmarks*"
  [lock]="*LocalLockBenchmarks*"
)

for category in "${CATEGORIES[@]}"; do
  project="${PROJECTS[$category]:-}"
  if [[ -z "$project" ]]; then
    echo "Unknown category: $category" >&2
    exit 1
  fi

  echo "==> Running $category benchmarks"
  # Pin the artifacts path next to the project so build-manifests.py can find <name>.lyobench.json
  # regardless of the directory this script is invoked from (BenchmarkDotNet otherwise uses the cwd).
  # --join produces ONE joined Summary across every benchmark class in the suite, so the exporter
  # writes a single <name>.lyobench.json containing all groups/comparison. Without it BenchmarkSwitcher
  # emits one Summary per class and the exporter (which writes a fixed <name>.lyobench.json) keeps only
  # the last class — the cause of past "only one group showed up" reports.
  artifacts="$NET_DIR/$(dirname "$project")/BenchmarkDotNet.Artifacts"
  # BENCH_NO_BUILD=1 (set by the Docker runner) reuses prebuilt output instead of recompiling/restoring.
  run_opts=(run -c Release --project "$NET_DIR/$project")
  if [[ "${BENCH_NO_BUILD:-0}" == "1" ]]; then
    run_opts+=(--no-build --no-restore)
  fi
  args=("${run_opts[@]}" -- --join --artifacts "$artifacts" --filter)
  if [[ "$NO_DOCKER" -eq 1 && -n "${NODOCKER_FILTERS[$category]:-}" ]]; then
    # Positive include of just the in-process classes (BDN has no exclusion flag).
    # shellcheck disable=SC2206
    args+=(${NODOCKER_FILTERS[$category]})
  else
    args+=("$FILTER")
  fi
  dotnet "${args[@]}"
  echo "==> Exporting $category dashboard data"
  python3 "$ROOT_DIR/scripts/benchmarks/build-manifests.py" --"${category}-only"
done

echo "Done. Reload docs/benchmarks/index.html to view results."
