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
  python3 scripts/nuget/build_nuget.py --compile-only Lyo.Encryption
  python3 scripts/nuget/build_nuget.py --pack-only Lyo.Encryption

Local packs append the SemVer prerelease label ``preview`` (e.g. 1.0.0-preview)
so they are distinct from release packages. Pass --release when publishing so
the version is used as-is (1.0.0).

Selection is the named patterns, ``--changed-since`` hits, or every packable
``Lyo.*`` project. The packer does not walk ProjectReferences in either
direction: changing Encryption packs Encryption only; changing Common packs
Common only. Lyo ProjectReferences in the nupkg are pinned to the dependency's
last published version unless that dependency is also in this pack set.
Those same pins are used as each dependency's ``Version`` / assembly version
at compile time. A global ``/p:Version`` on ``dotnet build`` would stamp every
ProjectReference at the new version, so Fusion 1.0.5 would load Exceptions
1.0.5.0 while NuGet restored Exceptions 1.0.4.

Change detection:
  Each project's source directory is fingerprinted using git (committed state +
  staged/unstaged diffs + untracked file contents). Matching fingerprints skip
  rebuild. Use -f / --force to always rebuild.
  ``--changed-since [REF]`` selects packable projects whose directory changed
  since REF (latest tag, or HEAD~1, when REF is omitted). Shared
  Directory.Build.props / Directory.Packages.props / package icon changes
  select all packages.

Environment:
  NUGET_OUTPUT_DIR  Output directory (default: ~/nuget-local)
  BUILD_CONFIG      Build configuration (default: Release)
