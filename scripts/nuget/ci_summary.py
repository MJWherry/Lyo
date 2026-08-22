#!/usr/bin/env python3
"""Write a Markdown report of a CI pipeline run.

Prints to stdout. When $GITHUB_STEP_SUMMARY is set, also appends there
(the Actions run Summary tab).

Usage:
  python3 scripts/nuget/ci_summary.py --phase plan --version 1.0.5 --branch main --scope changed
  python3 scripts/nuget/ci_summary.py --phase result --job pack=success --pack-report artifacts/nuget/pack-report.json
"""

from __future__ import annotations

import argparse
import json
import os
import sys
from pathlib import Path

_HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(_HERE))
sys.path.insert(0, str(_HERE.parent / "docs"))
import build_nuget as n  # noqa: E402
from markdown import document_to_md  # noqa: E402

STAGES_PACK = {"pack", "build-and-pack", "pack-and-publish", "all"}
STAGES_PUBLISH = {"pack-and-publish", "all"}

JOB_ORDER = [
    ("resolve", "Resolve"),
    ("build", "Build"),
    ("pack", "Pack"),
    ("publish-nuget", "nuget.org"),
    ("publish-github", "GitHub Packages"),
    ("tag", "Tag"),
]


def _flag(value: str) -> bool:
    return (value or "").strip().lower() in {"1", "true", "yes", "on"}


def _short_sha(sha: str) -> str:
    sha = (sha or "").strip()
    return sha[:7] if len(sha) > 7 else sha


def _pkg_count(n: int) -> str:
    return f"{n} package" if n == 1 else f"{n} packages"


def _bytes(size: int) -> str:
    if size < 1024:
        return f"{size} B"
    if size < 1024 * 1024:
        return f"{size / 1024:.1f} KiB"
    return f"{size / (1024 * 1024):.1f} MiB"


def _load_json(path: str) -> dict:
    if not path:
        return {}
    file = Path(path)
    if not file.is_file():
        return {}
    try:
        data = json.loads(file.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError):
        return {}
    return data if isinstance(data, dict) else {}


def _job_map(items: list[str]) -> dict[str, str]:
    jobs: dict[str, str] = {}
    for item in items:
        name, _, result = item.partition("=")
        name = name.strip()
        if name:
            jobs[name] = (result.strip() or "unknown")
    return jobs


def _id_from_nupkg(filename: str, version: str) -> str:
    name = filename.strip()
    if name.endswith(".nupkg"):
        name = name[:-6]
    if version and name.endswith("." + version):
        return name[: -(len(version) + 1)]
    return name


def _push_outcomes(report: dict, version: str) -> dict[str, str]:
    out: dict[str, str] = {}
    for status in ("pushed", "skipped", "failed"):
        for filename in report.get(status) or []:
            out[_id_from_nupkg(str(filename), version)] = status
    return out


def _github_urls(sha: str) -> dict[str, str]:
    server = (os.environ.get("GITHUB_SERVER_URL") or "https://github.com").rstrip("/")
    repo = os.environ.get("GITHUB_REPOSITORY") or ""
    run_id = os.environ.get("GITHUB_RUN_ID") or ""
    urls = {"server": server, "repo": repo}
    if repo:
        urls["commit"] = f"{server}/{repo}/commit/{sha}" if sha else ""
        urls["tree"] = f"{server}/{repo}/tree/{sha}" if sha else f"{server}/{repo}"
        urls["run"] = f"{server}/{repo}/actions/runs/{run_id}" if run_id else ""
        urls["packages"] = f"{server}/{repo}/pkgs/nuget"
    return urls


def _do_pack(stages: str) -> bool:
    return stages in STAGES_PACK


def _do_publish_nuget(stages: str, destination: str, dry_run: bool) -> bool:
    return (not dry_run) and stages in STAGES_PUBLISH and destination in {"nuget.org", "both"}


def _do_publish_github(stages: str, destination: str, dry_run: bool) -> bool:
    return (not dry_run) and stages in STAGES_PUBLISH and destination in {"github", "both"}


def resolve_since(scope: str, since: str) -> str:
    if scope != "changed":
        return (since or "").strip()
    return (since or "").strip() or n.default_changed_since()


