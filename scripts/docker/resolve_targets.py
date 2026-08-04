#!/usr/bin/env python3
"""Resolve a TARGET token list into deduped *.csproj paths (repo-relative, one per line).

Shared by the Docker build (which projects to compile), OCR native-lib auto-detection, and the
runtime entrypoint. Stdlib only.

Tokens (space- or comma-separated; multiple args or one string):
  benchmarks | bench   -> every Lyo.Net/**/*.Benchmarks.csproj
  tests      | test    -> every Lyo.Net/**/*.Tests.csproj
  all                  -> both of the above
  <Name>               -> <Name>.csproj (e.g. Lyo.Lock.Benchmarks)
  <glob>               -> matched against project file names (quote globs)
  path/to/Foo.csproj   -> used as-is (must exist)

Exits non-zero if any token resolves to nothing.
"""

from __future__ import annotations

import argparse
import fnmatch
import sys
from pathlib import Path

_SCRIPTS = Path(__file__).resolve().parents[1]
if str(_SCRIPTS) not in sys.path:
    sys.path.insert(0, str(_SCRIPTS))

from lyo_tooling.dotnet import REPO_ROOT  # noqa: E402

NET_DIR = REPO_ROOT / "Lyo.Net"


def _rel(path: Path) -> str:
    return str(path.resolve().relative_to(REPO_ROOT)).replace("\\", "/")


def _all_runnable() -> list[Path]:
    return sorted(NET_DIR.rglob("*.Benchmarks.csproj")) + sorted(NET_DIR.rglob("*.Tests.csproj"))


def _find_by_name_glob(pattern: str) -> list[Path]:
    """Match ``pattern`` against project file names under Lyo.Net."""
    return sorted(p for p in NET_DIR.rglob("*.csproj") if fnmatch.fnmatch(p.name, pattern))


def resolve_token(token: str) -> list[Path]:
    token = token.strip()
    if not token:
        return []

    if token in ("benchmarks", "bench"):
        return sorted(NET_DIR.rglob("*.Benchmarks.csproj"))
    if token in ("tests", "test"):
        return sorted(NET_DIR.rglob("*.Tests.csproj"))
    if token == "all":
        return _all_runnable()

    if any(ch in token for ch in "*?["):
        pat = token if token.endswith(".csproj") else f"{token}.csproj"
        matches = [p for p in _find_by_name_glob(pat) if p.name.endswith((".Tests.csproj", ".Benchmarks.csproj"))]
        return matches

    if token.endswith(".csproj"):
        path = Path(token)
        if path.is_absolute():
            return [path] if path.is_file() else []
        candidate = REPO_ROOT / token
        return [candidate] if candidate.is_file() else []

    return _find_by_name_glob(f"{token}.csproj")


def resolve_targets(*raw_tokens: str) -> list[str]:
    tokens: list[str] = []
    for raw in raw_tokens:
        for part in raw.replace(",", " ").split():
            if part:
                tokens.append(part)
    if not tokens:
        tokens = ["all"]

    results: list[Path] = []
    for token in tokens:
        matches = resolve_token(token)
        if not matches:
            raise SystemExit(f"resolve-targets: no project matched token '{token}'")
        results.extend(matches)

    # Dedupe, stable sort by repo-relative path.
    seen: set[str] = set()
    out: list[str] = []
    for path in sorted(results, key=lambda p: _rel(p)):
        rel = _rel(path)
        if rel not in seen:
            seen.add(rel)
            out.append(rel)
    return out


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("tokens", nargs="*", help="Target tokens (default: all)")
    args = parser.parse_args(argv)
    for rel in resolve_targets(*args.tokens):
        print(rel)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
