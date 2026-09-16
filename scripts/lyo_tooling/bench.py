"""Shared BenchmarkDotNet suite map for run/manifest/docker tooling."""

from __future__ import annotations

from pathlib import Path

from lyo_tooling.dotnet import REPO_ROOT

NET_DIR = REPO_ROOT / "Lyo.Net"
BENCH_DATA_DIR = REPO_ROOT / "docs" / "benchmarks" / "data"
BENCH_HISTORY_DIR = REPO_ROOT / "docs" / "benchmarks" / "history"


def gateway_public_benchmarks_dir() -> Path | None:
    """Next.js public/benchmarks in this repo, ``LYO_PUBLIC_ROOT``, or sibling ``Lyo-Public``."""
    import os

    env = os.environ.get("LYO_PUBLIC_ROOT", "").strip()
    candidates: list[Path] = []
    if env:
        candidates.append(Path(env).expanduser().resolve() / "apps" / "gateway" / "public" / "benchmarks")
    candidates.append(REPO_ROOT / "apps" / "gateway" / "public" / "benchmarks")
    candidates.append(REPO_ROOT.parent / "Lyo-Public" / "apps" / "gateway" / "public" / "benchmarks")
    for dest in candidates:
        if dest.parent.parent.is_dir():
            return dest
    return None

# category -> csproj path relative to Lyo.Net
BDN_PROJECTS: dict[str, str] = {
    "encryption": "Security/Encryption/Lyo.Encryption.Benchmarks/Lyo.Encryption.Benchmarks.csproj",
    "compression": "Data/Compression/Lyo.Compression.Benchmarks/Lyo.Compression.Benchmarks.csproj",
    "hashing": "Security/Hashing/Lyo.Hashing.Benchmarks/Lyo.Hashing.Benchmarks.csproj",
    "cache": "Core/Cache/Lyo.Cache.Benchmarks/Lyo.Cache.Benchmarks.csproj",
    "query": "Data/Query/Lyo.Query.Benchmarks/Lyo.Query.Benchmarks.csproj",
    "csv": "Data/Csv/Lyo.Csv.Benchmarks/Lyo.Csv.Benchmarks.csproj",
    "json": "Data/Json/Lyo.Json.Benchmarks/Lyo.Json.Benchmarks.csproj",
    "yaml": "Data/Yaml/Lyo.Yaml.Benchmarks/Lyo.Yaml.Benchmarks.csproj",
    "xml": "Data/Xml/Lyo.Xml.Benchmarks/Lyo.Xml.Benchmarks.csproj",
    "toml": "Data/Toml/Lyo.Toml.Benchmarks/Lyo.Toml.Benchmarks.csproj",
    "xlsx": "Data/Xlsx/Lyo.Xlsx.Benchmarks/Lyo.Xlsx.Benchmarks.csproj",
    "lock": "Core/Lock/Lyo.Lock.Benchmarks/Lyo.Lock.Benchmarks.csproj",
    "filestorage": "Data/FileStorage/Lyo.FileStorage.Benchmarks/Lyo.FileStorage.Benchmarks.csproj",
    "filesystemwatcher": "Data/FileSystemWatcher/Lyo.FileSystemWatcher.Benchmarks/Lyo.FileSystemWatcher.Benchmarks.csproj",
}

ALL_BDN_CATEGORIES = tuple(BDN_PROJECTS.keys())

# When --no-docker: positive BDN --filter globs (BDN has no exclusion flag).
NODOCKER_FILTERS: dict[str, tuple[str, ...]] = {
    "cache": ("*PayloadCacheBenchmarks*", "*CacheComparisonBenchmarks*"),
    "query": ("*WhereClauseBenchmarks*", "*SortBenchmarks*", "*ProjectionBenchmarks*", "*MappingBenchmarks*"),
    "lock": ("*LocalLockBenchmarks*",),
}


def project_path(category: str) -> Path:
    rel = BDN_PROJECTS.get(category)
    if rel is None:
        raise KeyError(category)
    return NET_DIR / rel


def artifacts_dir(category: str) -> Path:
    return project_path(category).parent / "BenchmarkDotNet.Artifacts"


def category_from_csproj_name(base: str) -> str | None:
    """``Lyo.Encryption.Benchmarks`` -> ``encryption``; non-benchmark names -> None."""
    if not base.startswith("Lyo.") or not base.endswith(".Benchmarks"):
        return None
    mid = base[len("Lyo.") : -len(".Benchmarks")]
    return mid.lower() if mid else None


def _clear_dir(path: Path) -> None:
    """Remove directory *contents* only — never the directory itself (Docker bind mounts cannot be rmtree'd)."""
    import shutil

    if not path.exists():
        path.mkdir(parents=True, exist_ok=True)
        return
    for child in path.iterdir():
        if child.is_dir() and not child.is_symlink():
            shutil.rmtree(child)
        else:
            child.unlink(missing_ok=True)


def sync_portfolio_history() -> None:
    """Copy ``docs/benchmarks/{data,history}`` JSON into the Gateway Next.js public tree."""
    import shutil

    dest_root = gateway_public_benchmarks_dir()
    if dest_root is None:
        print("sync-portfolio: no Gateway public/benchmarks (set LYO_PUBLIC_ROOT or clone sibling Lyo-Public) — skipping")
        return
    if not BENCH_HISTORY_DIR.is_dir():
        print("sync-portfolio: no docs/benchmarks/history — skipping")
        return

    history_dest = dest_root / "history"
    history_dest.mkdir(parents=True, exist_ok=True)
    _clear_dir(history_dest)
    for src in BENCH_HISTORY_DIR.iterdir():
        dest = history_dest / src.name
        if src.is_dir():
            shutil.copytree(src, dest)
        else:
            shutil.copy2(src, dest)

    data_dest = dest_root / "data"
    data_dest.mkdir(parents=True, exist_ok=True)
    _clear_dir(data_dest)
    if BENCH_DATA_DIR.is_dir():
        for src in BENCH_DATA_DIR.glob("*.json"):
            shutil.copy2(src, data_dest / src.name)

    print(f"sync-portfolio: copied data+history → {dest_root}")
