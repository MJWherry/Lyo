#!/usr/bin/env python3
"""Publish ``docs/benchmarks/{data,history}`` to the Gateway lyo.bench S3 bucket.

Layout matches the static hub:

  s3://<bucket>/data/{suite}.json
  s3://<bucket>/data/registry.json   (written from registry.js if needed)
  s3://<bucket>/history/{suite}/*.json

Usage:
  python3 scripts/benchmarks/publish_s3.py --bucket lyo-gateway-benchmarks-…
  LYO_BENCH_S3_BUCKET=… python3 scripts/benchmarks/publish_s3.py
"""

from __future__ import annotations

import argparse
import json
import os
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from lyo_tooling.dotnet import REPO_ROOT  # noqa: E402

DATA_DIR = REPO_ROOT / "docs" / "benchmarks" / "data"
HISTORY_DIR = REPO_ROOT / "docs" / "benchmarks" / "history"


def _aws(*args: str) -> None:
    cmd = ["aws", "s3", *args]
    print("+", " ".join(cmd), flush=True)
    subprocess.run(cmd, check=True)


def _write_registry_json(dest: Path) -> None:
    """Emit data/registry.json from registry.js when a JSON file is absent."""
    json_path = DATA_DIR / "registry.json"
    if json_path.is_file():
        shutil.copy2(json_path, dest / "registry.json")
        return
    js_path = DATA_DIR / "registry.js"
    if not js_path.is_file():
        return
    text = js_path.read_text(encoding="utf-8")
    start = text.find("{")
    end = text.rfind("}")
    if start < 0 or end <= start:
        return
    payload = json.loads(text[start : end + 1])
    (dest / "registry.json").write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")


def publish(bucket: str, prefix: str = "") -> None:
    bucket = bucket.strip()
    if not bucket:
        raise SystemExit("bucket is required (LYO_BENCH_S3_BUCKET or --bucket)")
    dest_root = f"s3://{bucket}/{prefix}".rstrip("/") if prefix else f"s3://{bucket}"

    with tempfile.TemporaryDirectory(prefix="lyo-bench-s3-") as tmp:
        tmp_path = Path(tmp)
        data_tmp = tmp_path / "data"
        data_tmp.mkdir()
        if DATA_DIR.is_dir():
            for src in DATA_DIR.glob("*.json"):
                shutil.copy2(src, data_tmp / src.name)
        _write_registry_json(data_tmp)
        if any(data_tmp.iterdir()):
            _aws("sync", str(data_tmp), f"{dest_root}/data", "--exclude", "*", "--include", "*.json")

        if HISTORY_DIR.is_dir():
            hist_tmp = tmp_path / "history"
            hist_tmp.mkdir()
            for suite in HISTORY_DIR.iterdir():
                if not suite.is_dir():
                    continue
                out = hist_tmp / suite.name
                out.mkdir()
                for src in suite.glob("*.json"):
                    shutil.copy2(src, out / src.name)
            if any(hist_tmp.iterdir()):
                _aws("sync", str(hist_tmp), f"{dest_root}/history", "--exclude", "*", "--include", "*.json")

    print(f"Published lyo.bench JSON to {dest_root}", flush=True)


def main() -> None:
    parser = argparse.ArgumentParser(description="Sync docs/benchmarks data+history to S3.")
    parser.add_argument(
        "--bucket",
        default=os.environ.get("LYO_BENCH_S3_BUCKET", ""),
        help="S3 bucket name (or LYO_BENCH_S3_BUCKET).",
    )
    parser.add_argument("--prefix", default="", help="Optional key prefix inside the bucket.")
    args = parser.parse_args()
    publish(args.bucket, args.prefix)


if __name__ == "__main__":
    main()
