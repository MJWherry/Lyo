#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$ROOT_DIR/../.." && pwd)"
OUT_DIR="${OUT_DIR:-$ROOT_DIR/results/$(date +%Y%m%d-%H%M%S)}"
K6_BIN="${K6_BIN:-k6}"
MODE="${MODE:-full}"
CONTINUE_ON_FAILURE="${CONTINUE_ON_FAILURE:-false}"

BASE_URL="${BASE_URL:-http://localhost:5251}"
ENDPOINT_PATH="${ENDPOINT_PATH:-/person/query}"
QUERY_PROJECT_PATH="${QUERY_PROJECT_PATH:-/person/QueryProject}"
TOKEN="${TOKEN:-}"

mkdir -p "$OUT_DIR"

declare -a MATRIX_TESTS=(
  "query_load.js"
  "query_stress.js"
  "query_spike.js"
  "query_soak.js"
  "queryproject_load.js"
  "queryproject_stress.js"
  "queryproject_spike.js"
  "queryproject_soak.js"
)

echo "Running framework-person matrix suite in: $ROOT_DIR"
echo "Results directory: $OUT_DIR"
echo "Mode: $MODE"
echo "Continue on failure: $CONTINUE_ON_FAILURE"
echo

echo "Building shared packages..."
(cd "$REPO_ROOT/packages/lyo-api-client" && npm install && npm run build)
(cd "$REPO_ROOT/packages/lyo-person-api-client" && npm install && npm run build)
echo "Shared packages built."
echo

for test_file in "${MATRIX_TESTS[@]}"; do
  test_name="${test_file%.js}"
  summary_file="$OUT_DIR/${test_name}.summary.json"
  log_file="$OUT_DIR/${test_name}.log"
  test_path="$ROOT_DIR/scenarios/$test_file"

  echo "=== Running $test_file ==="
  cmd=(
    "$K6_BIN" run
    "-e" "BASE_URL=$BASE_URL"
    "-e" "ENDPOINT_PATH=$ENDPOINT_PATH"
    "-e" "QUERY_PROJECT_PATH=$QUERY_PROJECT_PATH"
    "--summary-export" "$summary_file"
    "$test_path"
  )

  if [[ -n "$TOKEN" ]]; then
    cmd+=("-e" "TOKEN=$TOKEN")
  fi

  if [[ "$MODE" == "smoke" ]]; then
    cmd+=("--vus" "1" "--iterations" "1")
  fi

  if [[ -n "${EXTRA_K6_ARGS:-}" ]]; then
    # shellcheck disable=SC2206
    extra_args=( ${EXTRA_K6_ARGS} )
    cmd+=("${extra_args[@]}")
  fi

  set +e
  "${cmd[@]}" 2>&1 | tee "$log_file"
  exit_code="${PIPESTATUS[0]}"
  set -e

  if [[ "$exit_code" -ne 0 ]]; then
    echo "Test failed: $test_file (exit $exit_code)"
    echo "See: $log_file"
    if [[ "$CONTINUE_ON_FAILURE" != "true" ]]; then
      echo "Stopping because CONTINUE_ON_FAILURE is not true."
      exit "$exit_code"
    fi
    echo "Continuing because CONTINUE_ON_FAILURE=true."
  fi

  echo "Saved summary: $summary_file"
  echo "Saved log:     $log_file"
  echo
done

echo "All framework-person matrix tests completed."
echo "Results: $OUT_DIR"
