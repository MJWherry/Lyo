#!/usr/bin/env python3
"""Map GitHub Actions pack inputs onto build_nuget.py and resolve versions.

Usage:
  python3 scripts/nuget/ci_pack.py --scope all --version 1.2.0 --channel preview
  python3 scripts/nuget/ci_pack.py --scope named --packages "Lyo.Encryption Lyo.Encryption.*" --version 1.2.0 --channel release
  python3 scripts/nuget/ci_pack.py --scope changed --since v1.0.0 --version 1.2.0 --channel release
  python3 scripts/nuget/ci_pack.py --emit-config
  python3 scripts/nuget/ci_pack.py --resolve-main-version --print-resolved-version
"""

from __future__ import annotations

import argparse
import os
import re
import subprocess
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[2]
BUILD_NUGET = Path(__file__).resolve().parent / "build_nuget.py"

SEMVER_RE = re.compile(
    r"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)"
    r"(?:-((?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?"
    r"(?:\+([0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$"
)
TAG_RE = re.compile(r"^v?(\d+\.\d+\.\d+(?:[-+][0-9A-Za-z.-]+)?)$")

STAGES_BUILD = {"build", "build-and-pack", "all"}
STAGES_PACK = {"pack", "build-and-pack", "pack-and-publish", "all"}
STAGES_PUBLISH = {"pack-and-publish", "all"}


def _git(args: list[str]) -> subprocess.CompletedProcess[str]:
    return subprocess.run(["git", "-C", str(REPO_ROOT), *args], capture_output=True, text=True, check=False)


def validate_semver(version: str) -> str:
    version = version.strip()
    if not version or not SEMVER_RE.match(version):
        raise SystemExit(f"invalid SemVer: {version!r}")
    return version


def latest_version_tag() -> str:
    proc = _git(["tag", "-l", "v*", "--sort=-v:refname"])
    if proc.returncode != 0:
        return ""
    for line in proc.stdout.splitlines():
        tag = line.strip()
        if TAG_RE.match(tag):
            return tag
    return ""


def tag_at_head() -> str:
    proc = _git(["tag", "--points-at", "HEAD"])
    if proc.returncode != 0:
        return ""
    for line in proc.stdout.splitlines():
        tag = line.strip()
        if TAG_RE.match(tag):
            return tag
    return ""


def patch_bump(version: str) -> str:
    core, sep, rest = version.partition("-")
    plus = ""
    if "+" in core:
        core, plus = core.split("+", 1)
        plus = "+" + plus
    parts = core.split(".")
    if len(parts) < 3 or not parts[2].isdigit():
        raise SystemExit(f"cannot patch-bump version {version!r}")
    parts[2] = str(int(parts[2]) + 1)
    bumped = ".".join(parts[:3])
    if len(parts) > 3:
        bumped += "." + ".".join(parts[3:])
    return bumped + (sep + rest if sep else "") + plus


def resolve_main_release_version() -> str:
    head_tag = tag_at_head()
    if head_tag:
        return TAG_RE.match(head_tag).group(1)  # type: ignore[union-attr]
    latest = latest_version_tag()
    if not latest:
        raise SystemExit("no v* tags found; tag the current nuget.org line before auto-releasing main")
    return patch_bump(TAG_RE.match(latest).group(1))  # type: ignore[union-attr]


def apply_channel(version: str, *, channel: str, run_number: str) -> str:
    version = validate_semver(version)
    if channel == "release":
        return version
    if "-" in version:
        return version
    suffix = run_number.strip() or "0"
    return f"{version}-preview.{suffix}"


def _write_output(pairs: dict[str, str]) -> None:
    dest = os.environ.get("GITHUB_OUTPUT")
    lines = [f"{k}={v}" for k, v in pairs.items()]
    text = "\n".join(lines) + "\n"
    sys.stdout.write(text)
    if dest:
        Path(dest).open("a", encoding="utf-8").write(text)


