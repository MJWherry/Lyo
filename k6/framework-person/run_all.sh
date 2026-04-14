#!/usr/bin/env bash
set -uo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
OUT_DIR="${OUT_DIR:-$ROOT_DIR/results/$(date +%Y%m%d-%H%M%S)}"
K6_BIN="${K6_BIN:-k6}"

BASE_URL="${BASE_URL:-http://localhost:5251}"
ENDPOINT_PATH="${ENDPOINT_PATH:-/person/query}"
TOKEN="${TOKEN:-}"

mkdir -p "$OUT_DIR"

declare -a TESTS=(
  "01_load_mixed_queries.js"
  "02_stress_heavy_includes.js"
  "03_spike_select_fields.js"
  "04_soak_mixed_leak_watch.js"
  "05_load_query_subquery.js"
)

echo "Running framework-person suite in: $ROOT_DIR"
echo "Results directory: $OUT_DIR"
echo

for test_file in "${TESTS[@]}"; do
  test_name="${test_file%.js}"
  summary_file="$OUT_DIR/${test_name}.summary.json"
  log_file="$OUT_DIR/${test_name}.log"
  test_path="$ROOT_DIR/scenarios/$test_file"

  echo "=== Running $test_file ==="
  cmd=(
    "$K6_BIN" run
    "-e" "BASE_URL=$BASE_URL"
    "-e" "ENDPOINT_PATH=$ENDPOINT_PATH"
    "--summary-export" "$summary_file"
    "$test_path"
  )

  if [[ -n "$TOKEN" ]]; then
    cmd+=("-e" "TOKEN=$TOKEN")
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
  fi

  echo "Saved summary: $summary_file"
  echo "Saved log:     $log_file"
  echo
done

echo "All framework-person tests completed."
echo "Results: $OUT_DIR"
