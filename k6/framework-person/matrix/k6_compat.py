"""Make TypeScript package dist importable by k6 (no Node module resolution)."""

from __future__ import annotations

import os
import re
from pathlib import Path

# Bare package name → dist entry k6 should load.
_BARE_PACKAGES: dict[str, Path] = {
    "lyo-query": Path("packages/typescript/lyo-query/dist/index.js"),
    "lyo-api-client": Path("packages/typescript/lyo-api-client/dist/index.js"),
}

_SCAN_ROOTS = (
    Path("packages/typescript/lyo-person-api-client/dist"),
    Path("packages/typescript/lyo-api-client/dist"),
)

_BARE_FROM = re.compile(r"""from\s+(['"])(lyo-[a-z0-9-]+)\1""")


def _relative_import(from_file: Path, target: Path) -> str:
    rel = os.path.relpath(target, start=from_file.parent).replace("\\", "/")
    return rel if rel.startswith(".") else f"./{rel}"


def rewrite_bare_imports_for_k6(repo_root: Path) -> list[Path]:
    """Rewrite bare npm imports in package dist to relative paths k6 can load.

    k6 does not implement Node module resolution, so ``from "lyo-query"`` and
    ``from "lyo-api-client"`` fail. Portfolio/Node consumers keep using package
    names; this only patches dist after ``tsc`` for the k6 runner.
    """
    touched: list[Path] = []
    for scan_rel in _SCAN_ROOTS:
        scan = repo_root / scan_rel
        if not scan.is_dir():
            continue
        for path in sorted(scan.rglob("*.js")):
            original = path.read_text(encoding="utf-8")
            updated = original
            for match in _BARE_FROM.finditer(original):
                quote, pkg = match.group(1), match.group(2)
                dest = _BARE_PACKAGES.get(pkg)
                if dest is None:
                    continue
                target = repo_root / dest
                rel = _relative_import(path, target)
                updated = updated.replace(f"from {quote}{pkg}{quote}", f"from {quote}{rel}{quote}")
            if updated != original:
                path.write_text(updated, encoding="utf-8")
                touched.append(path)
    return touched
