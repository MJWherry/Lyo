#!/usr/bin/env bash
# Target-driven entrypoint for the Lyo runner container.
#
# The projects are prebuilt into the image (multi-stage Dockerfile keyed off the same TARGET), so this
# resolves TARGET and runs the baked binaries, dispatching by project suffix:
#   *.Benchmarks -> BenchmarkDotNet via scripts/benchmarks/run-dotnet-benchmarks.sh (BENCH_NO_BUILD=1)
#   *.Tests      -> dotnet test --no-build --no-restore
#
# Environment (set per-run in docker-compose.yml):
#   TARGET        token list (groups/name/list) - must match the image's build-time TARGET
#   BENCH_FILTER  BenchmarkDotNet --filter glob (default '*')
#   NO_DOCKER     1 to skip Testcontainers-backed benchmark classes
#   TEST_FILTER   optional xUnit --filter expression
#   HOST_UID/HOST_GID  ownership handed to the docs/benchmarks/data manifests on exit
set -euo pipefail

REPO_ROOT="/src"
cd "$REPO_ROOT"

TARGET="${TARGET:-all}"

# The only path mounted out to the host; build-manifests.py (bench) writes here as root, so hand
# ownership back to the host user on exit (success or failure).
DATA_DIR="$REPO_ROOT/docs/benchmarks/data"
chown_data() {
  if [[ -d "$DATA_DIR" ]]; then
    chown -R "${HOST_UID:-1000}:${HOST_GID:-1000}" "$DATA_DIR" 2>/dev/null || true
  fi
}
trap chown_data EXIT

# Resolve the target to the same csproj set the image compiled.
mapfile -t projects < <(bash "$REPO_ROOT/scripts/docker/resolve-targets.sh" "$TARGET")
if [[ ${#projects[@]} -eq 0 ]]; then
  echo "TARGET '$TARGET' resolved to no projects" >&2
  exit 2
fi

categories=()   # benchmark categories derived from *.Benchmarks project names
test_projects=()
for proj in "${projects[@]}"; do
  base="$(basename "$proj" .csproj)"
  case "$base" in
    *.Benchmarks)
      # Lyo.<X>.Benchmarks -> lowercase <X>, matching run-dotnet-benchmarks.sh / build-manifests.py.
      name="${base#Lyo.}"
      name="${name%.Benchmarks}"
      categories+=("$(printf '%s' "$name" | tr '[:upper:]' '[:lower:]')")
      ;;
    *.Tests)
      test_projects+=("$proj")
      ;;
    *)
      echo "==> skipping $proj (not a *.Benchmarks or *.Tests project)" >&2
      ;;
  esac
done

run_benchmarks() {
  local args=()
  if [[ "${NO_DOCKER:-0}" == "1" ]]; then
    args+=(--no-docker)
  fi
  args+=(--filter "${BENCH_FILTER:-*}")
  args+=("${categories[@]}")
  echo "==> bench: run-dotnet-benchmarks.sh ${args[*]}"
  # BENCH_NO_BUILD makes each suite reuse the image's prebuilt output (no recompile, no restore).
  BENCH_NO_BUILD=1 bash "$REPO_ROOT/scripts/benchmarks/run-dotnet-benchmarks.sh" "${args[@]}"
}

run_tests() {
  local common=(test -c Release --no-build --no-restore)
  if [[ -n "${TEST_FILTER:-}" ]]; then
    common+=(--filter "${TEST_FILTER}")
  fi
  echo "==> test: running ${#test_projects[@]} xUnit test project(s)"
  local failed=()
  for proj in "${test_projects[@]}"; do
    echo "==> dotnet ${common[*]} ${proj}"
    if ! dotnet "${common[@]}" "$proj"; then
      failed+=("$proj")
    fi
  done
  if [[ ${#failed[@]} -gt 0 ]]; then
    echo "==> ${#failed[@]} test project(s) failed:" >&2
    printf '   %s\n' "${failed[@]}" >&2
    return 1
  fi
  echo "==> all test projects passed"
}

rc=0
if [[ ${#categories[@]} -gt 0 ]]; then
  run_benchmarks
fi
if [[ ${#test_projects[@]} -gt 0 ]]; then
  run_tests || rc=1
fi
exit "$rc"
