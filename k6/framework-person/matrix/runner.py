"""Invoke k6 for a planned MatrixCell."""

from __future__ import annotations

import os
import subprocess
import sys
from pathlib import Path

from .cell import MatrixCell


class K6ProcessRunner:
    def __init__(
        self,
        *,
        root_dir: Path,
        out_dir: Path,
        k6_bin: str = "k6",
        mode: str = "full",
        base_url: str = "http://localhost:5251",
        endpoint_path: str = "/person/QueryConcrete",
        query_project_path: str = "/person/QueryProject",
        root_query_path: str = "/Query",
        token: str = "",
    ) -> None:
        self.root_dir = root_dir
        self.out_dir = out_dir
        self.k6_bin = k6_bin
        self.mode = mode
        self.base_url = base_url
        self.endpoint_path = endpoint_path
        self.query_project_path = query_project_path
        self.root_query_path = root_query_path
        self.token = token

    def run(self, cell: MatrixCell) -> int:
        summary_file = self.out_dir / f"{cell.cell_id}.summary.json"
        log_file = self.out_dir / f"{cell.cell_id}.log"
        test_path = self.root_dir / "scenarios" / cell.scenario_file

        print(f"=== Running {cell.cell_id} ({cell.scenario_file}) ===")
        cmd = [
            self.k6_bin,
            "run",
            "-e",
            f"BASE_URL={self.base_url}",
            "-e",
            f"ENDPOINT_PATH={self.endpoint_path}",
            "-e",
            f"QUERY_PROJECT_PATH={self.query_project_path}",
            "-e",
            f"ROOT_QUERY_PATH={self.root_query_path}",
            "--summary-export",
            str(summary_file),
            str(test_path),
        ]
        if self.token:
            cmd.extend(["-e", f"TOKEN={self.token}"])

        for key, value in cell.to_env().items():
            cmd.extend(["-e", f"{key}={value}"])

        for ceiling_var in (
            "CEILING_RATES",
            "CEILING_STEP_DURATION",
            "CEILING_MAX_VUS",
            "CEILING_GRACEFUL_STOP",
        ):
            if os.environ.get(ceiling_var):
                cmd.extend(["-e", f"{ceiling_var}={os.environ[ceiling_var]}"])

        if self.mode == "smoke":
            cmd.extend(["--vus", "1", "--iterations", "1"])

        extra = os.environ.get("EXTRA_K6_ARGS", "").strip()
        if extra:
            cmd.extend(extra.split())

        with log_file.open("w", encoding="utf-8") as log:
            proc = subprocess.run(cmd, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True)
            log.write(proc.stdout or "")
            sys.stdout.write(proc.stdout or "")

        if proc.returncode != 0:
            print(f"Test failed: {cell.cell_id} (exit {proc.returncode})")
            print(f"See: {log_file}")
        else:
            print(f"Saved summary: {summary_file}")
            print(f"Saved log:     {log_file}\n")
        return proc.returncode
