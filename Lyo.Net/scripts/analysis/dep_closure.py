#!/usr/bin/env python3
"""Per-TFM transitive dependency closure for Lyo projects.

Answers "what does this project actually drag in, on this target framework?"
and diffs that against a saved snapshot so a refactor can prove a closure
shrank rather than grew.

Unlike the docs project graph (scripts/gen_graph.py), ProjectReference entries
are evaluated per target framework here. Lyo.Validation and friends gate whole
project references behind `Condition="'$(TargetFramework)' == '...'"`, so a
framework-blind walk reports dependencies that a given TFM never sees.

Usage (repo root):
  # Inspect
  python3 Lyo.Net/scripts/analysis/dep_closure.py Lyo.Validation Lyo.Query
  python3 Lyo.Net/scripts/analysis/dep_closure.py --tfm net10.0 Lyo.Common.Core

  # Record a baseline, refactor, then prove nothing grew
  python3 Lyo.Net/scripts/analysis/dep_closure.py --save Lyo.Net/scripts/analysis/baselines/pre.json Lyo.Query Lyo.Validation
  python3 Lyo.Net/scripts/analysis/dep_closure.py --baseline Lyo.Net/scripts/analysis/baselines/pre.json Lyo.Query Lyo.Validation

Exit codes: 0 clean, 1 a closure grew (or a requested project is missing),
2 bad invocation.
"""

from __future__ import annotations

import argparse
import fnmatch
import json
import sys
import xml.etree.ElementTree as ET
from dataclasses import dataclass, field
from pathlib import Path

_REPO_SCRIPTS = Path(__file__).resolve().parents[3] / "scripts"
if str(_REPO_SCRIPTS) not in sys.path:
    sys.path.insert(0, str(_REPO_SCRIPTS))

from lyo_tooling.dotnet import (  # noqa: E402
    REPO_ROOT,
    framework_label,
    load_central_package_versions,
    parse_msbuild_xml,
    read_target_frameworks,
)

SLNX = REPO_ROOT / "Lyo.Net" / "Lyo.slnx"
DEFAULT_BASELINE_DIR = Path(__file__).resolve().parent / "baselines"


@dataclass
class Project:
    name: str
    rel_path: str
    abs_path: Path
    folder: str = ""
    tfms: list[str] = field(default_factory=list)
    # (target project name, framework label)
    project_refs: list[tuple[str, str]] = field(default_factory=list)
    # (package name, version, framework label)
    package_refs: list[tuple[str, str, str]] = field(default_factory=list)


def applies_to(fw_label: str, tfm: str) -> bool:
    """Whether an item tagged with `fw_label` is present when building `tfm`.

    `framework_label` reduces a Condition to "all", a TFM, or "!TFM". Anything
    else is an expression we do not evaluate, so treat it as present.
    """
    if fw_label == "all":
        return True
    if fw_label.startswith("!"):
        return fw_label[1:] != tfm
    if fw_label.replace(".", "").isalnum() or "-" in fw_label or "+" in fw_label:
        return fw_label == tfm
    return True


def load_projects() -> dict[str, Project]:
    """Every project registered in Lyo.slnx, with per-TFM refs parsed."""
    root = parse_msbuild_xml(SLNX)
    base = SLNX.parent
    central = load_central_package_versions()
    projects: dict[str, Project] = {}

    for folder in root.iter("Folder"):
        folder_name = folder.attrib.get("Name", "/")
        for node in folder.findall("Project"):
            rel = node.attrib["Path"].replace("\\", "/")
            abs_path = (base / rel).resolve()
            project = Project(
                name=abs_path.stem,
                rel_path=rel,
                abs_path=abs_path,
                folder=folder_name.strip("/"),
            )
            _parse_project(project, central)
            projects[project.name] = project
    return projects


def _parse_project(project: Project, central: dict[str, str]) -> None:
    if not project.abs_path.is_file():
        return
    project.tfms = read_target_frameworks(project.abs_path)
    try:
        root = parse_msbuild_xml(project.abs_path)
    except ET.ParseError:
        return

    for group in root.iter("ItemGroup"):
        group_fw = framework_label(group.attrib.get("Condition", "") or "")

        for ref in group.findall("ProjectReference"):
            include = ref.attrib.get("Include")
            if not include:
                continue
            target = Path(include.replace("\\", "/")).stem
            if target == project.name:
                continue
            item_fw = framework_label(ref.attrib.get("Condition", "") or "")
            fw = group_fw if item_fw == "all" else item_fw
            entry = (target, fw)
            if entry not in project.project_refs:
                project.project_refs.append(entry)

        for pkg in group.findall("PackageReference"):
            include = pkg.attrib.get("Include")
            if not include:
                continue
            version = pkg.attrib.get("Version", "")
            if not version:
                node = pkg.find("Version")
                version = (node.text or "").strip() if node is not None else ""
            version = version.strip() or central.get(include, "")
            item_fw = framework_label(pkg.attrib.get("Condition", "") or "")
            fw = group_fw if item_fw == "all" else item_fw
            entry = (include, version, fw)
            if entry not in project.package_refs:
                project.package_refs.append(entry)


