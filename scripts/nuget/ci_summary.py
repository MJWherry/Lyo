#!/usr/bin/env python3
"""Write a Markdown overview of a CI pipeline run.

Prints to stdout. When $GITHUB_STEP_SUMMARY is set, also appends there
(the Actions run Summary tab).

Usage:
  python3 scripts/nuget/ci_summary.py --phase plan --version 1.0.5 --branch main ...
  python3 scripts/nuget/ci_summary.py --phase result --job pack=success --job tag=skipped ...
"""

from __future__ import annotations

import argparse
import os
from pathlib import Path


def _cell(value: str) -> str:
    return value.replace("|", "\\|").replace("\n", " ").strip() or "—"


def _table(rows: list[tuple[str, str]]) -> list[str]:
    lines = ["| Field | Value |", "| --- | --- |"]
    for key, value in rows:
        if value == "":
            continue
        lines.append(f"| {key} | {_cell(value)} |")
    return lines


def _job_rows(jobs: list[str]) -> list[tuple[str, str]]:
    rows: list[tuple[str, str]] = []
    for item in jobs:
        name, _, result = item.partition("=")
        name = name.strip()
        result = result.strip() or "unknown"
        if name:
            rows.append((name, result))
    return rows


def build_markdown(args: argparse.Namespace) -> str:
    sha = (args.sha or "").strip()
    if len(sha) > 7:
        sha = sha[:7]
    dry = (args.dry_run or "").strip().lower() in {"1", "true", "yes"}
    rows: list[tuple[str, str]] = [
        ("Branch", f"`{args.branch}`" if args.branch else ""),
        ("SHA", f"`{sha}`" if sha else ""),
        ("Event", args.event),
        ("Version", f"`{args.version}`" if args.version else ""),
        ("Channel", args.channel),
        ("Destination", args.destination),
        ("Scope", args.scope),
        ("Packages", args.packages),
        ("Since", args.since),
        ("Stages", args.stages),
        ("Dry run", "yes" if dry else "no"),
    ]
    title = "Pipeline result" if args.phase == "result" else "Pipeline plan"
    who = " — ".join(p for p in (args.branch, args.version) if p)
    heading = f"{title} — {who}" if who else title
    lines = [f"# {heading}", "", *_table(rows)]
    job_rows = _job_rows(args.job)
    if job_rows:
        lines += ["", "## Jobs", "", "| Job | Result |", "| --- | --- |"]
        for name, result in job_rows:
            lines.append(f"| {name} | {result} |")
    lines.append("")
    return "\n".join(lines)


def write_summary(md: str) -> None:
    text = md if md.endswith("\n") else md + "\n"
    print(text, end="")
    dest = os.environ.get("GITHUB_STEP_SUMMARY")
    if dest:
        Path(dest).open("a", encoding="utf-8").write(text)


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--phase", choices=("plan", "result"), default="result")
    parser.add_argument("--branch", default="")
    parser.add_argument("--sha", default="")
    parser.add_argument("--event", default="")
    parser.add_argument("--version", default="")
    parser.add_argument("--channel", default="")
    parser.add_argument("--destination", default="")
    parser.add_argument("--scope", default="")
    parser.add_argument("--packages", default="")
    parser.add_argument("--since", default="")
    parser.add_argument("--stages", default="")
    parser.add_argument("--dry-run", default="")
    parser.add_argument("--job", action="append", default=[], help="name=result (repeatable)")
    args = parser.parse_args(argv)
    write_summary(build_markdown(args))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
