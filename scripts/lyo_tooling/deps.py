"""Per-project dependency rows for docs.json / README / portfolio.

Reuses the same ProjectReference + PackageReference model as scripts/gen_graph.py
(the Lyo Project Graph). Tags mirror the graph Depends panel:
  direct | transitive
  lyo | microsoft | third-party
  plus TFM labels (net10.0, netstandard2.0, …) when the PackageReference is gated.
"""

from __future__ import annotations

import sys
from pathlib import Path

_SCRIPTS = Path(__file__).resolve().parents[1]
if str(_SCRIPTS) not in sys.path:
    sys.path.insert(0, str(_SCRIPTS))

from gen_graph import (  # noqa: E402
    build_graph_data,
    is_test_like,
    load_slnx_projects,
    parse_refs,
)
from lyo_tooling.dotnet import load_central_package_versions  # noqa: E402


def is_microsoft_package(name: str) -> bool:
    n = name.casefold()
    return n.startswith("microsoft.") or n.startswith("system.")


def _fw_tag(framework: str) -> str | None:
    fw = (framework or "").strip()
    if not fw or fw == "all":
        return None
    return fw


def _pkg_rows_from_groups(
    groups: list[dict],
    *,
    scope: str,
    skip_names: set[str] | None = None,
) -> list[dict]:
    """Collapse framework-grouped packages into tagged dependency rows."""
    skip = skip_names or set()
    # name -> accumulated row
    by_name: dict[str, dict] = {}
    for group in groups or []:
        fw_tag = _fw_tag(group.get("framework") or "all")
        for pkg in group.get("packages") or []:
            name = pkg.get("name") or ""
            if not name or name in skip:
                continue
            versions = [v for v in (pkg.get("versions") or []) if v]
            vendor = "microsoft" if is_microsoft_package(name) else "third-party"
            tags = [scope, vendor]
            if fw_tag:
                tags.append(fw_tag)
            existing = by_name.get(name)
            if existing is None:
                by_name[name] = {
                    "name": name,
                    "kind": "package",
                    "version": versions[0] if versions else None,
                    "tags": tags,
                }
                continue
            # Merge tags / keep a version if we learn one
            tag_set = list(dict.fromkeys([*(existing.get("tags") or []), *tags]))
            existing["tags"] = tag_set
            if not existing.get("version") and versions:
                existing["version"] = versions[0]
    return list(by_name.values())


def dependency_rows_for_node(node: dict) -> list[dict]:
    """Build sorted dependency rows for one graph project node."""
    rows: list[dict] = []
    direct_lyo = [r for r in (node.get("refs") or []) if r]
    direct_lyo_set = set(direct_lyo)

    for name in direct_lyo:
        rows.append({"name": name, "kind": "lyo", "tags": ["direct", "lyo"]})

    for name in node.get("transitiveLyo") or []:
        if name in direct_lyo_set:
            continue
        rows.append({"name": name, "kind": "lyo", "tags": ["transitive", "lyo"]})

    direct_pkgs = _pkg_rows_from_groups(node.get("directPackages") or [], scope="direct")
    direct_pkg_names = {r["name"] for r in direct_pkgs}
    rows.extend(direct_pkgs)

    rows.extend(
        _pkg_rows_from_groups(
            node.get("transitivePackages") or [],
            scope="transitive",
            skip_names=direct_pkg_names,
        )
    )

    def sort_key(r: dict) -> tuple:
        scope = 0 if "direct" in (r.get("tags") or []) else 1
        kind = 0 if r.get("kind") == "lyo" else 1
        return (scope, kind, r.get("name") or "")

    rows.sort(key=sort_key)
    return rows


def load_dependency_map(*, include_tests: bool = False) -> dict[str, list[dict]]:
    """project name → dependency rows (same classification as the project graph)."""
    projects = load_slnx_projects()
    central = load_central_package_versions()
    for p in projects:
        parse_refs(p, central)
    if not include_tests:
        projects = [p for p in projects if not is_test_like(p.name)]
    data = build_graph_data(projects)
    return {node["name"]: dependency_rows_for_node(node) for node in data["projects"]}
