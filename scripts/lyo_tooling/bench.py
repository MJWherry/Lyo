"""Shared BenchmarkDotNet suite map for run/manifest/docker tooling."""

from __future__ import annotations

from pathlib import Path

from lyo_tooling.dotnet import REPO_ROOT

NET_DIR = REPO_ROOT / "Lyo.Net"
BENCH_DATA_DIR = REPO_ROOT / "docs" / "benchmarks" / "data"
BENCH_HISTORY_DIR = REPO_ROOT / "docs" / "benchmarks" / "history"
PORTFOLIO_HISTORY_DIR = REPO_ROOT / "apps" / "portfolio" / "public" / "benchmarks" / "history"

# category -> csproj path relative to Lyo.Net
BDN_PROJECTS: dict[str, str] = {
    "encryption": "Security/Encryption/Lyo.Encryption.Benchmarks/Lyo.Encryption.Benchmarks.csproj",
    "compression": "Data/Compression/Lyo.Compression.Benchmarks/Lyo.Compression.Benchmarks.csproj",
    "hashing": "Security/Hashing/Lyo.Hashing.Benchmarks/Lyo.Hashing.Benchmarks.csproj",
    "cache": "Core/Cache/Lyo.Cache.Benchmarks/Lyo.Cache.Benchmarks.csproj",
    "query": "Data/Query/Lyo.Query.Benchmarks/Lyo.Query.Benchmarks.csproj",
    "csv": "Data/Csv/Lyo.Csv.Benchmarks/Lyo.Csv.Benchmarks.csproj",
    "xlsx": "Data/Xlsx/Lyo.Xlsx.Benchmarks/Lyo.Xlsx.Benchmarks.csproj",
    "lock": "Core/Lock/Lyo.Lock.Benchmarks/Lyo.Lock.Benchmarks.csproj",
    "filestorage": "Data/FileStorage/Lyo.FileStorage.Benchmarks/Lyo.FileStorage.Benchmarks.csproj",
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
    """Copy ``docs/benchmarks/history`` into the portfolio public tree for snapshot APIs."""
    import shutil

    if not BENCH_HISTORY_DIR.is_dir():
        print("sync-portfolio: no docs/benchmarks/history — skipping")
        return
    PORTFOLIO_HISTORY_DIR.parent.mkdir(parents=True, exist_ok=True)
    _clear_dir(PORTFOLIO_HISTORY_DIR)
    # copytree into an existing (possibly mounted) dest: copy children one by one
    for src in BENCH_HISTORY_DIR.iterdir():
        dest = PORTFOLIO_HISTORY_DIR / src.name
        if src.is_dir():
            shutil.copytree(src, dest)
        else:
            shutil.copy2(src, dest)
    print(f"sync-portfolio: copied history → {PORTFOLIO_HISTORY_DIR.relative_to(REPO_ROOT)}")
