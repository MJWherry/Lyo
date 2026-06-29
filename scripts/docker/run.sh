#!/usr/bin/env bash
# Build and run the target-driven Lyo runner container.
#
# Derives a per-target image tag from the target so each selection caches as its own lean image, then
# `docker compose build`s and `run`s the single `run` service.
#
# Usage:
#   scripts/docker/run.sh [options] <target...>
#
# Targets (space/comma separated; see scripts/docker/resolve-targets.sh):
#   benchmarks | tests | all         a whole group
#   Lyo.Lock.Benchmarks              an exact project name
#   "Lyo.Lock.Benchmarks Lyo.Cache.Tests"   a list
#
# Options:
#   --fg                 run in the foreground (default: detached with -d)
#   --build-only         build the image but don't run
#   --no-docker          skip Testcontainers-backed benchmark classes (NO_DOCKER=1)
#   --filter GLOB        BenchmarkDotNet --filter (BENCH_FILTER)
#   --test-filter EXPR   xUnit --filter (TEST_FILTER)
#   -h | --help          show this help
#
# Examples:
#   scripts/docker/run.sh Lyo.Lock.Benchmarks
#   scripts/docker/run.sh --fg --filter '*Sha256*' Lyo.Hashing.Benchmarks
#   scripts/docker/run.sh tests
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

DETACH=1
BUILD_ONLY=0
export NO_DOCKER="${NO_DOCKER:-0}"
export BENCH_FILTER="${BENCH_FILTER:-*}"
export TEST_FILTER="${TEST_FILTER:-}"
targets=()

usage() { sed -n '2,30p' "$0" | sed 's/^# \{0,1\}//'; }

while [[ $# -gt 0 ]]; do
  case "$1" in
    --fg) DETACH=0; shift ;;
    --build-only) BUILD_ONLY=1; shift ;;
    --no-docker) NO_DOCKER=1; shift ;;
    --filter) BENCH_FILTER="$2"; shift 2 ;;
    --test-filter) TEST_FILTER="$2"; shift 2 ;;
    -h|--help) usage; exit 0 ;;
    --) shift; while [[ $# -gt 0 ]]; do targets+=("$1"); shift; done ;;
    -*) echo "unknown option: $1" >&2; usage >&2; exit 1 ;;
    *) targets+=("$1"); shift ;;
  esac
done

if [[ ${#targets[@]} -eq 0 ]]; then
  echo "error: no target specified" >&2
  usage >&2
  exit 1
fi

# One TARGET string for both the build arg and the runtime env.
export TARGET="${targets[*]}"

# Slug for the image tag: lowercase, non-alphanumeric -> '-', trim/collapse dashes.
slug="$(printf '%s' "$TARGET" \
  | tr '[:upper:]' '[:lower:]' \
  | sed -E 's/[^a-z0-9]+/-/g; s/^-+//; s/-+$//')"
[[ -z "$slug" ]] && slug="all"
export RUN_IMAGE="lyo-runner-${slug}"

echo "==> TARGET=$TARGET"
echo "==> RUN_IMAGE=$RUN_IMAGE"

cd "$ROOT_DIR"
docker compose build run

if [[ "$BUILD_ONLY" == "1" ]]; then
  echo "==> built $RUN_IMAGE (--build-only)"
  exit 0
fi

run_args=(compose run --rm)
if [[ "$DETACH" == "1" ]]; then
  run_args+=(-d)
fi
run_args+=(run)

docker "${run_args[@]}"
