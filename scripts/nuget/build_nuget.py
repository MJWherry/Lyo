#!/usr/bin/env python3
"""Build and pack NuGet packages for Lyo libraries.

Usage:
  python3 scripts/nuget/build_nuget.py
  python3 scripts/nuget/build_nuget.py -v 2.0.0
  python3 scripts/nuget/build_nuget.py --release
  python3 scripts/nuget/build_nuget.py -v 2.0.0 --release
  python3 scripts/nuget/build_nuget.py Lyo.Encryption
  python3 scripts/nuget/build_nuget.py -v 1.5.0 Lyo.Encryption
  python3 scripts/nuget/build_nuget.py -f Lyo.Encryption
  python3 scripts/nuget/build_nuget.py --release --changed-since
  python3 scripts/nuget/build_nuget.py --release --changed-since v1.0.0

Local packs append the SemVer prerelease label ``preview`` (e.g. 1.0.0-preview)
so they are distinct from release packages. Pass --release when publishing so
the version is used as-is (1.0.0).

Change detection:
  Each project's source directory is fingerprinted using git (committed state +
  staged/unstaged diffs + untracked file contents). Matching fingerprints skip
  rebuild. Use -f / --force to always rebuild.
  ``--changed-since [REF]`` selects packable projects whose directory changed
  since REF (latest tag, or HEAD~1, when REF is omitted). Shared
  Directory.Build.props / Directory.Packages.props changes select all packages.

Environment:
  NUGET_OUTPUT_DIR  Output directory (default: ~/nuget-local)
  BUILD_CONFIG      Build configuration (default: Release)
"""

from __future__ import annotations

import argparse
import hashlib
import os
import re
import subprocess
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[2]
LYO_NET = REPO_ROOT / "Lyo.Net"
TOOLING_DOCS = REPO_ROOT / "scripts" / "docs" / "tooling-docs.py"

BLUE = "\033[0;34m"
GREEN = "\033[0;32m"
YELLOW = "\033[1;33m"
RED = "\033[0;31m"
CYAN = "\033[0;36m"
MAGENTA = "\033[0;35m"
NC = "\033[0m"


def print_info(msg: str) -> None:
    print(f"{BLUE}[INFO]{NC} {msg}")


def print_success(msg: str) -> None:
    print(f"{GREEN}[SUCCESS]{NC} {msg}")


def print_warning(msg: str) -> None:
    print(f"{YELLOW}[WARNING]{NC} {msg}")


def print_error(msg: str) -> None:
    print(f"{RED}[ERROR]{NC} {msg}")


def print_skip(msg: str) -> None:
    print(f"{CYAN}[SKIP]{NC} {msg}")


def print_pack_only(msg: str) -> None:
    print(f"{MAGENTA}[PACK]{NC} {msg}")


def get_project_name(project_file: Path) -> str:
    return project_file.stem


def compute_project_hash(project_dir: Path) -> str:
    h = hashlib.sha256()
    try:
        commit = subprocess.run(
            ["git", "-C", str(LYO_NET), "log", "-1", "--format=%H", "--", str(project_dir)],
            capture_output=True,
            text=True,
            check=False,
        )
        h.update((commit.stdout.strip() or "no-commits").encode())
    except OSError:
        h.update(b"no-commits")

    diff = subprocess.run(
        ["git", "-C", str(LYO_NET), "diff", "HEAD", "--", str(project_dir)],
        capture_output=True,
        check=False,
    )
    h.update(diff.stdout or b"")

    untracked = subprocess.run(
        ["git", "-C", str(LYO_NET), "ls-files", "--others", "--exclude-standard", "--", str(project_dir)],
        capture_output=True,
        text=True,
        check=False,
    )
    for rel in (untracked.stdout or "").splitlines():
        if not rel:
            continue
        h.update(f"untracked:{rel}".encode())
        path = LYO_NET / rel
        try:
            h.update(path.read_bytes())
        except OSError:
            pass
    return h.hexdigest()


def _read_state_entry(state_file: Path, project_name: str) -> tuple[str, str]:
    if not state_file.is_file():
        return "", ""
    prefix = f"{project_name}="
    last = ""
    for line in state_file.read_text(encoding="utf-8").splitlines():
        if line.startswith(prefix):
            last = line[len(prefix):]
    if not last:
        return "", ""
    if ":" in last:
        src_hash, version = last.split(":", 1)
        return src_hash, version
    return last, ""


