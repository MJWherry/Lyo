#!/usr/bin/env python3
"""Normalize every project's references to the Lyo.Common family down to what it actually uses.

The split of Lyo.Common left every consumer pointing at the Lyo.Common facade so the build stayed
green. This walks each project's sources, works out which of the split packages it really needs, and
rewrites its references to exactly that set.

The pass covers every project in the solution, not just former facade consumers, because narrowing
one project stops Common flowing transitively to its consumers. Making each project reference what it
uses is the only self-consistent end state. It is idempotent: re-running converges on the same set.

Each namespace now sits under the package that owns it, so a `using` names its package outright and
no symbol-level guessing is needed. Ambiguity is gone; a project needs exactly the packages whose
namespace prefixes appear in its sources.
"""

from __future__ import annotations

import argparse
import os
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[3]
NET = ROOT / "Lyo.Net"

CORE = "Lyo.Common.Core"
METADATA = "Lyo.Common.Metadata"
JSON = "Lyo.Common.Json"
SYSINFO = "Lyo.SystemInformation"

PROJECT_FILES = {
    CORE: NET / "Core/Common/Lyo.Common.Core/Lyo.Common.Core.csproj",
    METADATA: NET / "Core/Common/Lyo.Common.Metadata/Lyo.Common.Metadata.csproj",
    JSON: NET / "Core/Common/Lyo.Common.Json/Lyo.Common.Json.csproj",
    SYSINFO: NET / "Core/SystemInformation/Lyo.SystemInformation/Lyo.SystemInformation.csproj",
}

# Packages that already include another, so the narrower one is redundant alongside them.
INCLUDES = {
    METADATA: {CORE},
    JSON: {CORE},
    SYSINFO: {CORE},
}

# Longest prefix first so Lyo.Common.Core is not mistaken for a bare Lyo.Common.
PREFIXES = sorted(PROJECT_FILES, key=len, reverse=True)

SKIP_PROJECTS = {CORE, METADATA, JSON, SYSINFO, "Lyo.Common.Tests"}

REFERENCED = re.compile(r"\b(Lyo\.Common\.Core|Lyo\.Common\.Metadata|Lyo\.Common\.Json|Lyo\.SystemInformation)\b")
COMMON_REFERENCE = re.compile(
    r"^([ \t]*)<ProjectReference Include=\"[^\"]*[\\/](Lyo\.Common\.Core|Lyo\.Common\.Metadata|Lyo\.Common\.Json|Lyo\.SystemInformation)\.csproj\"[^>]*/>[ \t]*\r?\n",
    re.M)
ANY_PROJECT_REFERENCE = re.compile(r"^([ \t]*)<ProjectReference Include=\"[^\"]*\"[^>]*/>[ \t]*\r?\n", re.M)


def sources(project: pathlib.Path) -> list[pathlib.Path]:
    out: list[pathlib.Path] = []
    for glob in ("*.cs", "*.razor"):
        out.extend(f for f in project.parent.rglob(glob) if "obj" not in f.parts and "bin" not in f.parts)
    return out


def required_packages(project: pathlib.Path) -> set[str]:
    needed: set[str] = set()
    for f in sources(project):
        needed.update(REFERENCED.findall(f.read_text(errors="ignore")))
    return needed


def minimal(packages: set[str]) -> set[str]:
    return {p for p in packages if not any(p in INCLUDES.get(other, ()) for other in packages if other != p)}


def reference_line(project: pathlib.Path, package: str, indent: str) -> str:
    relative = os.path.relpath(PROJECT_FILES[package], project.parent).replace("/", "\\")
    return f"{indent}<ProjectReference Include=\"{relative}\"/>\n"


def normalize(project: pathlib.Path, desired: set[str]) -> tuple[bool, set[str]]:
    """Rewrites the project's Common-family references to `desired`. Returns (changed, previous)."""
    text = project.read_text()
    matches = list(COMMON_REFERENCE.finditer(text))
    previous = {m.group(2) for m in matches}

    if matches:
        indent, insert_at = matches[0].group(1), matches[0].start()
    else:
        if not (anchor := ANY_PROJECT_REFERENCE.search(text)):
            return False, previous
        indent, insert_at = anchor.group(1), anchor.start()

    # Compare the rendered lines rather than the package names, so a stale relative path or a
    # duplicated reference is corrected instead of being mistaken for the desired state.
    expected = [reference_line(project, p, indent) for p in sorted(desired)]
    if [m.group(0) for m in matches] == expected:
        return False, previous

    # Drop every existing Common-family reference, then re-insert the desired set at the first slot.
    removed_before = sum(len(m.group(0)) for m in matches if m.start() < insert_at)
    text = COMMON_REFERENCE.sub("", text)
    insert_at -= removed_before

    project.write_text(text[:insert_at] + "".join(expected) + text[insert_at:])
    return True, previous


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dry-run", action="store_true")
    parser.add_argument("--verbose", action="store_true")
    args = parser.parse_args()

    projects = sorted(p for p in NET.rglob("*.csproj") if "obj" not in p.parts and p.stem not in SKIP_PROJECTS)
    tally: dict[str, int] = {}
    changed = 0

    for project in projects:
        desired = minimal(required_packages(project))
        has_reference = bool(COMMON_REFERENCE.search(project.read_text()))
        if not desired and not has_reference:
            continue

        key = " + ".join(sorted(desired)) or "(none - dropping reference)"
        tally[key] = tally.get(key, 0) + 1

        if args.dry_run:
            if args.verbose:
                print(f"  {project.stem}: {key}")
            continue

        did, previous = normalize(project, desired)
        if did:
            changed += 1
            if args.verbose:
                print(f"  {project.stem}: {' + '.join(sorted(previous)) or '(none)'} -> {key}")

    for key, n in sorted(tally.items(), key=lambda kv: -kv[1]):
        print(f"  {n:3d}  {key}")
    if not args.dry_run:
        print(f"rewrote {changed} project files")
    return 0


if __name__ == "__main__":
    sys.exit(main())