"""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
import subprocess
import sys
import urllib.error
import urllib.request
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
    "Lyo.Net/assets/icon.png",
    "Lyo.Net/assets/icon.svg",
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
        print_warning(f"Shared Directory.Build/Packages.props or package icon changed since {since}; selecting all packages")
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


def _stable_versions(versions: list[str]) -> list[str]:
    return [v for v in versions if v and "-" not in v]


def _nuget_org_latest(package_id: str) -> str:
    url = f"https://api.nuget.org/v3-flatcontainer/{package_id.lower()}/index.json"
    try:
        with urllib.request.urlopen(url, timeout=15) as resp:
            data = json.loads(resp.read().decode("utf-8"))
    except (urllib.error.URLError, TimeoutError, json.JSONDecodeError, OSError):
        return ""
    versions = [str(v) for v in (data.get("versions") or []) if v]
    if not versions:
        return ""
    stable = _stable_versions(versions)
    return (stable or versions)[-1]


def _github_packages_latest(package_id: str) -> str:
    owner = os.environ.get("GITHUB_REPOSITORY_OWNER") or os.environ.get("GITHUB_PACKAGES_OWNER") or ""
    token = os.environ.get("GITHUB_TOKEN") or os.environ.get("GITHUB_PACKAGES_TOKEN") or ""
    if not owner or not token:
        return ""
    url = f"https://nuget.pkg.github.com/{owner}/download/{package_id.lower()}/index.json"
    req = urllib.request.Request(url, headers={"Authorization": f"Bearer {token}", "Accept": "application/json"})
    try:
        with urllib.request.urlopen(req, timeout=15) as resp:
            data = json.loads(resp.read().decode("utf-8"))
    except (urllib.error.URLError, TimeoutError, json.JSONDecodeError, OSError):
        return ""
    versions = [str(v) for v in (data.get("versions") or []) if v]
    if not versions:
        return ""
    stable = _stable_versions(versions)
    return (stable or versions)[-1]


def last_published_version_optional(package_id: str, state_file: Path) -> str | None:
    """Last known version: local .build-state, then nuget.org, then GitHub Packages."""
    _, stored = _read_state_entry(state_file, package_id)
    if stored:
        return stored
    nuget_ver = _nuget_org_latest(package_id)
    if nuget_ver:
        return nuget_ver
    github_ver = _github_packages_latest(package_id)
    if github_ver:
        return github_ver
    return None


def last_published_version(package_id: str, state_file: Path, fallback: str) -> str:
    return last_published_version_optional(package_id, state_file) or fallback


def project_reference_closure(roots: list[Path]) -> list[Path]:
    seen: set[Path] = set()
    stack = [p.resolve() for p in roots]
    while stack:
        proj = stack.pop()
        if proj in seen:
            continue
        seen.add(proj)
        stack.extend(d.resolve() for d in get_project_dependencies(proj))
    return list(seen)


def expand_pack_set_unpublished_deps(pack_set: list[Path], *, state_file: Path) -> list[Path]:
    """First-time Lyo ProjectReferences must ship in this pack or assembly versions will not exist on the feed."""
    pack_resolved = [p.resolve() for p in pack_set]
    extra: list[Path] = []
    for proj in project_reference_closure(pack_resolved):
        if proj in pack_resolved or proj in extra:
            continue
        name = get_project_name(proj)
        if last_published_version_optional(name, state_file) is None:
            print_warning(f"{name} has never been published; adding it to this pack set")
            extra.append(proj)
    return pack_set + extra


def collect_compile_versions(pack_set: list[Path], *, version: str, state_file: Path) -> dict[str, str]:
    """Pack-set projects get ``version``; other compiled ProjectReferences keep last published."""
    pack_names = {get_project_name(p) for p in pack_set}
    versions: dict[str, str] = {}
    for proj in project_reference_closure(pack_set):
        name = get_project_name(proj)
        if name in pack_names:
            versions[name] = version
            continue
        versions[name] = last_published_version_optional(name, state_file) or version
    return versions


def write_pack_version_props(path: Path, versions: dict[str, str]) -> None:
    """Per-project Version so ``dotnet build`` does not stamp the whole graph at the pack version."""
    path.parent.mkdir(parents=True, exist_ok=True)
    lines = [
        "<!-- Generated by build_nuget.py. Do not pass /p:Version with this file. -->",
        "<Project>",
    ]
    for name, ver in sorted(versions.items()):
        safe_name = name.replace("'", "")
        file_ver = file_version_from_package_version(ver)
        lines.append(f"  <PropertyGroup Condition=\"'$(MSBuildProjectName)' == '{safe_name}'\">")
        lines.append(f"    <Version>{ver}</Version>")
        lines.append(f"    <InformationalVersion>{ver}</InformationalVersion>")
        lines.append(f"    <FileVersion>{file_ver}</FileVersion>")
        lines.append("  </PropertyGroup>")
    lines.extend(["</Project>", ""])
    path.write_text("\n".join(lines), encoding="utf-8")


def collect_dependency_pins(pack_set: list[Path], *, version: str, state_file: Path) -> dict[str, str]:
    pack_names = {get_project_name(p) for p in pack_set}
    pins: dict[str, str] = {}
    for proj in pack_set:
        for dep in get_project_dependencies(proj):
            name = get_project_name(dep)
            if name in pins:
                continue
            pins[name] = version if name in pack_names else last_published_version(name, state_file, version)
    return pins


def write_dependency_version_targets(path: Path, pins: dict[str, str]) -> None:
    """Generate an MSBuild targets file that pins ProjectReference nupkg versions."""
    path.parent.mkdir(parents=True, exist_ok=True)
    lines = [
        '<Project>',
        '  <Target Name="ApplyLyoDependencyVersions"',
        '          AfterTargets="_GetProjectReferenceVersions"',
        '          BeforeTargets="GenerateNuspec">',
        '    <ItemGroup>',
    ]
    for name, ver in sorted(pins.items()):
        safe_name = name.replace("'", "")
        safe_ver = ver.replace("'", "")
        lines.append(
            f"""      <_ProjectReferencesWithVersions Update="*" Condition="'%(Filename)' == '{safe_name}'">"""
        )
        lines.append(f"        <ProjectVersion>{safe_ver}</ProjectVersion>")
        lines.append("      </_ProjectReferencesWithVersions>")
    lines.extend(
        [
            "    </ItemGroup>",
            "  </Target>",
            "</Project>",
            "",
        ]
    )
    path.write_text("\n".join(lines), encoding="utf-8")


def _version_msbuild_props(version: str) -> list[str]:
    return [
        f"/p:Version={version}",
        f"/p:InformationalVersion={version}",
        f"/p:FileVersion={file_version_from_package_version(version)}",
    ]


def _msbuild_version_args(version: str, version_props: Path | None) -> list[str]:
    # Global /p:Version flows to every ProjectReference and breaks subset packs
    # (nupkg pin says 1.0.4, compiled assembly asks for 1.0.5.0).
    if version_props is not None:
        return [f"/p:LyoPackVersionProps={version_props}"]
    return _version_msbuild_props(version)


def build_project(
    csproj_file: Path,
    *,
    version: str,
    config: str,
    incremental: bool,
    version_props: Path | None = None,
) -> bool:
    name = get_project_name(csproj_file)
    label = "incremental" if incremental else "version " + version
    print_info(f"Building {name} ({label})...")
    # Leave ProjectReferences on so upstream libs compile; they are not packed.
    cmd = ["dotnet", "build", str(csproj_file), "-c", config, *_msbuild_version_args(version, version_props)]
    if not incremental:
        # After the full argv — never insert next to -c or MSBuild treats the flag as Configuration.
        cmd.append("--no-incremental")
    ok = subprocess.run(cmd, cwd=LYO_NET).returncode == 0
    if ok:
        print_success(f"Built {name}" + (" (incremental)" if incremental else ""))
    else:
        print_error(f"Failed to build {name}")
    return ok


def has_build_output(csproj_file: Path, config: str) -> bool:
    bin_dir = csproj_file.parent / "bin" / config
    return bin_dir.is_dir() and any(bin_dir.rglob("*.dll"))


def pack_project(
    csproj_file: Path,
    *,
    version: str,
    config: str,
    output_dir: Path,
    dep_targets: Path | None,
    version_props: Path | None = None,
) -> bool:
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
        *_msbuild_version_args(version, version_props),
        "/p:BuildProjectReferences=false",
        "/p:SkipToolingDocsOnPack=true",
    ]
    if dep_targets is not None:
        cmd.append(f"/p:LyoDependencyVersionTargets={dep_targets}")
    ok = subprocess.run(cmd, cwd=LYO_NET).returncode == 0
    if ok:
        print_success(f"Packed {name}")
    else:
        print_error(f"Failed to pack {name}")
    return ok


class Builder:
    def __init__(
        self,
        *,
        version: str,
        force: bool,
        config: str,
        output_dir: Path,
        dep_targets: Path | None,
        version_props: Path | None = None,
        do_compile: bool = True,
        do_pack: bool = True,
    ) -> None:
        self.version = version
        self.force = force
        self.config = config
        self.output_dir = output_dir
        self.dep_targets = dep_targets
        self.version_props = version_props
        self.do_compile = do_compile
        self.do_pack = do_pack
        self.state_file = output_dir / ".build-state"
        self.visited: set[Path] = set()
        self.failed: set[Path] = set()
        self.skipped: list[str] = []
        self.pack_only: list[str] = []
        self.built: list[str] = []

    def build_and_pack(self, csproj_file: Path) -> bool:
        name = get_project_name(csproj_file)
        project_dir = csproj_file.parent

        if csproj_file in self.visited:
            return csproj_file not in self.failed

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

        if self.do_compile:
            if source_changed:
                if not build_project(
                    csproj_file,
                    version=self.version,
                    config=self.config,
                    incremental=False,
                    version_props=self.version_props,
                ):
                    self.failed.add(csproj_file)
                    self.visited.add(csproj_file)
                    return False
            else:
                print_pack_only(f"{name} — source unchanged, rebuilding for new version {self.version}")
                if not build_project(
                    csproj_file,
                    version=self.version,
                    config=self.config,
                    incremental=True,
                    version_props=self.version_props,
                ):
                    self.failed.add(csproj_file)
                    self.visited.add(csproj_file)
                    return False
        elif not has_build_output(csproj_file, self.config):
            print_error(f"{name} has no {self.config} build output; run --compile-only first")
            self.failed.add(csproj_file)
            self.visited.add(csproj_file)
            return False

        if self.do_pack:
            if not pack_project(
                csproj_file,
                version=self.version,
                config=self.config,
                output_dir=self.output_dir,
                dep_targets=self.dep_targets,
                version_props=self.version_props,
            ):
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
    phase = parser.add_mutually_exclusive_group()
    phase.add_argument("--compile-only", action="store_true", help="Build selected projects; do not pack")
    phase.add_argument("--pack-only", action="store_true", help="Pack selected projects without compiling (requires prior --compile-only)")
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
    if args.compile_only:
        print_info("Compile only (no pack)")
    if args.pack_only:
        print_info("Pack only (no compile)")
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

    state_file = output_dir / ".build-state"
    projects_to_build = expand_pack_set_unpublished_deps(projects_to_build, state_file=state_file)

    print_info(f"Found {len(projects_to_build)} project(s) to pack (selected only, no graph walk):")
    for project in projects_to_build:
        print(f"  - {get_project_name(project)}")
    print()

    pins = collect_dependency_pins(projects_to_build, version=version, state_file=state_file)
    dep_targets: Path | None = None
    if pins:
        dep_targets = output_dir / "lyo-dep-versions.targets"
        write_dependency_version_targets(dep_targets, pins)
        print_info("Lyo ProjectReference pins (last published unless also in this pack set):")
        for name, ver in sorted(pins.items()):
            print(f"  - {name} -> {ver}")
        print()

    compile_versions = collect_compile_versions(projects_to_build, version=version, state_file=state_file)
    version_props = output_dir / "lyo-pack-versions.props"
    write_pack_version_props(version_props, compile_versions)
    print_info("Compile versions (this pack set vs last published ProjectReferences):")
    for name, ver in sorted(compile_versions.items()):
        print(f"  - {name} -> {ver}")
    print()

    builder = Builder(
        version=version,
        force=args.force,
        config=config,
        output_dir=output_dir,
        dep_targets=dep_targets,
        version_props=version_props,
        do_compile=not args.pack_only,
        do_pack=not args.compile_only,
    )
    failed_projects: list[str] = []
    for project in projects_to_build:
        if project in builder.visited:
            continue
        if not builder.build_and_pack(project):
            failed_projects.append(get_project_name(project))
            print_warning(f"Failed: {get_project_name(project)}")

    print()
    print_info("Build Summary:")
    if args.compile_only:
        print_success(f"Built (source changed): {len(builder.built)} package(s)")
        print_pack_only(f"Rebuilt for new version (same source): {len(builder.pack_only)} package(s)")
    elif args.pack_only:
        print_success(f"Packed (source changed): {len(builder.built)} package(s)")
        print_pack_only(f"Packed new version (same source): {len(builder.pack_only)} package(s)")
    else:
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