def save_project_state(state_file: Path, project_name: str, src_hash: str, version: str) -> None:
    state_file.parent.mkdir(parents=True, exist_ok=True)
    lines: list[str] = []
    if state_file.is_file():
        lines = [ln for ln in state_file.read_text(encoding="utf-8").splitlines() if not ln.startswith(f"{project_name}=")]
    lines.append(f"{project_name}={src_hash}:{version}")
    state_file.write_text("\n".join(lines) + "\n", encoding="utf-8")


def _should_skip_project(path: Path) -> bool:
    name = get_project_name(path)
    parts = set(path.parts)
    if ".Tests" in name or ".Benchmarks" in name or "TestConsole" in name or name.endswith(".Host"):
        return True
    if name.startswith("Lyo.TestConsole"):
        return True
    if "Tests" in parts or "Benchmarks" in parts or "Tools" in parts:
        return True
    return False


def find_projects(pattern: str) -> list[Path]:
    regex = re.compile("^" + re.escape(pattern).replace(r"\*", ".*") + "$")
    found: list[Path] = []
    for path in sorted(LYO_NET.rglob("*.csproj")) + sorted(LYO_NET.rglob("*.fsproj")):
        if _should_skip_project(path):
            continue
        if regex.match(get_project_name(path)):
            found.append(path)
    return found


SHARED_PACK_TRIGGERS = (
    "Lyo.Net/Directory.Build.props",
    "Lyo.Net/Directory.Build.targets",
    "Lyo.Net/Directory.Packages.props",
)


def _git(args: list[str]) -> subprocess.CompletedProcess[str]:
    return subprocess.run(["git", "-C", str(REPO_ROOT), *args], capture_output=True, text=True, check=False)


def default_changed_since() -> str:
    tag = _git(["describe", "--tags", "--abbrev=0"]).stdout.strip()
    if tag:
        return tag
    return "HEAD~1"


def git_changed_paths(since: str) -> list[str]:
    proc = _git(["diff", "--name-only", "--diff-filter=ACDMR", f"{since}...HEAD"])
    if proc.returncode != 0:
        err = (proc.stderr or proc.stdout).strip() or f"exit {proc.returncode}"
        raise SystemExit(f"git diff failed for --changed-since {since!r}: {err}")
    return [ln.strip().replace("\\", "/") for ln in proc.stdout.splitlines() if ln.strip()]


def find_changed_projects(since: str) -> list[Path]:
    paths = git_changed_paths(since)
    if any(p in SHARED_PACK_TRIGGERS for p in paths):
        print_warning(f"Shared Directory.Build/Packages.props changed since {since}; selecting all packages")
        return find_projects("Lyo.*")
    selected: list[Path] = []
    for proj in find_projects("Lyo.*"):
        rel_dir = proj.parent.relative_to(REPO_ROOT).as_posix()
        rel_file = proj.relative_to(REPO_ROOT).as_posix()
        if any(p == rel_file or p.startswith(rel_dir + "/") for p in paths):
            selected.append(proj)
    return selected


_PROJECT_REF_RE = re.compile(r'ProjectReference\s+Include="([^"]+)"')
PRERELEASE_LABEL = "preview"


def resolve_package_version(version: str, *, release: bool) -> str:
    """Return the NuGet version. Local packs get ``-preview`` unless already prerelease."""
    version = version.strip()
    if release or "-" in version:
        return version
    return f"{version}-{PRERELEASE_LABEL}"


def file_version_from_package_version(version: str) -> str:
    """Win32 FileVersion is numeric-only; drop any SemVer prerelease label."""
    return version.split("-", 1)[0]


def get_project_dependencies(csproj_file: Path) -> list[Path]:
    deps: list[Path] = []
    text = csproj_file.read_text(encoding="utf-8", errors="replace")
    project_dir = csproj_file.parent
    for match in _PROJECT_REF_RE.finditer(text):
        ref_path = match.group(1).replace("\\", "/")
        ref_full = Path(ref_path) if Path(ref_path).is_absolute() else (project_dir / ref_path).resolve()
        if not ref_full.is_file():
            continue
        if not get_project_name(ref_full).startswith("Lyo."):
            continue
        if ref_full not in deps:
            deps.append(ref_full)
    return deps