def closure(
    name: str, tfm: str, projects: dict[str, Project]
) -> tuple[list[str], dict[str, str], int]:
    """Transitive (projects, packages, max depth) reachable from `name` on `tfm`."""
    root = projects.get(name)
    if root is None:
        return [], {}, 0

    reached: dict[str, int] = {}
    packages: dict[str, str] = {}
    for pkg, version, fw in root.package_refs:
        if applies_to(fw, tfm):
            packages[pkg] = version

    queue: list[tuple[str, int]] = [
        (target, 1) for target, fw in root.project_refs if applies_to(fw, tfm)
    ]
    while queue:
        current, depth = queue.pop(0)
        if current in reached and reached[current] <= depth:
            continue
        if current in reached:
            continue
        reached[current] = depth
        node = projects.get(current)
        if node is None:
            continue
        # A dependency built for `tfm` resolves its own refs for the nearest
        # compatible TFM it declares; fall back to `tfm` when it multi-targets.
        node_tfm = tfm if (not node.tfms or tfm in node.tfms) else node.tfms[0]
        for pkg, version, fw in node.package_refs:
            if applies_to(fw, node_tfm):
                packages.setdefault(pkg, version)
        for target, fw in node.project_refs:
            if applies_to(fw, node_tfm) and target != name:
                queue.append((target, depth + 1))

    max_depth = max(reached.values()) if reached else 0
    return sorted(reached), packages, max_depth


def snapshot(names: list[str], projects: dict[str, Project]) -> dict:
    out: dict = {"projects": {}}
    for name in names:
        project = projects[name]
        tfms = project.tfms or ["net10.0"]
        entry: dict = {}
        for tfm in tfms:
            deps, packages, depth = closure(name, tfm, projects)
            entry[tfm] = {
                "projects": deps,
                "packages": {k: packages[k] for k in sorted(packages)},
                "maxDepth": depth,
            }
        out["projects"][name] = entry
    return out


def render(data: dict) -> str:
    lines: list[str] = []
    for name in sorted(data["projects"]):
        for tfm in sorted(data["projects"][name]):
            entry = data["projects"][name][tfm]
            deps = entry["projects"]
            packages = entry["packages"]
            lines.append(f"{name}  [{tfm}]  depth {entry['maxDepth']}")
            lines.append(f"  projects ({len(deps)}): {', '.join(deps) or '-'}")
            pkg_text = ", ".join(f"{k} {v}".strip() for k, v in packages.items())
            lines.append(f"  packages ({len(packages)}): {pkg_text or '-'}")
            lines.append("")
    return "\n".join(lines).rstrip() + "\n"


def diff(baseline: dict, current: dict) -> tuple[str, bool]:
    """Render a baseline-vs-current report. Returns (text, grew).

    Only projects in `current` are compared, so asking about a handful of projects
    does not report every other baseline entry as missing.

    A package appearing where it did not before is the failure signal: it means real
    dependency weight arrived. New *project* edges are reported but not failed on,
    because splitting one package into several necessarily adds edges while reducing
    the weight behind them.
    """
    lines: list[str] = []
    grew = False

    for name in sorted(current.get("projects", {})):
        base_entry = baseline.get("projects", {}).get(name)
        cur_entry = current["projects"][name]
        if base_entry is None:
            lines.append(f"{name}: new to the snapshot (no baseline)")
            continue

        for tfm in sorted(set(base_entry) | set(cur_entry)):
            base = base_entry.get(tfm)
            cur = cur_entry.get(tfm)
            if base is None:
                lines.append(f"{name} [{tfm}]: new target framework")
                continue
            if cur is None:
                lines.append(f"{name} [{tfm}]: target framework removed")
                continue

            for kind in ("projects", "packages"):
                before = set(base[kind])
                after = set(cur[kind])
                added = sorted(after - before)
                removed = sorted(before - after)
                if not added and not removed:
                    continue
                lines.append(f"{name} [{tfm}] {kind}: {len(before)} -> {len(after)}")
                for item in removed:
                    lines.append(f"  - {item}")
                for item in added:
                    lines.append(f"  + {item}")
                if added and kind == "packages":
                    grew = True

    if not lines:
        return "No closure changes.\n", False
    return "\n".join(lines) + "\n", grew


