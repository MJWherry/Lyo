#!/usr/bin/env python3
"""Measure duplicated code blocks across the Lyo solution.

Reports two numbers that answer different questions:

  type-1 (identifiers kept)     -> "how much is literal copy-paste?"
  type-2 (identifiers renamed)  -> "how much is the same code with different domain nouns?"

The type-2 figure is the interesting one for this repo: parametric duplication
(`JobFooService` vs `ReportFooService`) does not show up in a type-1 scan at all.

Generated EF migration code is excluded by default -- it is machine-authored and
churns by design, and including it swamps the handwritten signal.

Usage (repo root):
  python3 Lyo.Net/scripts/analysis/clone_report.py                      # summary
  python3 Lyo.Net/scripts/analysis/clone_report.py --top 40             # biggest clusters
  python3 Lyo.Net/scripts/analysis/clone_report.py --json out.json      # machine-readable
  python3 Lyo.Net/scripts/analysis/clone_report.py --baseline out.json  # compare against a prior run
"""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import sys
from collections import defaultdict
from dataclasses import dataclass, field
from pathlib import Path

_REPO_SCRIPTS = Path(__file__).resolve().parents[3] / "scripts"
if str(_REPO_SCRIPTS) not in sys.path:
    sys.path.insert(0, str(_REPO_SCRIPTS))

from lyo_tooling.dotnet import REPO_ROOT  # noqa: E402

SOURCE_ROOT = REPO_ROOT / "Lyo.Net"
SOURCE_SUFFIXES = (".cs", ".razor")
EXCLUDED_DIR_NAMES = frozenset({"obj", "bin", "node_modules", ".idea", ".vs"})

# Minimum block length. Eight lines is the PMD CPD default for C-family languages and
# filters out incidental matches (using blocks, property runs) without hiding real clones.
DEFAULT_MIN_BLOCK = 8

_MIGRATION_PATH_RE = re.compile(r"[\\/]Migrations[\\/]")
_COMMENT_RE = re.compile(r"^\s*(//|///|\*|/\*|\*/)")
_STRING_RE = re.compile(r"""(?<!\\)(?:"(?:[^"\\]|\\.)*"|'(?:[^'\\]|\\.)*')""")
_NUMBER_RE = re.compile(r"\b\d[\d_.]*[fdmuUlL]?\b")
_IDENTIFIER_RE = re.compile(r"\b[A-Za-z_][A-Za-z0-9_]*\b")

# Kept verbatim under identifier renaming so structure still has to match; without this
# every `if`/`return`/`var` collapses to the same placeholder and similarity inflates.
CSHARP_KEYWORDS = frozenset("""
abstract as async await base bool break byte case catch char checked class const continue decimal
default delegate do double else enum event explicit extern false finally fixed float for foreach get
global goto if implicit in init int interface internal is lock long namespace new nint nuint null
object operator out override params private protected public readonly record ref required return
sbyte sealed set short sizeof stackalloc static string struct switch this throw true try typeof uint
ulong unchecked unsafe ushort using var virtual void volatile when where while with yield
""".split())

# Lines that carry no structural information on their own. A block boundary that starts or ends
# on one of these is not evidence of a clone.
_NOISE_LINES = frozenset({"{", "}", "};", ")", ");", "]", "],", "});", "else", "try", "break;", "return;"})


@dataclass
class Line:
    """One normalized source line, tagged with where it came from."""

    path: str
    lineno: int
    normalized: str


@dataclass
class Cluster:
    """A block of consecutive lines appearing at 2+ locations."""

    length: int
    occurrences: list[tuple[str, int]] = field(default_factory=list)

    @property
    def copies(self) -> int:
        return len(self.occurrences)

    @property
    def redundant_lines(self) -> int:
        """Lines that would disappear if all copies collapsed into one."""
        return self.length * (self.copies - 1)

    @property
    def projects(self) -> set[str]:
        return {project_of(path) for path, _ in self.occurrences}