def select_packages(scope: str, packages: str, since: str) -> tuple[list[str], list[str]]:
    """Return (package names, shared pack-trigger paths that fired)."""
    if scope == "named":
        selected: list[Path] = []
        for pattern in packages.split():
            for project in n.find_projects(pattern):
                if project not in selected:
                    selected.append(project)
        return [n.get_project_name(p) for p in selected], []
    if scope == "all":
        return [n.get_project_name(p) for p in n.find_projects("Lyo.*")], []
    if scope == "changed":
        if not since:
            return [], []
        try:
            paths = n.git_changed_paths(since)
        except SystemExit:
            return [], []
        triggers = [p for p in n.SHARED_PACK_TRIGGERS if p in paths]
        return [n.get_project_name(p) for p in n.find_changed_projects(since)], triggers
    return [], []


def git_commits(since: str, limit: int = 20) -> tuple[int, list[tuple[str, str]]]:
    if not since:
        return 0, []
    count_proc = n._git(["rev-list", "--count", f"{since}..HEAD"])
    if count_proc.returncode != 0:
        return 0, []
    try:
        total = int((count_proc.stdout or "0").strip() or "0")
    except ValueError:
        total = 0
    log_proc = n._git(["log", f"--max-count={limit}", "--format=%h\t%s", f"{since}..HEAD"])
    rows: list[tuple[str, str]] = []
    for line in (log_proc.stdout or "").splitlines():
        short, _, subject = line.partition("\t")
        if short:
            rows.append((short.strip(), subject.strip()))
    return total, rows


def nupkgs_from_dir(folder: Path, version: str) -> list[dict]:
    if not folder.is_dir():
        return []
    rows: list[dict] = []
    suffix = f".{version}.nupkg" if version else ".nupkg"
    for path in sorted(folder.glob("*.nupkg")):
        ident = _id_from_nupkg(path.name, version) if version else path.stem
        if version and not path.name.endswith(suffix):
            ident = path.stem
        rows.append({"id": ident, "file": path.name, "size": path.stat().st_size})
    return rows


def packed_ids(pack_report: dict, nupkgs: list[dict], selected: list[str]) -> list[str]:
    if "packed" in pack_report:
        ids = [str(x) for x in (pack_report.get("packed") or [])]
        if ids:
            return ids
        if pack_report.get("compile_only"):
            return [str(x) for x in (pack_report.get("built") or pack_report.get("selected") or [])]
        return []
    if nupkgs:
        return [str(x.get("id") or "") for x in nupkgs if x.get("id")]
    return list(selected)


def nupkg_size_map(pack_report: dict, nupkgs: list[dict]) -> dict[str, int]:
    sizes: dict[str, int] = {}
    for row in pack_report.get("nupkgs") or []:
        if isinstance(row, dict) and row.get("id"):
            try:
                sizes[str(row["id"])] = int(row.get("size") or 0)
            except (TypeError, ValueError):
                sizes[str(row["id"])] = 0
    for row in nupkgs:
        ident = str(row.get("id") or "")
        if ident and ident not in sizes:
            try:
                sizes[ident] = int(row.get("size") or 0)
            except (TypeError, ValueError):
                sizes[ident] = 0
    return sizes


def _push_label(status: str) -> str:
    if status == "pushed":
        return "pushed"
    if status == "skipped":
        return "already on feed"
    if status == "failed":
        return "failed"
    return "-"


def identity_line(args: argparse.Namespace, urls: dict[str, str], since: str) -> str:
    parts: list[str] = []
    branch = (args.branch or "").strip()
    sha = _short_sha(args.sha)
    if branch and urls.get("tree"):
        parts.append(f"[`{branch}`]({urls['tree']})")
    elif branch:
        parts.append(f"`{branch}`")
    if args.version:
        parts.append(f"`{args.version}`")
    if args.channel:
        parts.append(args.channel)
    if args.destination:
        parts.append(args.destination)
    if args.event:
        parts.append(args.event)
    if sha and urls.get("commit"):
        parts.append(f"[`{sha}`]({urls['commit']})")
    elif sha:
        parts.append(f"`{sha}`")
    if since and urls.get("repo"):
        compare = f"{urls['server']}/{urls['repo']}/compare/{since}...{(args.sha or '').strip() or 'HEAD'}"
        parts.append(f"[changes since `{since}`]({compare})")
    elif since:
        parts.append(f"since `{since}`")
    return " · ".join(parts)


