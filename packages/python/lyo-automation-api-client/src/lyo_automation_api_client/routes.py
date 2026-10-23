"""REST paths from ``Lyo.Web.Automation.Core.Constants.Rest`` (no leading slash)."""

from urllib.parse import quote

ROUTE = "Automation"
RUNS = "Automation/Run"


def run_path(run_id: str) -> str:
    return f"{RUNS}/{run_id}"


def run_cancel_path(run_id: str) -> str:
    return f"{RUNS}/{run_id}/Cancel"


def run_log_path(run_id: str) -> str:
    return f"{RUNS}/{run_id}/Log"


def run_snapshots_path(run_id: str) -> str:
    return f"{RUNS}/{run_id}/Snapshots"


def run_snapshot_path(run_id: str, name: str) -> str:
    return f"{RUNS}/{run_id}/Snapshot/{quote(name, safe='')}"


def run_step_screenshot_path(run_id: str, index: int) -> str:
    return f"{RUNS}/{run_id}/Steps/{index}/Screenshot"