def iter_source_files(include_migrations: bool) -> list[Path]:
    files: list[Path] = []
    for path in SOURCE_ROOT.rglob("*"):
        if path.suffix not in SOURCE_SUFFIXES or not path.is_file():
            continue
        if EXCLUDED_DIR_NAMES.intersection(path.parts):
            continue
        if not include_migrations and _MIGRATION_PATH_RE.search(str(path)):
            continue
        files.append(path)
    return sorted(files)


def project_of(rel_path: str) -> str:
    """Owning project for a repo-relative path: the nearest `Lyo.*` ancestor directory."""
    for part in reversed(Path(rel_path).parts[:-1]):
        if part.startswith("Lyo."):
            return part
    parts = Path(rel_path).parts
    return parts[-2] if len(parts) > 1 else "<root>"


def normalize(line: str, rename_identifiers: bool) -> str | None:
    """Collapse a source line to its comparable form, or None if it carries no signal.

    String and numeric literals always collapse; two blocks that differ only in a log
    message are still the same code. Identifiers collapse only in type-2 mode.
    """
    stripped = line.strip()
    if not stripped or _COMMENT_RE.match(stripped) or stripped in _NOISE_LINES:
        return None

    text = _STRING_RE.sub('""', stripped)
    text = _NUMBER_RE.sub("0", text)
    if rename_identifiers:
        text = _IDENTIFIER_RE.sub(lambda m: m.group(0) if m.group(0) in CSHARP_KEYWORDS else "$I", text)
    return re.sub(r"\s+", " ", text)


def build_line_table(files: list[Path], rename_identifiers: bool) -> list[Line]:
    table: list[Line] = []
    for path in files:
        try:
            content = path.read_text(encoding="utf-8", errors="replace")
        except OSError:
            continue
        rel = str(path.relative_to(REPO_ROOT))
        for lineno, raw in enumerate(content.splitlines(), start=1):
            norm = normalize(raw, rename_identifiers)
            if norm is not None:
                table.append(Line(rel, lineno, norm))
    return table


def find_clusters(table: list[Line], min_block: int) -> list[Cluster]:
    """Seed-and-extend maximal repeated blocks.

    Hash every window of `min_block` consecutive lines within a file, group identical
    hashes, then extend each group forward as far as all members agree. Only maximal
    blocks are kept, so a 40-line clone is reported once rather than as 33 sub-blocks.
    """
    seeds: dict[str, list[int]] = defaultdict(list)
    for i in range(len(table) - min_block + 1):
        window = table[i : i + min_block]
        # Windows must not straddle a file boundary.
        if window[0].path != window[-1].path:
            continue
        digest = hashlib.blake2b(
            "\n".join(l.normalized for l in window).encode("utf-8"), digest_size=16
        ).digest()
        seeds[digest.hex()].append(i)

    consumed: set[int] = set()
    clusters: list[Cluster] = []
    for starts in sorted(seeds.values(), key=lambda s: -len(s)):
        if len(starts) < 2:
            continue
        starts = [s for s in starts if s not in consumed]
        if len(starts) < 2:
            continue

        length = min_block
        while True:
            nxt = length
            probe = [s + nxt for s in starts]
            if any(p >= len(table) for p in probe):
                break
            first = table[probe[0]]
            if first.path != table[starts[0]].path:
                break
            if any(
                table[p].normalized != first.normalized or table[p].path != table[s].path
                for p, s in zip(probe, starts)
            ):
                break
            length += 1

        # Overlapping copies of the same block (a loop body repeated back-to-back) would
        # otherwise be double-counted; keep only non-overlapping starts.
        kept: list[int] = []
        last_end = -1
        for s in sorted(starts):
            if s > last_end:
                kept.append(s)
                last_end = s + length - 1
        if len(kept) < 2:
            continue

        for s in kept:
            consumed.update(range(s, s + length))
        clusters.append(
            Cluster(length=length, occurrences=[(table[s].path, table[s].lineno) for s in kept])
        )

    return sorted(clusters, key=lambda c: -c.redundant_lines)


