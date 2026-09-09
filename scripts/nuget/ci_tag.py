#!/usr/bin/env python3
"""Create and push a stable v* tag after a nuget.org release.

Only X.Y.Z (no prerelease). If the tag already points at HEAD, this is a no-op.
Refuses to move a tag that points at a different commit.

Usage:
  python3 scripts/nuget/ci_tag.py --version 1.0.5
  python3 scripts/nuget/ci_tag.py --version 1.0.5 --no-push
"""

from __future__ import annotations

import argparse
import re
import subprocess
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[2]
STABLE_RE = re.compile(r"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)$")


def _git(args: list[str]) -> subprocess.CompletedProcess[str]:
    return subprocess.run(["git", "-C", str(REPO_ROOT), *args], capture_output=True, text=True, check=False)


def _require_ok(proc: subprocess.CompletedProcess[str], action: str) -> str:
    if proc.returncode != 0:
        err = (proc.stderr or proc.stdout or "").strip() or f"exit {proc.returncode}"
        raise SystemExit(f"{action}: {err}")
    return (proc.stdout or "").strip()


def _head() -> str:
    return _require_ok(_git(["rev-parse", "HEAD"]), "git rev-parse HEAD")


def _tag_commit(tag: str) -> str:
    proc = _git(["rev-parse", "-q", "--verify", f"{tag}^{{commit}}"])
    if proc.returncode != 0:
        return ""
    return (proc.stdout or "").strip()


def _remote_tag_commit(remote: str, tag: str) -> str:
    proc = _git(["ls-remote", "--tags", remote, f"refs/tags/{tag}"])
    if proc.returncode != 0:
        return ""
    peeled = ""
    direct = ""
    for line in proc.stdout.splitlines():
        parts = line.split()
        if len(parts) < 2:
            continue
        sha, ref = parts[0], parts[1]
        if ref.endswith("^{}"):
            peeled = sha
        else:
            direct = sha
    return peeled or direct


def _ensure_identity() -> None:
    name = _git(["config", "user.name"])
    email = _git(["config", "user.email"])
    if name.returncode != 0 or not (name.stdout or "").strip():
        _git(["config", "user.name", "github-actions[bot]"])
    if email.returncode != 0 or not (email.stdout or "").strip():
        _git(["config", "user.email", "41898282+github-actions[bot]@users.noreply.github.com"])


def tag_release(version: str, *, push: bool, remote: str) -> int:
    version = version.strip()
    if not STABLE_RE.match(version):
        raise SystemExit(f"refusing to tag prerelease or invalid version {version!r}; expected X.Y.Z")
    tag = f"v{version}"
    head = _head()
    existing = _tag_commit(tag)
    if existing and existing != head:
        raise SystemExit(f"tag {tag} points at {existing[:12]}, not HEAD {head[:12]}")
    if not existing:
        _ensure_identity()
        _require_ok(_git(["tag", "-a", tag, "-m", tag]), f"git tag {tag}")
        print(f"created {tag} at {head[:12]}")
    else:
        print(f"tag {tag} already points at HEAD")

    if not push:
        return 0

    remote_commit = _remote_tag_commit(remote, tag)
    if remote_commit == head:
        print(f"{tag} already on {remote} at HEAD")
        return 0
    if remote_commit and remote_commit != head:
        raise SystemExit(f"tag {tag} on {remote} points at {remote_commit[:12]}, not HEAD {head[:12]}")

    proc = _git(["push", remote, f"refs/tags/{tag}"])
    if proc.returncode != 0:
        err = (proc.stderr or proc.stdout or "").strip()
        raise SystemExit(f"git push {remote} {tag}: {err}")
    print(f"pushed {tag} to {remote}")
    return 0


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--version", required=True, help="Stable SemVer to tag as vX.Y.Z")
    parser.add_argument("--remote", default="origin")
    parser.add_argument("--no-push", action="store_true", help="Create the tag locally only")
    args = parser.parse_args(argv)
    return tag_release(args.version, push=not args.no_push, remote=args.remote)


if __name__ == "__main__":
    raise SystemExit(main())