def get_full_build_set(projects: list[Path]) -> list[Path]:
    result: list[Path] = []
    seen: set[Path] = set()

    def add_with_deps(csproj_file: Path) -> None:
        if csproj_file in seen:
            return
        seen.add(csproj_file)
        for dep in get_project_dependencies(csproj_file):
            add_with_deps(dep)
        result.append(csproj_file)

    for project in projects:
        add_with_deps(project)
    return result


def _version_msbuild_props(version: str) -> list[str]:
    return [
        f"/p:Version={version}",
        f"/p:InformationalVersion={version}",
        f"/p:FileVersion={file_version_from_package_version(version)}",
    ]


def build_project(csproj_file: Path, *, version: str, config: str, incremental: bool) -> bool:
    name = get_project_name(csproj_file)
    label = "incremental" if incremental else "version " + version
    print_info(f"Building {name} ({label})...")
    cmd = ["dotnet", "build", str(csproj_file), "-c", config, "/p:BuildProjectReferences=false", *_version_msbuild_props(version)]
    if not incremental:
        # After the full argv — never insert next to -c or MSBuild treats the flag as Configuration.
        cmd.append("--no-incremental")
    ok = subprocess.run(cmd, cwd=LYO_NET).returncode == 0
    if ok:
        print_success(f"Built {name}" + (" (incremental)" if incremental else ""))
    else:
        print_error(f"Failed to build {name}")
    return ok


def pack_project(csproj_file: Path, *, version: str, config: str, output_dir: Path) -> bool:
    name = get_project_name(csproj_file)
    print_info(f"Packing {name} (version {version})...")
    cmd = [
        "dotnet",
        "pack",
        str(csproj_file),
        "-c",
        config,
        "--no-build",
        "--output",
        str(output_dir),
        *_version_msbuild_props(version),
        "/p:BuildProjectReferences=false",
        "/p:SkipToolingDocsOnPack=true",
    ]
    ok = subprocess.run(cmd, cwd=LYO_NET).returncode == 0
    if ok:
        print_success(f"Packed {name}")
    else:
        print_error(f"Failed to pack {name}")
    return ok


class Builder:
    def __init__(self, *, version: str, force: bool, config: str, output_dir: Path) -> None:
        self.version = version
        self.force = force
        self.config = config
        self.output_dir = output_dir
        self.state_file = output_dir / ".build-state"
        self.visited: set[Path] = set()
        self.failed: set[Path] = set()
        self.skipped: list[str] = []
        self.pack_only: list[str] = []
        self.built: list[str] = []

    def build_and_pack_with_deps(self, csproj_file: Path) -> bool:
        name = get_project_name(csproj_file)
        project_dir = csproj_file.parent

        if csproj_file in self.visited:
            return csproj_file not in self.failed

        for dep in get_project_dependencies(csproj_file):
            if not dep.is_file():
                continue
            if not self.build_and_pack_with_deps(dep):
                print_error(f"Failed to build dependency: {get_project_name(dep)}")
                self.failed.add(csproj_file)
                self.visited.add(csproj_file)
                return False

        source_changed = True
        version_changed = True
        if not self.force:
            stored_hash, stored_version = _read_state_entry(self.state_file, name)
            current_hash = compute_project_hash(project_dir)
            source_changed = current_hash != stored_hash
            version_changed = self.version != stored_version

        if not source_changed and not version_changed:
            print_skip(f"{name} — source and version unchanged")
            self.skipped.append(name)
            self.visited.add(csproj_file)
            return True

        if source_changed:
            if not build_project(csproj_file, version=self.version, config=self.config, incremental=False):
                self.failed.add(csproj_file)
                self.visited.add(csproj_file)
                return False
        else:
            print_pack_only(f"{name} — source unchanged, rebuilding for new version {self.version} then packing")
            if not build_project(csproj_file, version=self.version, config=self.config, incremental=True):
                self.failed.add(csproj_file)
                self.visited.add(csproj_file)
                return False

        if not pack_project(csproj_file, version=self.version, config=self.config, output_dir=self.output_dir):
            self.failed.add(csproj_file)
            self.visited.add(csproj_file)
            return False

        save_project_state(self.state_file, name, compute_project_hash(project_dir), self.version)
        if source_changed:
            self.built.append(name)
        else:
            self.pack_only.append(name)
        self.visited.add(csproj_file)
        return True