def plan_heading(args: argparse.Namespace, selected: list[str], since: str) -> str:
    version = args.version or "unresolved"
    count = len(selected)
    if args.stages == "build":
        return f"Will build Lyo.slnx as `{version}`"
    if _flag(args.dry_run) and _do_pack(args.stages):
        if count == 0 and args.scope == "changed":
            return f"Dry run. Nothing to pack since `{since}`" if since else "Dry run. Nothing to pack"
        return f"Dry run. Will pack {count} as `{version}`, will not push"
    if count == 0 and args.scope == "changed" and _do_pack(args.stages):
        return f"Nothing to pack since `{since}`" if since else "Nothing to pack"
    if _do_publish_nuget(args.stages, args.destination, _flag(args.dry_run)):
        return f"Will publish {_pkg_count(count)} to nuget.org as `{version}`"
    if _do_publish_github(args.stages, args.destination, _flag(args.dry_run)):
        return f"Will publish {_pkg_count(count)} to GitHub Packages as `{version}`"
    if _do_pack(args.stages):
        return f"Will pack {count} as `{version}` (artifacts only)"
    return f"Pipeline plan for `{version}`"


def result_heading(args: argparse.Namespace, jobs: dict[str, str], packed: list[str], since: str, nuget_push: dict) -> str:
    version = args.version or "unknown version"
    labels = dict(JOB_ORDER)
    for name, _label in JOB_ORDER:
        if jobs.get(name) == "failure":
            return f"Failed at {labels.get(name, name)}"
    for name, result in jobs.items():
        if result == "failure":
            return f"Failed at {name}"

    pack_result = jobs.get("pack", "")
    nuget_result = jobs.get("publish-nuget", "")
    github_result = jobs.get("publish-github", "")
    pushed = [str(x) for x in (nuget_push.get("pushed") or [])]

    if args.stages == "build" and jobs.get("build") == "success":
        return f"Built Lyo.slnx as `{version}`"
    if _flag(args.dry_run) and pack_result == "success":
        if not packed:
            return f"Dry run. Nothing to pack since `{since}`" if since else "Dry run. Nothing to pack"
        return f"Dry run packed {len(packed)} as `{version}`"
    if pack_result == "success" and not packed and _do_pack(args.stages):
        return f"Nothing to pack since `{since}`" if since else "Nothing to pack"
    if nuget_result == "success":
        count = len(pushed) or len(packed)
        if github_result == "success":
            return f"Published {_pkg_count(count)} to nuget.org and GitHub Packages as `{version}`"
        return f"Published {_pkg_count(count)} to nuget.org as `{version}`"
    if github_result == "success":
        count = len(packed)
        return f"Published {_pkg_count(count)} to GitHub Packages as `{version}`"
    if pack_result == "success":
        return f"Packed {len(packed)} as `{version}` (not published)"
    if jobs.get("resolve") == "success" and pack_result in {"", "skipped"}:
        return f"Resolved `{version}`"
    return f"Pipeline finished (`{version}`)"


def plan_prose(args: argparse.Namespace, selected: list[str], since: str, shared_triggers: list[str]) -> str:
    if args.stages == "build":
        return f"Build compiles `Lyo.slnx` as `{args.version}`. No nupkgs."

    count = len(selected)
    bits: list[str] = []
    if args.scope == "changed":
        ref = since or "the last tag"
        if shared_triggers:
            shown = "`, `".join(shared_triggers)
            bits.append(
                f"Shared `{shown}` changed since `{ref}`, so the pack set is every packable Lyo.* library ({count})."
            )
        elif count == 0:
            bits.append(f"No packable library directories changed since `{ref}`. Pack will succeed with no nupkgs.")
        else:
            noun = "library" if count == 1 else "libraries"
            bits.append(f"Pack compiles the {count} {noun} whose directories changed since `{ref}`.")
    elif args.scope == "named":
        bits.append(f"Pack compiles the named set ({count}). Patterns: `{args.packages}`.")
    elif args.scope == "all":
        bits.append(f"Pack compiles every packable Lyo.* library ({count}).")

    if _flag(args.force):
        bits.append("Force is on, so fingerprint skip is off.")

    dry = _flag(args.dry_run)
    if dry:
        bits.append("Dry run. nupkgs stay on the Actions artifact. No push and no tag.")
    elif _do_publish_nuget(args.stages, args.destination, dry) or _do_publish_github(args.stages, args.destination, dry):
        dests: list[str] = []
        if _do_publish_nuget(args.stages, args.destination, dry):
            dests.append("nuget.org")
        if _do_publish_github(args.stages, args.destination, dry):
            dests.append("GitHub Packages")
        bits.append(f"Then push to {' and '.join(dests)} as `{args.version}`.")
        if args.channel == "release" and args.branch == "main" and _do_publish_nuget(args.stages, args.destination, dry):
            bits.append(f"After nuget.org accepts the push, tag `v{args.version}` on HEAD.")
        elif args.channel == "preview":
            bits.append("Preview runs are not tagged.")
    elif _do_pack(args.stages):
        bits.append("nupkgs stay on the Actions artifact. No feed push this run.")

    return " ".join(bits)