def self_test(projects: dict[str, Project]) -> int:
    """Guard against the depth-1 walk bug: assert a known multi-hop path.

    Lyo.Query reaches Lyo.Exceptions only through intermediate projects, so a
    correct walk reports depth >= 2 and includes the far end.
    """
    failures: list[str] = []
    if "Lyo.Query" not in projects:
        print("self-test skipped: Lyo.Query not in the solution")
        return 0

    deps, packages, depth = closure("Lyo.Query", "net10.0", projects)
    if depth < 2:
        failures.append(f"expected multi-hop depth >= 2 for Lyo.Query, got {depth}")
    if "Lyo.Exceptions" not in deps:
        failures.append("expected Lyo.Exceptions in the Lyo.Query closure")
    if len(deps) < 2:
        failures.append(f"expected several projects in the closure, got {deps}")
    if not packages:
        failures.append("expected at least one transitive package for Lyo.Query")

    direct = {t for t, _ in projects["Lyo.Query"].project_refs}
    if set(deps) <= direct:
        failures.append("closure never went past direct references")

    for failure in failures:
        print(f"FAIL: {failure}")
    if failures:
        return 1
    print(f"self-test ok (Lyo.Query: {len(deps)} projects, depth {depth})")
    return 0


def resolve_names(patterns: list[str], projects: dict[str, Project]) -> list[str]:
    if not patterns:
        return []
    selected: list[str] = []
    for pattern in patterns:
        if pattern in projects:
            if pattern not in selected:
                selected.append(pattern)
            continue
        matched = sorted(n for n in projects if fnmatch.fnmatch(n, pattern))
        if not matched:
            print(f"warning: no project matched '{pattern}'")
        for name in matched:
            if name not in selected:
                selected.append(name)
    return selected


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Per-TFM transitive dependency closure for Lyo projects.",
        formatter_class=argparse.RawDescriptionHelpFormatter,
    )
    parser.add_argument("patterns", nargs="*", help="Project names or glob patterns")
    parser.add_argument("--tfm", help="Restrict output to a single target framework")
    parser.add_argument("--save", metavar="PATH", help="Write a JSON snapshot")
    parser.add_argument("--baseline", metavar="PATH", help="Diff against a JSON snapshot")
    parser.add_argument("--json", action="store_true", help="Print JSON to stdout")
    parser.add_argument("--self-test", action="store_true", help="Verify the walk itself")
    args = parser.parse_args()

    projects = load_projects()
    if not projects:
        print(f"error: no projects loaded from {SLNX}")
        return 1

    if args.self_test:
        return self_test(projects)

    names = resolve_names(args.patterns, projects)
    if not names and args.baseline:
        baseline_path = _resolve_path(args.baseline)
        if baseline_path.is_file():
            names = sorted(json.loads(baseline_path.read_text()).get("projects", {}))
    if not names:
        parser.error("no projects selected; pass a name or glob pattern")

    data = snapshot(names, projects)
    if args.tfm:
        for name, entry in data["projects"].items():
            data["projects"][name] = {k: v for k, v in entry.items() if k == args.tfm}

    if args.baseline:
        baseline_path = _resolve_path(args.baseline)
        if not baseline_path.is_file():
            print(f"error: baseline not found: {baseline_path}")
            return 1
        text, grew = diff(json.loads(baseline_path.read_text()), data)
        sys.stdout.write(text)
        return 1 if grew else 0

    if args.save:
        out_path = _resolve_path(args.save)
        out_path.parent.mkdir(parents=True, exist_ok=True)
        out_path.write_text(json.dumps(data, indent=2, sort_keys=True) + "\n")
        print(f"wrote {out_path}")

    if args.json:
        print(json.dumps(data, indent=2, sort_keys=True))
    elif not args.save:
        sys.stdout.write(render(data))
    return 0


def _resolve_path(value: str) -> Path:
    path = Path(value)
    if path.is_absolute():
        return path
    if path.parent == Path("baselines") or str(path.parent) == "baselines":
        return DEFAULT_BASELINE_DIR / path.name
    return (Path.cwd() / path).resolve()


if __name__ == "__main__":
    sys.exit(main())