def summarize(files: list[Path], min_block: int) -> dict:
    result: dict = {"files": len(files), "min_block": min_block, "modes": {}}
    for mode, rename in (("type1", False), ("type2", True)):
        table = build_line_table(files, rename)
        clusters = find_clusters(table, min_block)
        redundant = sum(c.redundant_lines for c in clusters)
        cross = [c for c in clusters if len(c.projects) > 1]
        result["modes"][mode] = {
            "normalized_lines": len(table),
            "blocks": len(clusters),
            "redundant_lines": redundant,
            "redundant_pct": round(100 * redundant / len(table), 2) if table else 0.0,
            "cross_project_blocks": len(cross),
            "cross_project_redundant_lines": sum(c.redundant_lines for c in cross),
            "clusters": [
                {
                    "length": c.length,
                    "copies": c.copies,
                    "redundant_lines": c.redundant_lines,
                    "projects": sorted(c.projects),
                    "occurrences": [f"{p}:{n}" for p, n in c.occurrences],
                }
                for c in clusters
            ],
        }
    return result


def print_report(result: dict, top: int, baseline: dict | None) -> None:
    print(f"Scanned {result['files']} files (min block {result['min_block']} lines)\n")
    for mode, label in (("type1", "type-1 literal copy-paste"), ("type2", "type-2 parametric (identifiers renamed)")):
        m = result["modes"][mode]
        print(f"{label}")
        print(f"  normalized lines      {m['normalized_lines']:>9,}")
        print(f"  duplicate blocks      {m['blocks']:>9,}")
        print(f"  redundant lines       {m['redundant_lines']:>9,}  ({m['redundant_pct']}%)")
        print(f"  cross-project blocks  {m['cross_project_blocks']:>9,}  ({m['cross_project_redundant_lines']:,} lines)")
        if baseline:
            b = baseline["modes"][mode]
            delta = m["redundant_lines"] - b["redundant_lines"]
            print(f"  vs baseline           {delta:>+9,} lines")
        print()

    if top:
        print(f"Top {top} type-2 clusters by redundant lines:\n")
        for i, c in enumerate(result["modes"]["type2"]["clusters"][:top], start=1):
            scope = f"{len(c['projects'])} projects" if len(c["projects"]) > 1 else "intra-project"
            print(f"{i:>3}. {c['redundant_lines']:>5} lines  ({c['length']}L x {c['copies']} copies, {scope})")
            for occ in c["occurrences"][:6]:
                print(f"       {occ}")
            if len(c["occurrences"]) > 6:
                print(f"       ... +{len(c['occurrences']) - 6} more")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--min-block", type=int, default=DEFAULT_MIN_BLOCK, help="minimum duplicated block length in lines")
    parser.add_argument("--top", type=int, default=25, help="how many clusters to list (0 to skip)")
    parser.add_argument("--include-migrations", action="store_true", help="include generated EF migration code")
    parser.add_argument("--json", type=Path, help="write full results to this path")
    parser.add_argument("--baseline", type=Path, help="compare against a previous --json run")
    args = parser.parse_args()

    files = iter_source_files(args.include_migrations)
    if not files:
        print(f"error: no source files found under {SOURCE_ROOT}", file=sys.stderr)
        return 1

    result = summarize(files, args.min_block)

    baseline = None
    if args.baseline:
        if args.baseline.is_file():
            baseline = json.loads(args.baseline.read_text(encoding="utf-8"))
        else:
            print(f"warning: baseline {args.baseline} not found; skipping comparison", file=sys.stderr)

    print_report(result, args.top, baseline)

    if args.json:
        args.json.parent.mkdir(parents=True, exist_ok=True)
        args.json.write_text(json.dumps(result, indent=2), encoding="utf-8")
        print(f"\nWrote {args.json}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