def result_prose(
    args: argparse.Namespace,
    jobs: dict[str, str],
    packed: list[str],
    since: str,
    nuget_push: dict,
    github_push: dict,
    pack_report: dict,
) -> str:
    labels = dict(JOB_ORDER)
    for name, _label in JOB_ORDER:
        if jobs.get(name) == "failure":
            return f"{labels.get(name, name)} failed. Later publish and tag jobs did not run. Open that job's log."
    for name, result in jobs.items():
        if result == "failure":
            return f"{name} failed. Open that job's log."

    failed_pkgs = [str(x) for x in (pack_report.get("failed") or [])]
    if failed_pkgs:
        return "Pack reported failures: " + ", ".join(f"`{p}`" for p in failed_pkgs) + "."

    bits: list[str] = []
    if args.stages == "build" and jobs.get("build") == "success":
        return f"Built `Lyo.slnx` as `{args.version}`."

    if jobs.get("pack") == "success":
        skipped = [str(x) for x in (pack_report.get("skipped") or [])]
        if packed:
            bits.append(f"Pack produced {len(packed)} nupkg{'s' if len(packed) != 1 else ''} as `{args.version}`.")
        else:
            ref = since or pack_report.get("since") or "the change window"
            bits.append(f"Pack ran. No nupkgs. Nothing in the pack set since `{ref}`.")
        if skipped:
            bits.append(f"Fingerprint skip left {len(skipped)} unchanged.")

    nuget_result = jobs.get("publish-nuget", "")
    if nuget_result == "success":
        n_push = len(nuget_push.get("pushed") or [])
        n_skip = len(nuget_push.get("skipped") or [])
        bits.append(f"nuget.org accepted {n_push}.")
        if n_skip:
            bits.append(f"{n_skip} already on the feed.")
    elif nuget_result == "skipped":
        if _flag(args.dry_run):
            bits.append("Dry run, so nuget.org was not called.")
        elif args.destination == "none":
            bits.append("destination=none. Artifacts only.")

    github_result = jobs.get("publish-github", "")
    if github_result == "success":
        n_push = len(github_push.get("pushed") or [])
        bits.append(f"GitHub Packages accepted {n_push}.")

    if jobs.get("tag") == "success" and args.version:
        bits.append(f"Tagged `v{args.version}`.")
    elif jobs.get("tag") == "skipped" and args.channel == "preview":
        bits.append("No tag. Previews are not tagged.")

    return " ".join(bits)