def emit_config(args: argparse.Namespace) -> int:
    event = (args.event or os.environ.get("GITHUB_EVENT_NAME") or "workflow_dispatch").strip()
    ref_name = (args.ref_name or os.environ.get("GITHUB_REF_NAME") or "").strip()
    run_number = (args.run_number or os.environ.get("GITHUB_RUN_NUMBER") or "0").strip()
    is_push_main = event == "push" and ref_name == "main"

    channel = (args.channel or "").strip()
    destination = (args.destination or "").strip()
    if channel == "auto":
        channel = ""
    if destination == "auto":
        destination = ""
    scope = (args.scope or "").strip() or "changed"
    stages = (args.stages or "").strip() or "all"
    version_in = (args.version or "").strip()
    dry_run = bool(args.dry_run)

    if is_push_main:
        channel = "release"
        destination = destination or "nuget.org"
        scope = scope or "changed"
        stages = stages or "all"
        version = version_in or resolve_main_release_version()
        version = apply_channel(version, channel="release", run_number=run_number)
    else:
        if not channel:
            channel = "release" if ref_name == "main" else "preview"
        if not destination:
            destination = "nuget.org" if ref_name in {"main", "dev"} else "none"
        if channel == "release" and ref_name != "main":
            raise SystemExit("channel=release is only allowed on main")
        version = apply_channel(
            version_in or resolve_main_release_version(),
            channel=channel,
            run_number=run_number,
        )

    if destination not in {"none", "github", "nuget.org", "both"}:
        raise SystemExit(f"unknown destination: {destination}")
    if channel not in {"preview", "release"}:
        raise SystemExit(f"unknown channel: {channel}")
    if scope not in {"all", "changed", "named"}:
        raise SystemExit(f"unknown scope: {scope}")
    if stages not in {"build", "pack", "build-and-pack", "pack-and-publish", "all"}:
        raise SystemExit(f"unknown stages: {stages}")
    if scope == "named" and not (args.packages or "").strip():
        raise SystemExit("scope=named requires --packages")

    do_build = stages in STAGES_BUILD
    do_pack = stages in STAGES_PACK
    do_publish = stages in STAGES_PUBLISH and not dry_run
    do_publish_nuget = do_publish and destination in {"nuget.org", "both"}
    do_publish_github = do_publish and destination in {"github", "both"}

    _write_output(
        {
            "version": version,
            "channel": channel,
            "destination": destination,
            "scope": scope,
            "stages": stages,
            "do_build": str(do_build).lower(),
            "do_pack": str(do_pack).lower(),
            "do_publish_nuget": str(do_publish_nuget).lower(),
            "do_publish_github": str(do_publish_github).lower(),
            "dry_run": str(dry_run).lower(),
        }
    )
    return 0


def pack(args: argparse.Namespace) -> int:
    channel = args.channel or "preview"
    if channel not in {"preview", "release"}:
        raise SystemExit(f"unknown channel: {channel}")
    if args.resolve_main_version:
        version = resolve_main_release_version()
    else:
        version = args.version or "1.0.0"
    version = apply_channel(version, channel=channel, run_number=args.run_number or os.environ.get("GITHUB_RUN_NUMBER") or "0")
    if args.print_resolved_version:
        print(version)
        return 0

    cmd = [sys.executable, str(BUILD_NUGET), "-v", version]
    if channel == "release":
        cmd.append("--release")
    if args.force:
        cmd.append("--force")
    if args.scope == "changed":
        cmd.append("--changed-since")
        if args.since:
            cmd.append(args.since)
    elif args.scope == "named":
        packages = (args.packages or "").split()
        if not packages:
            raise SystemExit("scope=named requires --packages")
        cmd.extend(packages)
    elif args.scope not in {None, "", "all"}:
        raise SystemExit(f"unknown scope: {args.scope}")

    print("Pack:", " ".join(cmd), flush=True)
    return subprocess.call(cmd, cwd=REPO_ROOT)


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--scope", choices=["all", "changed", "named"], default="all")
    parser.add_argument("--packages", default="", help="Space-separated names/globs when scope=named")
    parser.add_argument("--since", default="", help="Git ref for scope=changed")
    parser.add_argument("--version", default="")
    parser.add_argument("--channel", default="", help="preview or release (empty = infer)")
    parser.add_argument("--destination", default="", help="none, github, nuget.org, or both (empty = infer)")
    parser.add_argument("--stages", default="")
    parser.add_argument("--event", default="")
    parser.add_argument("--ref-name", default="")
    parser.add_argument("--run-number", default="")
    parser.add_argument("--force", action="store_true")
    parser.add_argument("--dry-run", action="store_true")
    parser.add_argument("--resolve-main-version", action="store_true")
    parser.add_argument("--print-resolved-version", action="store_true")
    parser.add_argument("--emit-config", action="store_true")
    args = parser.parse_args(argv)
    if args.emit_config:
        return emit_config(args)
    return pack(args)


if __name__ == "__main__":
    raise SystemExit(main())
