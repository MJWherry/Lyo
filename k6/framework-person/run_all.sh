#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$ROOT_DIR/../.." && pwd)"
OUT_DIR="${OUT_DIR:-$ROOT_DIR/results/$(date +%Y%m%d-%H%M%S)}"
K6_BIN="${K6_BIN:-k6}"
MODE="${MODE:-full}"
CONTINUE_ON_FAILURE="${CONTINUE_ON_FAILURE:-false}"
TEST_FILTER="${TEST_FILTER:-}"

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

declare -a FILTER_KEYWORDS=()
declare -a SELECTED_TESTS=()

print_filter_help() {
  cat <<'EOF'
Usage: ./run_all.sh [keyword ...]

Keywords can be scenario names (substring match) or group aliases:
  - load | stress | spike | soak
  - query | queryproject
  - nonsoak (alias: no-soak, nosoak)
  - all (or matrix)

Examples:
  ./run_all.sh spike
  ./run_all.sh query spike
  TEST_FILTER="query_spike,queryproject_load" ./run_all.sh
EOF
}

normalize_keyword() {
  local keyword="$1"
  keyword="${keyword//[[:space:]]/}"
  echo "${keyword,,}"
}

matches_keyword() {
  local test_name="$1"
  local test_file="$2"
  local keyword="$3"

  case "$keyword" in
    all|matrix)
      return 0
      ;;
    load|stress|spike|soak)
      [[ "$test_name" == *"_$keyword" ]] && return 0
      ;;
    query)
      [[ "$test_name" == query_* ]] && return 0
      ;;
    queryproject|projected|projection)
      [[ "$test_name" == queryproject_* ]] && return 0
      ;;
    nonsoak|no-soak|nosoak)
      [[ "$test_name" != *_soak ]] && return 0
      ;;
  esac

  [[ "$test_name" == *"$keyword"* || "$test_file" == *"$keyword"* ]]
}

if [[ -n "$TEST_FILTER" ]]; then
  IFS=',' read -r -a env_filters <<< "$TEST_FILTER"
  for filter in "${env_filters[@]}"; do
    normalized="$(normalize_keyword "$filter")"
    [[ -n "$normalized" ]] && FILTER_KEYWORDS+=("$normalized")
  done
fi

if [[ "$#" -gt 0 ]]; then
  for arg in "$@"; do
    if [[ "$arg" == "-h" || "$arg" == "--help" ]]; then
      print_filter_help
      exit 0
    fi
    IFS=',' read -r -a cli_filters <<< "$arg"
    for filter in "${cli_filters[@]}"; do
      normalized="$(normalize_keyword "$filter")"
      [[ -n "$normalized" ]] && FILTER_KEYWORDS+=("$normalized")
    done
  done
fi

for test_file in "${MATRIX_TESTS[@]}"; do
  if [[ "${#FILTER_KEYWORDS[@]}" -eq 0 ]]; then
    SELECTED_TESTS+=("$test_file")
    continue
  fi

  test_name="${test_file%.js}"
  include_test="false"
  for keyword in "${FILTER_KEYWORDS[@]}"; do
    if matches_keyword "$test_name" "$test_file" "$keyword"; then
      include_test="true"
      break
    fi
  done

  if [[ "$include_test" == "true" ]]; then
    SELECTED_TESTS+=("$test_file")
  fi
done

if [[ "${#SELECTED_TESTS[@]}" -eq 0 ]]; then
  echo "No tests matched filter(s): ${FILTER_KEYWORDS[*]}"
  echo "Available tests: ${MATRIX_TESTS[*]}"
  echo "Supported groups: load stress spike soak query queryproject nonsoak all"
  exit 1
fi

echo "Running framework-person matrix suite in: $ROOT_DIR"
echo "Results directory: $OUT_DIR"
echo "Mode: $MODE"
echo "Continue on failure: $CONTINUE_ON_FAILURE"
if [[ "${#FILTER_KEYWORDS[@]}" -gt 0 ]]; then
  echo "Filter keywords: ${FILTER_KEYWORDS[*]}"
fi
echo "Selected tests: ${SELECTED_TESTS[*]}"
echo

echo "Building shared packages..."
(cd "$REPO_ROOT/packages/lyo-api-client" && npm install && npm run build)
(cd "$REPO_ROOT/packages/lyo-person-api-client" && npm install && npm run build)
echo "Shared packages built."
echo

for test_file in "${SELECTED_TESTS[@]}"; do
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

if command -v python3 >/dev/null 2>&1; then
  echo "Refreshing k6 benchmark dashboard manifest..."
  python3 "$REPO_ROOT/scripts/benchmarks/build-manifests.py" --k6-only --k6-run-dir "$OUT_DIR" || echo "Warning: benchmark manifest refresh failed."
fi