def job_notes(
    args: argparse.Namespace,
    jobs: dict[str, str],
    packed: list[str],
    pack_report: dict,
    nuget_push: dict,
    github_push: dict,
) -> dict[str, str]:
    dry = _flag(args.dry_run)
    notes: dict[str, str] = {}
    for name, result in jobs.items():
        if result == "cancelled":
            notes[name] = "cancelled"
            continue
        if result == "failure":
            notes[name] = "failed. Open that job's log."
            continue
        if result == "success":
            if name == "resolve":
                bits = [x for x in (args.version, args.channel, args.destination) if x]
                notes[name] = " ".join(bits)
            elif name == "build":
                notes[name] = "built Lyo.slnx"
            elif name == "pack":
                failed = pack_report.get("failed") or []
                if failed:
                    notes[name] = "failed: " + ", ".join(str(x) for x in failed)
                elif packed:
                    notes[name] = f"{len(packed)} nupkg{'s' if len(packed) != 1 else ''}"
                else:
                    notes[name] = "no nupkgs"
            elif name == "publish-nuget":
                n_push = len(nuget_push.get("pushed") or [])
                n_skip = len(nuget_push.get("skipped") or [])
                if nuget_push:
                    notes[name] = f"{n_push} pushed, {n_skip} already on feed"
                else:
                    notes[name] = "pushed"
            elif name == "publish-github":
                n_push = len(github_push.get("pushed") or [])
                n_skip = len(github_push.get("skipped") or [])
                if github_push:
                    notes[name] = f"{n_push} pushed, {n_skip} already on feed"
                else:
                    notes[name] = "pushed"
            elif name == "tag":
                notes[name] = f"v{args.version}" if args.version else "tagged"
            else:
                notes[name] = ""
            continue
        if result == "skipped":
            if name == "build":
                if _do_pack(args.stages):
                    notes[name] = "not used. Pack compiles the selected libraries. slnx build is stages=build only."
                else:
                    notes[name] = "stages did not include build"
            elif name == "pack":
                notes[name] = "stages did not include pack"
            elif name == "publish-nuget":
                if jobs.get("pack") == "failure":
                    notes[name] = "pack failed"
                elif dry:
                    notes[name] = "dry run. nupkgs stay on the artifact."
                elif not _do_publish_nuget(args.stages, args.destination, dry):
                    if args.destination == "none":
                        notes[name] = "destination=none. artifacts only."
                    elif jobs.get("pack") in {"skipped", ""}:
                        notes[name] = "pack did not run"
                    else:
                        notes[name] = "not publishing to nuget.org this run"
                else:
                    notes[name] = "did not run"
            elif name == "publish-github":
                if jobs.get("pack") == "failure":
                    notes[name] = "pack failed"
                elif dry:
                    notes[name] = "dry run. nupkgs stay on the artifact."
                elif not _do_publish_github(args.stages, args.destination, dry):
                    notes[name] = "not publishing to GitHub Packages this run"
                else:
                    notes[name] = "did not run"
            elif name == "tag":
                if args.channel != "release":
                    notes[name] = "previews are not tagged"
                elif args.branch != "main":
                    notes[name] = "only main is tagged"
                elif dry:
                    notes[name] = "dry run. no tag."
                elif jobs.get("publish-nuget") != "success":
                    notes[name] = "waits for nuget.org"
                else:
                    notes[name] = "did not run"
            else:
                notes[name] = "skipped"
    return notes


def package_section(
    names: list[str],
    *,
    version: str,
    sizes: dict[str, int],
    nuget_outcomes: dict[str, str],
    github_outcomes: dict[str, str],
    failed: set[str],
    skipped: set[str],
    show_nuget: bool,
    show_github: bool,
    show_size: bool,
    urls: dict[str, str],
) -> dict | None:
    if not names:
        return None
    headers = ["Package"]
    if show_size:
        headers.append("nupkg")
    if show_nuget:
        headers.append("nuget.org")
    if show_github:
        headers.append("GitHub Packages")
    pkg_root = urls.get("packages") or ""
    rows: list[list[str]] = []
    for ident in names:
        nuget_href = f"https://www.nuget.org/packages/{ident}/{version}" if version else f"https://www.nuget.org/packages/{ident}"
        cells = [f"[`{ident}`]({nuget_href})"]
        if show_size:
            if ident in failed:
                size = "failed"
            elif ident in skipped:
                size = "unchanged, skipped"
            elif ident in sizes:
                size = _bytes(sizes[ident])
            else:
                size = "-"
            cells.append(size)
        if show_nuget:
            nu = _push_label(nuget_outcomes.get(ident, ""))
            if nuget_outcomes.get(ident) == "pushed":
                nu = f"[pushed]({nuget_href})"
            cells.append(nu)
        if show_github:
            gh = _push_label(github_outcomes.get(ident, ""))
            if pkg_root and github_outcomes.get(ident) == "pushed":
                gh = f"[pushed]({pkg_root}/{ident})"
            cells.append(gh)
        rows.append(cells)
    return {"type": "table", "title": f"Packages ({len(names)})", "headers": headers, "rows": rows}


def jobs_section(jobs: dict[str, str], notes: dict[str, str]) -> dict | None:
    if not jobs:
        return None
    rows: list[list[str]] = []
    seen: set[str] = set()
    labels = dict(JOB_ORDER)
    for name, label in JOB_ORDER:
        if name not in jobs:
            continue
        seen.add(name)
        rows.append([label, jobs[name], notes.get(name, "") or "-"])
    for name, result in jobs.items():
        if name in seen:
            continue
        rows.append([name, result, notes.get(name, "") or "-"])
    return {"type": "table", "title": "Jobs", "headers": ["Job", "Result", "Notes"], "rows": rows}