def _refresh_tooling_docs() -> None:
    if not TOOLING_DOCS.is_file():
        return
    print_info("Refreshing tooling docs...")
    rc = subprocess.run([sys.executable, str(TOOLING_DOCS), "render"], cwd=REPO_ROOT).returncode
    if rc != 0:
        print_warning("tooling-docs render failed (continuing with pack)")


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("-v", "--version", default="1.0.0", help="Package version prefix (default: 1.0.0)")
    parser.add_argument(
        "--release",
        action="store_true",
        help="Pack a release version with no prerelease label (for deploy/publish). Local packs default to VERSION-preview.",
    )
    parser.add_argument("-f", "--force", action="store_true", help="Force build ignoring change detection")
    parser.add_argument(
        "--changed-since",
        nargs="?",
        const="",
        default=None,
        metavar="REF",
        help="Only pack projects whose directory changed since REF. Omit REF to use the latest git tag, or HEAD~1 if untagged.",
    )
    parser.add_argument("patterns", nargs="*", help="Project name patterns (glob-style *)")
    args = parser.parse_args(argv)

    output_dir = Path(os.environ.get("NUGET_OUTPUT_DIR", str(Path.home() / "nuget-local"))).expanduser()
    config = os.environ.get("BUILD_CONFIG", "Release")
    version = resolve_package_version(args.version, release=args.release)
    changed_since = None if args.changed_since is None else (args.changed_since.strip() or default_changed_since())

    _refresh_tooling_docs()

    print_info("Lyo NuGet Package Builder")
    print_info(f"Output directory: {output_dir}")
    print_info(f"Build configuration: {config}")
    print_info(f"Package version: {version}" + (" (release)" if args.release else " (local preview)"))
    if changed_since:
        print_info(f"Changed since: {changed_since}")
    if args.force:
        print_warning("Change detection disabled (--force)")
    print()

    output_dir.mkdir(parents=True, exist_ok=True)

    projects_to_build: list[Path] = []
    if changed_since is not None:
        print_info(f"Finding packable projects changed since {changed_since}...")
        changed = find_changed_projects(changed_since)
        if args.patterns:
            wanted: set[Path] = set()
            for pattern in args.patterns:
                wanted.update(find_projects(pattern))
            projects_to_build = [p for p in changed if p in wanted]
        else:
            projects_to_build = changed
        if not projects_to_build:
            print_warning(f"No packable packages changed since {changed_since}")
            return 0
    elif not args.patterns:
        print_info("No pattern specified, building all packages...")
        projects_to_build = find_projects("Lyo.*")
    else:
        for pattern in args.patterns:
            print_info(f"Finding projects matching pattern: {pattern}")
            for project in find_projects(pattern):
                if project not in projects_to_build:
                    projects_to_build.append(project)

    if not projects_to_build:
        print_warning("No projects found matching the specified pattern(s)")
        return 1

    full_set = get_full_build_set(projects_to_build)
    print_info(f"Found {len(projects_to_build)} project(s) to evaluate ({len(full_set)} including deps):")
    for project in projects_to_build:
        print(f"  - {get_project_name(project)}")
    print()

    builder = Builder(version=version, force=args.force, config=config, output_dir=output_dir)
    failed_projects: list[str] = []
    for project in projects_to_build:
        if project in builder.visited:
            continue
        if not builder.build_and_pack_with_deps(project):
            failed_projects.append(get_project_name(project))
            print_warning(f"Skipping remaining dependents of {get_project_name(project)}")

    print()
    print_info("Build Summary:")
    print_success(f"Built and packed (source changed): {len(builder.built)} package(s)")
    print_pack_only(f"Rebuild + pack (new version, same source): {len(builder.pack_only)} package(s)")
    print_skip(f"Skipped (source and version unchanged): {len(builder.skipped)} package(s)")
    for name in builder.pack_only:
        print(f"  - {name}")
    for name in builder.skipped:
        print(f"  - {name}")

    if failed_projects:
        print()
        print_warning("Failed to build:")
        for failed in failed_projects:
            print(f"  - {failed}")
        print_info("Note: This may be due to SDK version mismatches (e.g., targeting net10.0 with SDK 8.0)")
        print_info(f"Packages are available in: {output_dir}")
        return 1

    print_success(f"Done! Packages are available in: {output_dir}")
    if not args.force:
        print_info("Tip: run with -f / --force to rebuild all packages regardless of changes.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
