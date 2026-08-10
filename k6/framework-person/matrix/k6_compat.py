"""Make TypeScript package dist importable by k6 (no Node module resolution)."""

from __future__ import annotations

from pathlib import Path

# Bare package name → path relative to the importing dist file.
# From lyo-person-api-client/dist/models/*.js → ../../../lyo-query/dist/index.js
_PERSON_QUERY_REQUESTS = Path(
    "packages/typescript/lyo-person-api-client/dist/models/queryRequests.js"
)
_REWRITE_RULES: list[tuple[Path, tuple[tuple[str, str], ...]]] = [
    (
        _PERSON_QUERY_REQUESTS,
        (
            ('from "lyo-query"', 'from "../../../lyo-query/dist/index.js"'),
            ("from 'lyo-query'", "from '../../../lyo-query/dist/index.js'"),
        ),
    ),
]


def rewrite_bare_imports_for_k6(repo_root: Path) -> list[Path]:
    """Rewrite bare npm imports in package dist to relative paths k6 can load.

    k6 does not implement Node module resolution, so ``from "lyo-query"`` fails.
    Portfolio/Node consumers keep using package names; this only patches dist after
    ``tsc`` for the k6 runner.
    """
    touched: list[Path] = []
    for rel, rules in _REWRITE_RULES:
        path = repo_root / rel
        if not path.is_file():
            continue
        original = path.read_text(encoding="utf-8")
        updated = original
        for old, new in rules:
            updated = updated.replace(old, new)
        if updated != original:
            path.write_text(updated, encoding="utf-8")
            touched.append(path)
    return touched
