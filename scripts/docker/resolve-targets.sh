#!/usr/bin/env bash
# Resolve a TARGET token list into a deduped list of *.csproj paths (repo-relative, one per line).
#
# Single source of truth shared by the Docker build (which projects to compile), the OCR native-lib
# auto-detection, and the runtime entrypoint (what to run). Keep it dependency-free (find + sort).
#
# Tokens (space- or comma-separated, may be passed as multiple args or one string):
#   benchmarks | bench   -> every Lyo.Net/**/*.Benchmarks.csproj
#   tests      | test    -> every Lyo.Net/**/*.Tests.csproj
#   all                  -> both of the above
#   <Name>               -> the project whose file is <Name>.csproj (e.g. Lyo.Lock.Benchmarks)
#   path/to/Foo.csproj   -> used as-is (must exist)
#
# Exits non-zero if any token resolves to nothing.
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
NET_DIR="$ROOT_DIR/Lyo.Net"

# Collect raw tokens from all args, splitting on commas and whitespace.
raw="$*"
raw="${raw//,/ }"
read -r -a tokens <<<"$raw"

if [[ ${#tokens[@]} -eq 0 ]]; then
  tokens=(all)
fi

# Emit repo-relative paths so the output is identical on the host and inside /src in the image.
rel() { printf '%s\n' "${1#"$ROOT_DIR"/}"; }

find_by_glob() {
  # $1: -name glob; prints repo-relative csproj paths.
  find "$NET_DIR" -name "$1" -print0 | while IFS= read -r -d '' f; do rel "$f"; done
}

results=()
for token in "${tokens[@]}"; do
  [[ -z "$token" ]] && continue
  matches=()
  case "$token" in
    benchmarks|bench)
      mapfile -t matches < <(find_by_glob '*.Benchmarks.csproj') ;;
    tests|test)
      mapfile -t matches < <(find_by_glob '*.Tests.csproj') ;;
    all)
      mapfile -t matches < <(find_by_glob '*.Benchmarks.csproj'; find_by_glob '*.Tests.csproj') ;;
    *.csproj)
      # Literal path (repo-relative or absolute). Verify it exists.
      if [[ "$token" = /* ]]; then
        [[ -f "$token" ]] && matches=("$(rel "$token")")
      else
        [[ -f "$ROOT_DIR/$token" ]] && matches=("$token")
      fi
      ;;
    *)
      mapfile -t matches < <(find_by_glob "$token.csproj") ;;
  esac

  if [[ ${#matches[@]} -eq 0 ]]; then
    echo "resolve-targets: no project matched token '$token'" >&2
    exit 1
  fi
  results+=("${matches[@]}")
done

# Dedupe while keeping things sorted/stable.
printf '%s\n' "${results[@]}" | LC_ALL=C sort -u