def commits_section(since: str, total: int, rows: list[tuple[str, str]], urls: dict[str, str]) -> dict | None:
    if not since or total <= 0:
        return None
    title = f"{total} commit{'s' if total != 1 else ''} since `{since}`"
    items: list[str] = []
    for short, subject in rows:
        commit_url = f"{urls['server']}/{urls['repo']}/commit/{short}" if urls.get("repo") else ""
        if commit_url:
            items.append(f"[`{short}`]({commit_url}) {subject}")
        else:
            items.append(f"`{short}` {subject}")
    if total > len(rows):
        items.append(f"… {total - len(rows)} more")
    return {"type": "details", "title": title, "items": items}


def build_document(args: argparse.Namespace) -> dict:
    jobs = _job_map(args.job)
    pack_report = _load_json(args.pack_report)
    nuget_push: dict = {}
    github_push: dict = {}
    for item in args.push_report:
        feed, _, path = item.partition("=")
        feed = feed.strip()
        path = path.strip() or feed
        report = _load_json(path)
        if not report:
            continue
        label = feed if "=" in item else str(report.get("feed") or "")
        if label in {"nuget", "nuget.org"} or str(report.get("feed")) == "nuget.org":
            nuget_push = report
        elif label in {"github", "gh"} or str(report.get("feed")) == "github":
            github_push = report

    since = (pack_report.get("since") or "").strip() or resolve_since(args.scope, args.since)
    shared_triggers: list[str] = []
    selected: list[str] = [str(x) for x in (pack_report.get("selected") or [])]
    if args.phase == "plan":
        try:
            selected, shared_triggers = select_packages(args.scope, args.packages, since)
        except SystemExit:
            pass

    nupkgs = nupkgs_from_dir(Path(args.nupkg_dir), args.version) if args.nupkg_dir else []
    packed = packed_ids(pack_report, nupkgs, [])
    if args.phase == "plan":
        packed = list(selected)
    sizes = nupkg_size_map(pack_report, nupkgs)
    nuget_outcomes = _push_outcomes(nuget_push, args.version)
    github_outcomes = _push_outcomes(github_push, args.version)
    failed = {str(x) for x in (pack_report.get("failed") or [])}
    skipped = {str(x) for x in (pack_report.get("skipped") or [])}
    urls = _github_urls(args.sha)

    if args.phase == "plan":
        heading = plan_heading(args, selected, since)
        prose = plan_prose(args, selected, since, shared_triggers)
        names = selected
    else:
        heading = result_heading(args, jobs, packed, since, nuget_push)
        prose = result_prose(args, jobs, packed, since, nuget_push, github_push, pack_report)
        names = list(dict.fromkeys(packed + sorted(failed) + sorted(skipped)))

    sections: list[dict] = []
    if args.phase == "plan" and args.stages != "build" and names:
        section = package_section(
            names,
            version=args.version,
            sizes={},
            nuget_outcomes={},
            github_outcomes={},
            failed=set(),
            skipped=set(),
            show_nuget=False,
            show_github=False,
            show_size=False,
            urls=urls,
        )
        if section:
            sections.append(section)
    elif args.phase == "result" and (names or failed or skipped):
        section = package_section(
            names,
            version=args.version,
            sizes=sizes,
            nuget_outcomes=nuget_outcomes,
            github_outcomes=github_outcomes,
            failed=failed,
            skipped=skipped,
            show_nuget=bool(nuget_outcomes),
            show_github=bool(github_outcomes),
            show_size=True,
            urls=urls,
        )
        if section:
            sections.append(section)

    if jobs:
        notes = job_notes(args, jobs, packed, pack_report, nuget_push, github_push)
        section = jobs_section(jobs, notes)
        if section:
            sections.append(section)

    if args.scope == "changed" and since:
        total, commit_rows = git_commits(since)
        section = commits_section(since, total, commit_rows, urls)
        if section:
            sections.append(section)

    doc: dict = {
        "name": heading,
        "tagline": identity_line(args, urls, since if args.scope == "changed" else ""),
        "description": prose,
        "sections": sections,
    }
    return doc


def build_markdown(args: argparse.Namespace) -> str:
    return document_to_md(build_document(args))


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
    parser.add_argument("--force", default="")
    parser.add_argument("--job", action="append", default=[], help="name=result (repeatable)")
    parser.add_argument("--pack-report", default="", help="pack-report.json from the pack job")
    parser.add_argument("--nupkg-dir", default="", help="Directory of packed nupkgs")
    parser.add_argument("--push-report", action="append", default=[], help="feed=path (repeatable)")
    args = parser.parse_args(argv)
    write_summary(build_markdown(args))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
